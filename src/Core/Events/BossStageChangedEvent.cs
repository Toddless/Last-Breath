namespace Core.Events
{
    using Battle;
    using Entity;

    /// <summary>
    /// The boss finished its stage transformation (effects stripped, parameters at the new stage,
    /// vitals fully restored). Recorded at resolve; the director's future transformation beat
    /// reacts at show time.
    /// </summary>
    public record BossStageChangedEvent(IFightable Boss, int Stage) : ICombatEvent, IBattleEvent;
}
