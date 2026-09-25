namespace Core.Events
{
    using Narrative.Quests;

    /// <summary>Every status transition of a quest, including the initial Active on accept.</summary>
    public record QuestStatusChangedEvent(string QuestId, QuestStatus Status) : IGameEvent;

    /// <summary>The quest moved on to the named stage; not fired for entering the first one.</summary>
    public record QuestStageAdvancedEvent(string QuestId, string StageId) : IGameEvent;
}
