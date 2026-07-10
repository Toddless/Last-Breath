namespace Core.Events.GameEvents
{
    using Narrative.Quests;

    /// <summary>Every status transition of a quest, including the initial Active on accept.</summary>
    public record QuestStatusChangedEvent(string QuestId, QuestStatus Status) : IGameEvent;

    /// <summary>The quest moved to its next stage (0-based; not fired for entering stage 0).</summary>
    public record QuestStageAdvancedEvent(string QuestId, int StageIndex) : IGameEvent;
}
