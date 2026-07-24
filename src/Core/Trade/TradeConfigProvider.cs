namespace Core.Trade
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Newtonsoft.Json;

    public class TradeConfigProvider : ITradeConfigProvider, IGameDataParticipant
    {
        public TradeConfig Config { get; private set; } = new();

        public IReadOnlyList<string> Catalogs => [DataCatalog.Trade];

        public void Apply(string catalog, GameDataFile file) =>
            Config = JsonConvert.DeserializeObject<TradeConfig>(file.Json)
                     ?? throw new InvalidOperationException($"Failed to deserialize trade config '{file.FileName}'");
    }
}
