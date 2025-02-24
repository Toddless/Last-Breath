namespace Playground.Script.QuestSystem
{
    using System.Collections.Generic;

    public class Quest
    {
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? Description { get; set; }
        public QuestType Type { get; set; }
        public List<QuestObjective>? Objectives { get; set; }
        public Reward? Reward { get; }
        public QuestStatus Status { get; set; }
    }
}
