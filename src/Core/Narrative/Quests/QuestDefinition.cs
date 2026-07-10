namespace Core.Narrative.Quests
{
    using System.Collections.Generic;
    using Actions;
    using Conditions;
    using Enums;

    /// <summary>Immutable, fully parsed quest. Display name/description come from the .po by
    /// convention: Quest_&lt;Id&gt;, Quest_&lt;Id&gt;_Description, stage text Quest_&lt;Id&gt;_Stage_&lt;StageId&gt;.</summary>
    public record QuestDefinition(
        string Id,
        string GiverNpcId,
        Fractions? Faction,
        int Tier,
        bool Repeatable,
        IReadOnlyList<string> TurnInNpcIds,
        DeclinePolicy DeclinePolicy,
        int DeclineCooldownHours,
        int TimeLimitHours,
        IReadOnlyList<INarrativeCondition> AcceptConditions,
        IReadOnlyList<QuestStageDefinition> Stages,
        QuestRewards Rewards,
        IReadOnlyList<INarrativeAction> OnAccept,
        IReadOnlyList<INarrativeAction> OnDecline,
        IReadOnlyList<INarrativeAction> OnFail);

    public record QuestStageDefinition(
        string Id,
        IReadOnlyList<QuestObjectiveDefinition> Objectives,
        IReadOnlyList<INarrativeAction> OnEnter,
        IReadOnlyList<INarrativeAction> OnComplete);

    /// <summary>Either Condition (boolean predicate, retroactive by nature) or Counter is set.</summary>
    public record QuestObjectiveDefinition(
        string Id,
        INarrativeCondition? Condition,
        QuestCounter? Counter,
        bool IsOptional,
        bool IsHidden);

    public record QuestCounter(string FactKey, int Amount, bool Retroactive);

    public record QuestRewards(
        int InfluenceExp,
        IReadOnlyList<QuestRewardItem> Items,
        IReadOnlyList<INarrativeAction> Actions);

    public record QuestRewardItem(string ItemId, int Amount);
}
