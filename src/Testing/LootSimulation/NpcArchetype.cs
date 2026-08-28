namespace LastBreathTest.LootSimulation
{
    using Core.Enums;

    /// <summary>One simulated NPC configuration. Modifier composition follows the game's
    /// NpcProvider.RollModifiers paths (issue #223): <see cref="RandomModifierCount"/> is the
    /// AUTHORED absolute — exactly that many slots, no dice on the count, the game's boss/villager
    /// path; <see cref="CascadeRolled"/> is the rolled spawn — the type × rarity formula gives the
    /// slot ceiling and each slot fills at the falling chance of the NpcSpawnRolls catalog.
    /// <see cref="ModifierIds"/> is a lab fixture with no game analog: a fixed set for isolating one
    /// modifier per scenario; it stacks on top of either rolled path.</summary>
    internal sealed record NpcArchetype(
        string Name,
        EntityType EntityType,
        Rarity Rarity,
        int Level,
        Fractions Fraction)
    {
        public IReadOnlyList<string> ModifierIds { get; init; } = [];

        /// <summary>Authored absolute modifier count (0 = none authored). Mirrors the "authored"
        /// section of an Npc.json record: the count rolls no dice, only the composition does.</summary>
        public int RandomModifierCount { get; init; }

        /// <summary>When true (and no authored count), how MANY modifiers the spawn carries is the
        /// slot cascade of issue #223 — the post-rework reality of every non-authored spawn.</summary>
        public bool CascadeRolled { get; init; }
    }
}
