namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    public class WorldClockSaveData
    {
        [JsonProperty("day")] public int Day { get; init; }
        [JsonProperty("minuteOfDay")] public int MinuteOfDay { get; init; }
    }
}
