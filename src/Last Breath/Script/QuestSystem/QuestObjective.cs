namespace Playground.Script.QuestSystem
{
    public abstract class QuestObjective
    {
        public string? Description { get; set; }
        public bool IsDone { get; set; }
        public abstract void UpdateProgress(object eventData);
    }
}
