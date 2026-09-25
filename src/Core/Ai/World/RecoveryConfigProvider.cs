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

        public void Apply(string catalog, GameDataFile file) =>
            Config = JsonConvert.DeserializeObject<RecoveryConfig>(file.Json)
                     ?? throw new InvalidOperationException("Failed to deserialize recovery data");
    }
}
