namespace LastBreath.World
{
    using System;
    using System.Collections.Generic;
    using Core.Ai.World;
    using Core.Data.GameData;
    using Core.Data.WorldData;
    using Newtonsoft.Json;

    /// <summary>Consumes the Player catalog; the built-in defaults apply if the JSON is absent.</summary>
    public class PlayerLifecycleConfigProvider : IPlayerLifecycleConfigProvider, IGameDataParticipant
    {
        public PlayerLifecycleConfig Config { get; private set; } = new();

        public IReadOnlyList<string> Catalogs => [DataCatalog.Player];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<PlayerLifecycleData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize player lifecycle data");
            Config = new PlayerLifecycleConfig
            {
                LieGameHours = data.LieGameHours,
                ReviveHealthPercent = data.ReviveHealthPercent,
                ReviveManaPercent = data.ReviveManaPercent,
                DeadTimeScale = data.DeadTimeScale,
                BurnChance = data.BurnChance,
                BurnRadius = data.BurnRadius,
            };
        }
    }
}
