namespace Core.Data.NpcData
{
    using Enums;

    /// <summary>
    /// One parsed combat reaction of an NPC: on <see cref="Trigger"/> roll <see cref="Chance"/> and
    /// cast the hidden ability, at most <see cref="MaxPerTurn"/> times within a single turn.
    /// <see cref="BlockedByFinalDeathOf"/> is an npc id: once that NPC is finally dead (world fact),
    /// the reaction never fires again — the twin cannot help from beyond the pyre.
    /// </summary>
    public record NpcReactionConfig(
        string AbilityId,
        ReactionTrigger Trigger,
        float Chance,
        int MaxPerTurn,
        string? BlockedByFinalDeathOf);
}
