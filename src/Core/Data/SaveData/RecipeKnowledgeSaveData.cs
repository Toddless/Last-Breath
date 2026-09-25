namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    public record RecipeKnowledgeSaveData
    {
        [JsonProperty("learned")] public string[] Learned { get; init; } = [];
    }
}
