using System.Text.Json.Serialization;
using Playground.Script.Items;

namespace Playground.Localization
{
    public class DialogueOption
    {
        [JsonPropertyName(nameof(OptionName))]
        public string OptionName {  get; set; } = string.Empty;

        [JsonPropertyName(nameof(Text))]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName(nameof(TargetNode))]
        public string TargetNode { get; set; } = string.Empty;

        [JsonPropertyName(nameof(RelationEffect))]
        public int RelationEffect { get; set; } = 0;

        [JsonPropertyName(nameof(RequiredItem))]
        public Item? RequiredItem { get; set; }
    }
}
