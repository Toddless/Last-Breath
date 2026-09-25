namespace Core.Enums
{
    /// <summary>What fires an NPC combat reaction (the "reactions" section of Npc.json). Extensible:
    /// new triggers get a member here and a matching branch in the reactions driver.</summary>
    public enum ReactionTrigger
    {
        /// <summary>The owner took damage caused by an attack (not by abilities/effects — reaction cycles are cut by cause).</summary>
        DamagedByAttack,
    }
}
