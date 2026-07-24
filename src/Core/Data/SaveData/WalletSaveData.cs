namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    public record WalletSaveData
    {
        [JsonProperty("gold")] public int Gold { get; init; }
    }
}
