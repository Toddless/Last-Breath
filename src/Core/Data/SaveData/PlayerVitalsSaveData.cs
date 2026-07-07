namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    public class PlayerVitalsSaveData
    {
        [JsonProperty("health")] public float Health { get; init; }
        [JsonProperty("barrier")] public float Barrier { get; init; }
        [JsonProperty("mana")] public float Mana { get; init; }
    }
}
