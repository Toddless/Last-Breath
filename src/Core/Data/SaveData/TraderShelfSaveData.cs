namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// The trader shelves as a file holds them. Saving them is what gives a reload a cost: a shelf rolled
    /// again on every load would let the player shop for a better one by loading until it appeared.
    /// </summary>
    public class TraderShelfSaveData
    {
        /// <summary>How many offers have been numbered so far. Without it the numbering would start over
        /// and a freshly stocked offer would take the id of one already standing on a restored shelf.</summary>
        [JsonProperty("offerCounter")] public int OfferCounter { get; init; }

        [JsonProperty("traders")] public List<TraderShelfEntrySaveData> Traders { get; init; } = [];
    }

    public class TraderShelfEntrySaveData
    {
        [JsonProperty("traderId")] public string TraderId { get; init; } = string.Empty;

        /// <summary>The game minute the shelf is due to restock at. A shelf that has never been stocked
        /// carries the far-negative floor it is born with, which every clock reading is already past — so
        /// the first visit after loading stocks it.</summary>
        [JsonProperty("nextRestockMinutes")] public double NextRestockMinutes { get; init; }

        [JsonProperty("offers")] public List<TraderOfferSaveData> Offers { get; init; } = [];

        /// <summary>What the player sold, waiting to be bought back at the price it earned.</summary>
        [JsonProperty("buyback")] public List<TraderOfferSaveData> Buyback { get; init; } = [];
    }

    /// <summary>One shelf slot. The goods are written whole, in the shape the bag writes its own
    /// (<see cref="InventoryItemSaveData"/>) and with the units left of them as its amount: a rolled
    /// instance re-created on load would not be the one the player was looking at.</summary>
    public class TraderOfferSaveData
    {
        [JsonProperty("offerId")] public string OfferId { get; init; } = string.Empty;

        /// <summary>A slot holding its own rolled instance — it is sold as it stands, not copied.</summary>
        [JsonProperty("isRandomEquip")] public bool IsRandomEquip { get; init; }

        [JsonProperty("isBuyback")] public bool IsBuyback { get; init; }

        /// <summary>What the trader paid per unit — undoing a sale costs exactly what it earned.</summary>
        [JsonProperty("buybackUnitPrice")] public int BuybackUnitPrice { get; init; }

        [JsonProperty("item")] public InventoryItemSaveData Item { get; init; } = new();
    }
}
