namespace Playground.Script.QuestSystem
{
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class QuestData
    {
        [JsonPropertyName(nameof(Quests))]
        public List<Quest>? Quests { get; set; }
    }
}
