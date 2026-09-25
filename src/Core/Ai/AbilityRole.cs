namespace Core.Ai
{
    /// <summary>
    /// How the AI treats an ability. Abilities are opaque to the planner, so the role
    /// comes from behavior data and selects the target ranking and considerations.
    /// </summary>
    public enum AbilityRole : byte
    {
        Damage,
        Buff,
        Debuff,
        Control,
        Heal,
        Utility
    }
}
