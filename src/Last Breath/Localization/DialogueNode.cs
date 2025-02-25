namespace Playground.Localization
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DialogueNode
    {
        [JsonPropertyName(nameof(Id))]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName(nameof(Texts))]
        public List<DialogueText>? Texts { get; set; }

        [JsonPropertyName(nameof(Options))]
        public List<DialogueOption>? Options { get; set; }

        [JsonPropertyName(nameof(ReturnToPrevious))]
        public bool ReturnToPrevious { get; set; } = false;

        [JsonPropertyName(nameof(Quests))]
        public List<string>? Quests { get; set; }
    }
}
