namespace Core.Narrative.Quests
{
    /// <summary>No state for a quest means "not taken yet".</summary>
    public enum QuestStatus : byte
    {
        Active,

        /// <summary>All mandatory objectives met; completion happens only by the turn-in action —
        /// walking in with the item already found lands here straight from Accept.</summary>
        ReadyToTurnIn,

        Completed,
        Failed,

        /// <summary>Turned down under CanReturn/Cooldown policy; may be offered again.</summary>
        Declined,
    }

    public enum DeclinePolicy : byte
    {
        /// <summary>Turning it down (or abandoning it) fails the quest for good.</summary>
        Fail,

        CanReturn,

        /// <summary>May be offered again after declineCooldownHours of game time.</summary>
        Cooldown,
    }
}
