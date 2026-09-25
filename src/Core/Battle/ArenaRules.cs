namespace Core.Battle
{
    /// <summary>Parsed arena rules (see CombatRules.json, "arena" section).</summary>
    public record ArenaRules(int MaxBattleSlots)
    {
        /// <summary>Fallback for projects that don't register a rules provider (Main lags Battle).</summary>
        public static readonly ArenaRules Default = new(10);
    }
}
