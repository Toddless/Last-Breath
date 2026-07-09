namespace LastBreathTest.LootSimulation
{
    using Core.Enums;

    /// <summary>One simulated NPC configuration. Modifiers come from fixed ids, a random weighted
    /// roll of N modifiers (as the game spawner does), or both.</summary>
    internal sealed record NpcArchetype(
        string Name,
        EntityType EntityType,
        Rarity Rarity,
        int Level,
        Fractions Fraction)
    {
        public IReadOnlyList<string> ModifierIds { get; init; } = [];
        public int RandomModifierCount { get; init; }
    }
}
