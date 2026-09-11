namespace Core.Save.Participants
{
    using Ai.World.Time;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;

    public class WorldClockSaveParticipant(IWorldClock clock) : ISaveParticipant
    {
        public string SectionId => "worldClock";
        public int Version => 2;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture() => JToken.FromObject(new WorldClockSaveData
        {
            TotalMinutes = clock.TotalMinutes,
            Day = clock.Day,
            MinuteOfDay = clock.MinuteOfDay
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<WorldClockSaveData>();
            if (saved == null) return;
            clock.RestoreTime(saved.TotalMinutes ?? saved.Day * 1440.0 + saved.MinuteOfDay);
        }
    }
}
