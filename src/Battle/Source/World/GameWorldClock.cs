namespace Battle.Source.World
{
    using System;
    using System.Threading.Tasks;
    using Core;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.WorldData;
    using Newtonsoft.Json;

    /// <summary>The runtime world clock: the pure WorldClock configured from res://Data/World/.</summary>
    public class GameWorldClock : WorldClock
    {
        private const string DataPath = "res://Data/World/";

        public GameWorldClock() => _ = LoadDataAsync();

        private async Task LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(DataPath, ParseConfig);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load world clock data", e);
            }
        }

        private Task ParseConfig(string json)
        {
            var data = JsonConvert.DeserializeObject<WorldClockData>(json)
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
            return Task.CompletedTask;
        }
    }
}
