namespace Playground.Script.QuestSystem
{
    using Playground.Localization;
    public abstract class QuestObjective
    {
        public LocalizedText? Description { get; set; }
        public bool IsDone { get; set; }
        public abstract void UpdateProgress(object eventData);
    }
}
