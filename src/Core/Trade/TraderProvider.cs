namespace Core.Trade
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Data.TradeData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>A parsed trader: the enum fields are validated once at load.</summary>
    public record TraderDefinition(
        string Id,
        Fractions Fraction,
        float RestockGameMinutes,
        IReadOnlyList<TraderCatalogEntryData> Catalog,
        TraderRandomEquipData? RandomEquip);

    public interface ITraderProvider
    {
        IReadOnlyList<TraderDefinition> Traders { get; }

        TraderDefinition? GetTrader(string id);
    }

    public class TraderProvider : ITraderProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, TraderDefinition> _traders = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Traders];

        public IReadOnlyList<TraderDefinition> Traders => [.. _traders.Values];

        public TraderDefinition? GetTrader(string id) => _traders.GetValueOrDefault(id);

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<TradersData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize traders file '{file.FileName}'");

            foreach (var trader in data.Traders)
            {
                if (string.IsNullOrWhiteSpace(trader.Id))
                {
                    Tracker.TrackError($"Skipping trader without an id in '{file.FileName}'");
                    continue;
                }

                if (!EnumParser.TryParseEnum<Fractions>(trader.Fraction, out var fraction))
                {
                    Tracker.TrackError($"Skipping trader '{trader.Id}': '{trader.Fraction}' is not a faction");
                    continue;
                }

                _traders[trader.Id] = new TraderDefinition(trader.Id, fraction, trader.RestockGameMinutes, trader.Catalog, trader.RandomEquip);
            }
        }
    }
}
