namespace Core.Data.NpcData
{
    using Enums;

    /// <summary>
    /// Pins identity facts of a definition instead of rolling them — the save system rebuilds
    /// corpses/risen undead with their original level/rarity/stance (abilities and NPC modifiers
    /// re-roll: acceptable fidelity loss, they are not identity).
    /// </summary>
    public record NpcDefinitionOverrides
    {
        public int? Level { get; init; }
        public Rarity? Rarity { get; init; }
        public Stance? Stance { get; init; }
    }
}
