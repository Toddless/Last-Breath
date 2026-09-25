namespace Core.Battle
{
    /// <summary>
    /// Parsed exhaustion rules (see CombatRules.json). Exhaustion limits the "ability rain" within a
    /// single turn: every activation stacks a surcharge on all ability costs, the decay forgives a
    /// typical turn entirely — only a real burst accumulates. Applies to every combatant, NPC included.
    /// </summary>
    public record ExhaustionRules(float CostIncreasePerStack, int DecayPerTurn)
    {
        public static readonly ExhaustionRules Disabled = new(0f, int.MaxValue);
    }
}
