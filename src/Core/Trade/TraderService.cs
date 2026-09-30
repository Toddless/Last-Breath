namespace Core.Trade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Time;
    using Data;
    using Entity.Components;
    using Enums;
    using Items;
    using Services;
    using Session;

    /// <summary>One shelf slot: plain goods keep a counter and mint a copy per purchase; a random
    /// equip slot holds its concrete rolled instance (what you see is what you buy). A buyback offer
    /// remembers the unit price the trader paid — undoing a sale costs exactly what it earned.</summary>
    public record TraderOffer(string OfferId, IItem Item, int Remaining, bool IsRandomEquip, bool IsBuyback = false, int BuybackUnitPrice = 0);

    /// <summary>One trader's shelf as it stands: what is on it, what waits on its buyback row, and the
    /// game minute it is due to restock at.</summary>
    public record TraderShelf(string TraderId, double NextRestockMinutes, IReadOnlyList<TraderOffer> Offers, IReadOnlyList<TraderOffer> Buyback);

    /// <summary>Every shelf plus the offer numbering they share — without the counter a shelf carried over
    /// from a file and a freshly stocked one would hand the same offer id to two different things.</summary>
    public record TraderShelves(int OfferCounter, IReadOnlyList<TraderShelf> Shelves);

    public interface ITraderService
    {
        /// <summary>The trader's current shelf (buyback offers included); restocks lazily when its
        /// game-time is due. Unknown trader id = empty (reported once by the provider at load).</summary>
        IReadOnlyList<TraderOffer> GetStock(string traderId);

        /// <summary>Takes units off the shelf all-or-nothing: a fresh copy for plain goods (the
        /// caller adds it with the amount), the stored instance for equips/buyback. Null when the
        /// offer can't cover the amount. The caller owns payment.</summary>
        IItem? TakeMany(string traderId, string offerId, int amount);

        /// <summary>A sold item lands on the trader's buyback shelf at the price it earned.</summary>
        void AddBuyback(string traderId, IItem item, int amount, int unitPrice);

        TraderDefinition? GetTrader(string traderId);

        /// <summary>The absolute game minute the trader's shelf refreshes at; null while no shelf
        /// has been rolled yet. Reading schedules nothing — the visit that shows the shelf
        /// (GetStock) is what stocks it.</summary>
        double? GetNextRestockMinutes(string traderId);

        /// <summary>The shelves as they stand, for the save file. Reading them stocks nothing: a trader
        /// nobody has visited has no shelf yet, and a capture must not be the visit that rolls one.</summary>
        TraderShelves CaptureShelves();

        /// <summary>Puts the stored shelves back in place of the current ones, restock deadlines and offer
        /// numbering included — a loaded shelf is the one the player left, not a fresh roll.</summary>
        void RestoreShelves(TraderShelves shelves);
    }

    /// <summary>
    /// Hybrid stock: the authored catalog rolls per restock (chance gates an entry's appearance), the
    /// random slots mint real equip instances through the item pipeline. Shelves are saved whole, so a
    /// reload is not a way to shop for a better roll — they refresh on their own game-time schedule.
    /// </summary>
    public class TraderService(
        ITraderProvider traders,
        ITradeConfigProvider configProvider,
        IItemDataProvider? itemData = null,
        IItemCreationService? itemCreation = null,
        IWorldClock? clock = null,
        IRandomNumberGenerator? rnd = null) : ITraderService, ISessionResettable
    {
        private const int BuybackCapacity = 15;
        private const float Tolerance = 0.001f;
        private const double GameMinutesPerDay = 1440;

        private sealed class Offer(string offerId, IItem item, bool isRandomEquip, bool isBuyback = false, int buybackUnitPrice = 0)
        {
            public string OfferId { get; } = offerId;
            public IItem Item { get; } = item;
            public bool IsRandomEquip { get; } = isRandomEquip;
            public bool IsBuyback { get; } = isBuyback;
            public int BuybackUnitPrice { get; } = buybackUnitPrice;
            public int Remaining { get; set; } = 1;
        }

        private sealed class TraderState
        {
            public double NextRestockMinutes = double.MinValue; // first access always stocks
            public List<Offer> Offers = [];

            /// <summary>Survives restocks on purpose: the shelf refreshes, a mistake stays fixable.</summary>
            public List<Offer> Buyback = [];
        }

        private readonly Dictionary<string, TraderState> _states = [];
        private readonly IRandomNumberGenerator _rnd = rnd ?? new DefaultRandomNumberGenerator();
        private int _offerCounter;

        private double NowMinutes => clock != null ? clock.Day * GameMinutesPerDay + clock.MinuteOfDay : 0;

        public TraderDefinition? GetTrader(string traderId) => traders.GetTrader(traderId);

        public double? GetNextRestockMinutes(string traderId) =>
            _states.TryGetValue(traderId, out var state) && Math.Abs(state.NextRestockMinutes - double.MinValue) > Tolerance
                ? state.NextRestockMinutes
                : null;

        public IReadOnlyList<TraderOffer> GetStock(string traderId)
        {
            var state = EnsureFreshState(traderId);
            return state == null
                ? []
                : state.Offers.Concat(state.Buyback).Where(Standing).Select(Described).ToList();
        }

        public IItem? TakeMany(string traderId, string offerId, int amount)
        {
            if (amount < 1) return null;
            var state = EnsureFreshState(traderId);
            var offer = state?.Offers.Concat(state.Buyback).FirstOrDefault(entry => entry.OfferId == offerId && entry.Remaining >= amount);
            if (offer == null) return null;

            offer.Remaining -= amount;
            if (offer is { IsBuyback: true, Remaining: 0 }) state!.Buyback.Remove(offer);

            // Rolled instances (random equips, buyback gear) leave the shelf themselves;
            // plain goods hand out one fresh copy the caller adds with the amount.
            return offer.IsRandomEquip || offer is { IsBuyback: true, Item.MaxStackSize: <= 1 }
                ? offer.Item
                : offer.Item.Copy<IItem>();
        }

        public void AddBuyback(string traderId, IItem item, int amount, int unitPrice)
        {
            if (traders.GetTrader(traderId) == null || amount < 1) return;
            if (!_states.TryGetValue(traderId, out var state))
                _states[traderId] = state = new TraderState();

            // Piecemeal sales of the same stackable merge into one shelf row (same goods, same
            // earned price); a price moved by a perk change opens a new row — the shelf never lies.
            var existing = item.MaxStackSize > 1
                ? state.Buyback.FirstOrDefault(offer => offer.Item.Id == item.Id && offer.BuybackUnitPrice == unitPrice)
                : null;
            if (existing != null)
            {
                existing.Remaining += amount;
                return;
            }

            state.Buyback.Add(new Offer(NextOfferId(), item, isRandomEquip: false, isBuyback: true, buybackUnitPrice: unitPrice) { Remaining = amount });
            while (state.Buyback.Count > BuybackCapacity) state.Buyback.RemoveAt(0); // oldest mistake expires first
        }

        /// <summary>Only what still stands is written down: a sold-out offer is invisible, unbuyable and
        /// replaced whole by the next restock, so carrying it would write a bought item twice — once on
        /// the shelf it left and once in the bag it went to.</summary>
        public TraderShelves CaptureShelves() => new(
            _offerCounter,
            _states.Select(pair => new TraderShelf(
                pair.Key,
                pair.Value.NextRestockMinutes,
                pair.Value.Offers.Where(Standing).Select(Described).ToList(),
                pair.Value.Buyback.Where(Standing).Select(Described).ToList())).ToList());

        public void RestoreShelves(TraderShelves shelves)
        {
            _states.Clear();
            _offerCounter = shelves.OfferCounter;
            foreach (var shelf in shelves.Shelves)
                _states[shelf.TraderId] = new TraderState
                {
                    NextRestockMinutes = shelf.NextRestockMinutes,
                    Offers = [.. shelf.Offers.Select(Rebuilt)],
                    Buyback = [.. shelf.Buyback.Select(Rebuilt)]
                };
        }

        public void ResetSession()
        {
            _states.Clear();
            _offerCounter = 0;
        }

        private static bool Standing(Offer offer) => offer.Remaining > 0;

        private static TraderOffer Described(Offer offer) =>
            new(offer.OfferId, offer.Item, offer.Remaining, offer.IsRandomEquip, offer.IsBuyback, offer.BuybackUnitPrice);

        private static Offer Rebuilt(TraderOffer offer) =>
            new(offer.OfferId, offer.Item, offer.IsRandomEquip, offer.IsBuyback, offer.BuybackUnitPrice) { Remaining = offer.Remaining };

        private TraderState? EnsureFreshState(string traderId)
        {
            var definition = traders.GetTrader(traderId);
            if (definition == null) return null;

            if (!_states.TryGetValue(traderId, out var state))
                _states[traderId] = state = new TraderState();

            if (NowMinutes >= state.NextRestockMinutes || Math.Abs(state.NextRestockMinutes - double.MinValue) < Tolerance)
                Restock(definition, state);

            return state;
        }

        private void Restock(TraderDefinition definition, TraderState state)
        {
            state.NextRestockMinutes = NowMinutes + definition.RestockGameMinutes;
            state.Offers = [];

            foreach (var entry in definition.Catalog)
            {
                if (entry.Chance < 1f && _rnd.RandFloat() > entry.Chance) continue;
                var template = ResolveCatalogItem(entry.ItemId);
                if (template == null) continue; // the resolver reported
                state.Offers.Add(new Offer(NextOfferId(), template, isRandomEquip: false) { Remaining = Math.Max(1, entry.Count) });
            }

            MintRandomEquips(definition, state);
        }

        /// <summary>Plain goods copy from the item store; an equip id mints one instance at the
        /// blueprint's own rarity (an authored catalog sells the base version — the random slots
        /// are the source of rolled surprises).</summary>
        private IItem? ResolveCatalogItem(string itemId)
        {
            if (itemData == null) return null;

            if (itemData.GetBlueprint(itemId) is { } blueprint)
                return itemCreation?.CreateItem(itemId, [], blueprint.Rarity, configProvider.Config.RandomEquipEffectChance, 1f);

            try
            {
                return itemData.CopyItem(itemId);
            }
            catch (Exception)
            {
                Tracker.TrackError($"Trader catalog id '{itemId}' resolves to no item");
                return null;
            }
        }

        private void MintRandomEquips(TraderDefinition definition, TraderState state)
        {
            if (definition.RandomEquip is not { Count: > 0 } random || itemData == null || itemCreation == null) return;

            // Fixed-template exclusives never roll on a shelf: uniques/mythics are earned, not bought.
            var blueprints = itemData.AllBlueprints
                .Where(blueprint => blueprint.Rarity != Rarity.Unique && blueprint.Rarity != Rarity.Mythic)
                .ToList();
            if (blueprints.Count == 0) return;

            var rarities = ParseRarityWeights(random.RarityWeights, definition.Id);
            for (int i = 0; i < random.Count; i++)
            {
                var blueprint = blueprints[_rnd.RandIntRange(0, blueprints.Count - 1)];
                var rarity = rarities.Count > 0
                    ? rarities[(int)_rnd.RandWeighted(rarities.Select(pair => pair.Weight).ToArray())].Rarity
                    : blueprint.Rarity;
                var item = itemCreation.CreateItem(blueprint.Id, [], rarity, configProvider.Config.RandomEquipEffectChance, 1f);
                state.Offers.Add(new Offer(NextOfferId(), item, isRandomEquip: true));
            }
        }

        private static List<(Rarity Rarity, float Weight)> ParseRarityWeights(Dictionary<string, float> weights, string traderId)
        {
            var result = new List<(Rarity, float)>();
            foreach ((string key, float weight) in weights)
            {
                if (!EnumParser.TryParseEnum<Rarity>(key, out var rarity))
                {
                    Tracker.TrackError($"Trader '{traderId}': '{key}' is not a rarity — weight skipped");
                    continue;
                }

                result.Add((rarity, weight));
            }

            return result;
        }

        private string NextOfferId() => $"offer_{++_offerCounter}";
    }
}
