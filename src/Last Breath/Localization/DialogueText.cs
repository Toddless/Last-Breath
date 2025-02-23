namespace Playground.Localization
{
    using System.Text.Json.Serialization;
    public class DialogueText
    {
        [JsonPropertyName(nameof(NpcText))]
        public string NpcText {  get; set; } = string.Empty;

        [JsonPropertyName(nameof(MinRelation))]
        public int MinRelation { get; set; } = 0;
    }
}
