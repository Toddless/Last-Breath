namespace Core.Events
{
    using Battle;
    using Entity;

    /// <summary>
    /// A fighter's exhaustion count changed. <paramref name="Stacks"/> is the new count and
    /// <paramref name="Surcharge"/> the fraction it adds to every ability cost (0.75 = +75%);
    /// both are carried explicitly so the UI never resolves live state to draw the readout.
    /// Published on the bearer's combat bus, so the timeline records it and the replay shows it
    /// at the same moment as the cast that produced it.
    /// </summary>
    public record ExhaustionChangedEvent(IFightable Fighter, int Stacks, float Surcharge) : ICombatEvent, IBattleEvent;
}
