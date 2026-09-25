namespace Core.Narrative.Quests
{
    using System.Collections.Generic;

    /// <summary>
    /// The single owner of quest states. Objective completion is always re-derived from facts and
    /// inventory ("the world remembers, the quest asks"), so a battlefield or an item found before
    /// the quest closes its stage the moment the quest is accepted. Completion is never automatic:
    /// ReadyToTurnIn waits for the turn-in action (a dialogue in the normal flow).
    /// </summary>
    public interface IQuestLogService
    {
        IReadOnlyCollection<QuestState> States { get; }

        QuestState? GetState(string questId);

        /// <summary>Null = never taken (there is no state).</summary>
        QuestStatus? GetStatus(string questId);

        bool CanAccept(string questId, NarrativeContext context);
        bool Accept(string questId, NarrativeContext context);

        /// <summary>Turning an OFFER down. Policy decides: Fail buries the quest, CanReturn/Cooldown
        /// leave it offerable again — as does a Fail policy on a quest that cannot fail.</summary>
        void Decline(string questId, NarrativeContext context);

        /// <summary>Dropping an ACCEPTED quest; a Fail-policy quest fails for good unless it
        /// declares itself unloseable, in which case it becomes offerable again.</summary>
        void Abandon(string questId);

        /// <summary>The single gate of failure: every path to a Failed quest goes through here.
        /// False = nothing was buried — the quest declares it cannot fail, is already finished,
        /// or was never taken.</summary>
        bool Fail(string questId, string reason);

        /// <summary>False while rewards don't fit the inventory — the turn-in option shows disabled.</summary>
        bool CanTurnIn(string questId);

        bool TurnIn(string questId, NarrativeContext context);

        /// <summary>Live progress of an objective of the CURRENT stage; conditions read 0/1 of 1.</summary>
        (int Current, int Required) GetObjectiveProgress(string questId, string objectiveId);

        /// <summary>Load-time restore: replaces all states without firing events or actions.</summary>
        void RestoreState(IEnumerable<QuestState> states);
    }
}
