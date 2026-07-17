namespace Core.Enums
{
    /// <summary>The two boss categories (Боссы.md → Механика → Респавн боссов).</summary>
    public enum BossRespawnMode
    {
        /// <summary>Killed once — gone forever (gated by the final-death world fact).</summary>
        Single,

        /// <summary>Returns after every N combat deaths of his faction's members.</summary>
        FactionDeaths
    }
}
