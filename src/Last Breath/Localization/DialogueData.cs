namespace Playground.Localization
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DialogueData
    {
        [JsonPropertyName("Dialogs")]
        public List<DialogueNode>? Dialogs { get; set; }
    }
}
