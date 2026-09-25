namespace Core.Save.Participants
{
    using System.Linq;
    using Data;
    using Data.SaveData;
    using Items;
    using Newtonsoft.Json.Linq;
    using Trade;

    /// <summary>Persists what stands on every trader's shelf: the offers, the buyback row and the game
    /// minute each shelf restocks at. Goods travel in the shared item shape, whole — a random slot re-minted
    /// on load would be a different sword than the one the player was deciding about.</summary>
    /// <param name="augments">Optional, as everywhere an augment is written down: a build that mints none
    /// rebuilds none, and a slot holding one is dropped with a report instead of becoming another augment.</param>
    public class TraderShelfSaveParticipant(
        ITraderService traders,
        IItemDataProvider itemData,
        EquipItemSaveConverter converter,
        IAugmentItemMinter? augments = null) : ISaveParticipant
    {
        private readonly InventoryItemSaveConverter _items = new(converter, itemData, augments);

        public string SectionId => "traderShelf";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.TraderShelf;

        public JToken Capture()
        {
            var shelves = traders.CaptureShelves();
            return JToken.FromObject(new TraderShelfSaveData
            {
                OfferCounter = shelves.OfferCounter,
                Traders = [.. shelves.Shelves.Select(Written)]
            });
        }

        /// <summary>The whole set is built before any of it is handed over, so the shelves change once.</summary>
        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<TraderShelfSaveData>();
            if (saved == null) return;
            traders.RestoreShelves(new TraderShelves(saved.OfferCounter, [.. saved.Traders.Select(Read)]));
        }

        private TraderShelfEntrySaveData Written(TraderShelf shelf) => new()
        {
            TraderId = shelf.TraderId,
            NextRestockMinutes = shelf.NextRestockMinutes,
            Offers = [.. shelf.Offers.Select(Written)],
            Buyback = [.. shelf.Buyback.Select(Written)]
        };

        private TraderOfferSaveData Written(TraderOffer offer) => new()
        {
            OfferId = offer.OfferId,
            IsRandomEquip = offer.IsRandomEquip,
            IsBuyback = offer.IsBuyback,
            BuybackUnitPrice = offer.BuybackUnitPrice,
            Item = _items.ToData(offer.Item, offer.Remaining)
        };

        private TraderShelf Read(TraderShelfEntrySaveData saved) => new(
            saved.TraderId,
            saved.NextRestockMinutes,
            [.. saved.Offers.Select(Read).OfType<TraderOffer>()],
            [.. saved.Buyback.Select(Read).OfType<TraderOffer>()]);

        /// <summary>Goods whose copy can no longer be reproduced cost their slot, not the whole shelf.</summary>
        private TraderOffer? Read(TraderOfferSaveData saved)
        {
            if (_items.FromData(saved.Item) is not { } item)
            {
                Tracker.TrackNotFound($"Goods of trader offer '{saved.OfferId}'", this);
                return null;
            }

            return new TraderOffer(saved.OfferId, item, saved.Item.Amount, saved.IsRandomEquip, saved.IsBuyback, saved.BuybackUnitPrice);
        }
    }
}
