namespace Core.Narrative.Quests
{
    using System.Collections.Generic;
    using System.Linq;
    using Actions;
    using Conditions;
    using Enums;

    /// <summary>Immutable, fully parsed quest. Display name/description come from the .po by
    /// convention: Quest_&lt;Id&gt;, Quest_&lt;Id&gt;_Description, stage text Quest_&lt;Id&gt;_Stage_&lt;StageId&gt;,
    /// outcome text Quest_&lt;Id&gt;_Outcome_&lt;OutcomeId&gt;.</summary>
    /// <param name="CanFail">False makes the quest unloseable: every failure path refuses, so the
    /// journal entry survives a deadline, a lost turn-in NPC and a decline under a Fail policy.</param>
    public record QuestDefinition(
        string Id,
        string GiverNpcId,
        Fractions? Faction,
        int Tier,
        bool Repeatable,
        IReadOnlyList<string> TurnInNpcIds,
        DeclinePolicy DeclinePolicy,
        int DeclineCooldownHours,
        bool CanFail,
        int TimeLimitHours,
        IReadOnlyList<INarrativeCondition> AcceptConditions,
        IReadOnlyList<QuestStageDefinition> Stages,
        QuestRewards Rewards,
        IReadOnlyList<INarrativeAction> OnAccept,
        IReadOnlyList<INarrativeAction> OnDecline,
        IReadOnlyList<INarrativeAction> OnFail)
    {
        /// <summary>The stage a state stands on; a state naming none stands on the first one.</summary>
        public QuestStageDefinition? Stage(string stageId) =>
            stageId.Length == 0 ? Stages.FirstOrDefault() : Stages.FirstOrDefault(stage => stage.Id == stageId);

        /// <summary>The ending a state reached, if it named one. Outcome ids are unique per quest.</summary>
        public QuestOutcomeDefinition? Outcome(string? outcomeId) =>
            outcomeId == null ? null : Stages.Select(stage => stage.Outcome).FirstOrDefault(outcome => outcome?.Id == outcomeId);
    }

    /// <param name="Transitions">Routes out of the stage, tried in order; empty = the next stage of the list.</param>
    /// <param name="Outcome">Set = the stage ends the quest; such a stage carries no transitions.</param>
    public record QuestStageDefinition(
        string Id,
        IReadOnlyList<QuestObjectiveDefinition> Objectives,
        IReadOnlyList<INarrativeAction> OnEnter,
        IReadOnlyList<INarrativeAction> OnComplete,
        IReadOnlyList<QuestStageTransition> Transitions,
        QuestOutcomeDefinition? Outcome);

    /// <summary>One route out of a stage; an empty condition list is unconditional.</summary>
    public record QuestStageTransition(string ToStageId, IReadOnlyList<INarrativeCondition> Conditions);

    /// <summary>A named ending. Its rewards are paid INSTEAD of the quest-wide ones, so an outcome
    /// declaring none pays nothing. Fails buries the quest instead of offering a turn-in.</summary>
    public record QuestOutcomeDefinition(string Id, bool Fails, QuestRewards Rewards);

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
