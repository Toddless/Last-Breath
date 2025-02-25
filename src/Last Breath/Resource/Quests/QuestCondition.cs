namespace Playground.Resource.Quests
{
    using Playground.Components;

    public abstract class QuestCondition
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public abstract bool IsMet(PlayerProgress progress);
    }
}
