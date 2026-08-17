namespace Core.Ai.World
{
    using System;
    using System.Collections.Generic;
    using Recovery;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>Consumes the Recovery catalog; the built-in defaults apply if the JSON is absent.</summary>
    public class RecoveryConfigProvider : IRecoveryConfigProvider, IGameDataParticipant
    {
        public RecoveryConfig Config { get; private set; } = new();

        public IReadOnlyList<string> Catalogs => [DataCatalog.Recovery];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<RecoveryData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize recovery data");
            Config = new RecoveryConfig
            {
                HealthPercentPerMinute = data.HealthPercentPerMinute,
                ManaPercentPerMinute = data.ManaPercentPerMinute,
                BarrierPercentPerMinute = data.BarrierPercentPerMinute,
                DefaultZoneRadius = data.DefaultZoneRadius,
                NpcRetreatHealthPercent = data.NpcRetreatHealthPercent,
                NpcGiveUpMinutes = data.NpcGiveUpMinutes,
            };
        }

        private record RecoveryData
        {
            [JsonProperty("healthPercentPerMinute")] public float HealthPercentPerMinute { get; init; } = 0.1f;
            [JsonProperty("manaPercentPerMinute")] public float ManaPercentPerMinute { get; init; } = 0.15f;
            [JsonProperty("barrierPercentPerMinute")] public float BarrierPercentPerMinute { get; init; } = 0.15f;
            [JsonProperty("defaultZoneRadius")] public float DefaultZoneRadius { get; init; } = 150f;
            [JsonProperty("npcRetreatHealthPercent")] public float NpcRetreatHealthPercent { get; init; } = 0.5f;
            [JsonProperty("npcGiveUpMinutes")] public float NpcGiveUpMinutes { get; init; } = 3f;
        }
    }
}
