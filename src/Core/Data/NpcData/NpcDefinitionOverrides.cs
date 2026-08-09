namespace Core.Data.NpcData
{
    using Enums;

    /// <summary>
    /// Pins identity facts of a definition instead of rolling them — the save system rebuilds
    /// corpses/risen undead with their original level/rarity/stance (abilities and NPC modifiers
    /// re-roll: acceptable fidelity loss, they are not identity).
    /// <para>
    /// The re-roll is for BODIES. A wild living NPC (a quest's trial target) carries its modifier ids
    /// in a field of the npcWorld record itself and never gets them through this type: modifiers are
    /// not the identity of an ordinary body, and this type speaks for ordinary bodies.
    /// </para>
    /// </summary>
    public record NpcDefinitionOverrides
    {
        public int? Level { get; init; }
        public Rarity? Rarity { get; init; }
        public Stance? Stance { get; init; }
    }
}
