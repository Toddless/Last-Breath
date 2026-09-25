namespace Battle.Source.UIElements
{
    /// <summary>Filterable category of one battle-log line. System lines (turn markers) are always shown.</summary>
    public enum BattleLogCategory
    {
        System,
        Damage,
        AttackResult,
        Heal,
        Ability,
        Effect
    }

    /// <summary>One ready-to-render line of the battle log (BBCode).</summary>
    public record BattleLogEntry(BattleLogCategory Category, string Text);
}
