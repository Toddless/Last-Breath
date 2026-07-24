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
    /// equip slot holds its concrete rolled instance (what you see is what you buy).</summary>
    public record TraderOffer(string OfferId, IItem Item, int Remaining, bool IsRandomEquip);

    public interface ITraderService
    {
        /// <summary>The trader's current shelf; restocks lazily when its game-time is due.
        /// Unknown trader id = empty (reported once by the provider at load).</summary>
        IReadOnlyList<TraderOffer> GetStock(string traderId);

        /// <summary>Takes one unit off the shelf: a fresh copy for plain goods, the rolled instance
        /// itself for random equips. Null when the offer is gone. The caller owns payment.</summary>
        IItem? TakeOne(string traderId, string offerId);

        TraderDefinition? GetTrader(string traderId);
    }

    /// <summary>
    /// Hybrid stock (Todd 2026-07-24): the authored catalog rolls per restock (chance gates an
    /// entry's appearance), the random slots mint real equip instances through the item pipeline.
    /// Stock is session state for now — it is NOT saved (a reload restocks; revisit with the
    /// anti-savescam pass).
    /// </summary>
    public class TraderService(
        ITraderProvider traders,
        ITradeConfigProvider configProvider,
        IItemDataProvider? itemData = null,
        IItemCreationService? itemCreation = null,
        IWorldClock? clock = null,
        IRandomNumberGenerator? rnd = null) : ITraderService, ISessionResettable
    {
        private sealed class Offer(string offerId, IItem item, bool isRandomEquip)
        {
            public string OfferId { get; } = offerId;
            public IItem Item { get; } = item;
            public bool IsRandomEquip { get; } = isRandomEquip;
            public int Remaining { get; set; } = 1;
        }

        private sealed class TraderState
        {
            public double NextRestockMinutes = double.MinValue; // first access always stocks
            public List<Offer> Offers = [];
        }

        private readonly Dictionary<string, TraderState> _states = [];
        private readonly IRandomNumberGenerator _rnd = rnd ?? new DefaultRandomNumberGenerator();
        private int _offerCounter;

        private double NowMinutes => clock != null ? clock.Day * 1440 + clock.MinuteOfDay : 0;

        public TraderDefinition? GetTrader(string traderId) => traders.GetTrader(traderId);

        public IReadOnlyList<TraderOffer> GetStock(string traderId)
        {
            var state = EnsureFreshState(traderId);
            return state == null
                ? []
                : state.Offers.Where(offer => offer.Remaining > 0)
                    .Select(offer => new TraderOffer(offer.OfferId, offer.Item, offer.Remaining, offer.IsRandomEquip))
                    .ToList();
        }

        public IItem? TakeOne(string traderId, string offerId)
        {
            var state = EnsureFreshState(traderId);
            var offer = state?.Offers.FirstOrDefault(entry => entry.OfferId == offerId && entry.Remaining > 0);
            if (offer == null) return null;

            offer.Remaining--;
            // A random equip is the instance on the shelf; plain goods hand out fresh copies.
            return offer.IsRandomEquip ? offer.Item : offer.Item.Copy<IItem>();
        }

        public void ResetSession()
        {
            _states.Clear();
            _offerCounter = 0;
        }

        private TraderState? EnsureFreshState(string traderId)
        {
            var definition = traders.GetTrader(traderId);
            if (definition == null) return null;

            if (!_states.TryGetValue(traderId, out var state))
                _states[traderId] = state = new TraderState();

            if (NowMinutes >= state.NextRestockMinutes || state.NextRestockMinutes == double.MinValue)
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
