namespace Core.Events
{
    using Battle;
    using Entity;

    /// <summary>
    /// A summoning ability asks the battlefield for reinforcements. Published on the caster's
    /// combat bus at resolve time; the arena's SummonService executes it (spawn, dynamic slot,
    /// the summoner's group). Count tops the pack up to <paramref name="MaxAlivePerSummoner"/> —
    /// a recast refills the fallen, never stacks past the cap.
    /// </summary>
    public record SummonRequestedEvent(
        IFightable Summoner,
        string NpcId,
        int Count,
        int MaxAlivePerSummoner,
        float StatShare) : ICombatEvent;
}
