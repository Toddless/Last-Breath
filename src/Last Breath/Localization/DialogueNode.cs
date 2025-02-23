using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Playground.Localization
{
    public class DialogueNode
    {
        [JsonPropertyName(nameof(Id))]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName(nameof(Texts))]
        public List<DialogueText>? Texts { get; set; }

        [JsonPropertyName(nameof(Options))]
        public List<DialogueOption>? Options { get; set; }
    }
}
