namespace Core.Data.WorldData
{
    using Newtonsoft.Json;

    /// <summary>WorldClock.json — maps 1:1 to Core.Ai.World.Time.WorldClockConfig.</summary>
    public record WorldClockData
    {
        [JsonProperty("realMinutesPerGameDay")] public float RealMinutesPerGameDay { get; init; } = 24f;
        [JsonProperty("startDay")] public int StartDay { get; init; } = 1;
        [JsonProperty("startHour")] public int StartHour { get; init; } = 8;
        [JsonProperty("morningStartHour")] public int MorningStartHour { get; init; } = 6;
        [JsonProperty("dayStartHour")] public int DayStartHour { get; init; } = 10;
        [JsonProperty("eveningStartHour")] public int EveningStartHour { get; init; } = 18;
        [JsonProperty("nightStartHour")] public int NightStartHour { get; init; } = 22;
    }
}
