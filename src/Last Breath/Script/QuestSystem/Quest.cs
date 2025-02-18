namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;
    using Playground.Script.Helpers;

    public class Quest
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public LocalizedText? Name { get; set; }
        public LocalizedText? Description { get; set; }
        public QuestType Type { get; set; }
        public List<QuestObjective>? Objectives { get; set; }
        public Reward? Reward { get; }
        public QuestStatus Status { get; set; }
    }
}
