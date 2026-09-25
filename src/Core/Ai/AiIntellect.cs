namespace Core.Ai
{
    /// <summary>
    /// Planner grade. An explicit profile field (EntityType only suggests a default):
    /// Simple = profile weights + noise, single cast; Tactical = full considerations;
    /// Mastermind = considerations with reduced noise and doubled kill-secure pressure.
    /// </summary>
    public enum AiIntellect : byte
    {
        Simple,
        Tactical,
        Mastermind
    }
}
