namespace Battle.Source.World
{
    using System;
    using System.Collections.Generic;
    using Core.Ai.World.Time;
    using Core.Data.GameData;
    using Core.Data.WorldData;
    using Newtonsoft.Json;

    /// <summary>The runtime world clock: the pure WorldClock configured from the World catalog.</summary>
    public class GameWorldClock : WorldClock, IGameDataParticipant
    {
        public IReadOnlyList<string> Catalogs => [DataCatalog.World];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<WorldClockData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize world clock data");
            Configure(new WorldClockConfig
            {
                RealMinutesPerGameDay = data.RealMinutesPerGameDay,
                StartDay = data.StartDay,
                StartHour = data.StartHour,
                MorningStartHour = data.MorningStartHour,
                DayStartHour = data.DayStartHour,
                EveningStartHour = data.EveningStartHour,
                NightStartHour = data.NightStartHour,
            });
        }
    }
}
