namespace Core.Data
{
    using Enums;

    /// <summary>
    /// How likely one more slot of a rolled NPC is to be filled. The type × rarity tables of
    /// <see cref="NpcData.NpcTypeDefaults"/> answer how many slots EXIST — this answers how many of
    /// them a given spawn actually walks away with, and it is the only reason two elites of the same
    /// rarity can come out of the same record wearing different numbers of modifiers.
    /// <para>
    /// A chance of 1 means the slot is not a question: the caller fills it and spends no dice on it,
    /// which is also what a type the catalog does not name falls back to (the behaviour that predates
    /// this catalog). The math lives here rather than at the call site because both callers — the
    /// modifier roll and the ability roll — ask the same question of two different tables.
    /// </para>
    /// </summary>
    public interface INpcSpawnRollsProvider
    {
        /// <summary>Chance that the modifier slot at <paramref name="slotIndex"/> (zero-based) is filled.</summary>
        float ModifierSlotChance(EntityType type, Rarity rarity, int slotIndex);

        /// <summary>Chance that the ability slot at <paramref name="slotIndex"/> (zero-based) is filled.</summary>
        float AbilitySlotChance(EntityType type, Rarity rarity, int slotIndex);
    }
}
