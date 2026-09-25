namespace Core.Events
{
    using Battle;
    using Entity;

    /// <summary>
    /// The boss crossed its stage threshold: it is damage-immune until the transformation on the
    /// next turn start — the player's cue to finish the turn. Recorded at resolve; the director's
    /// future transition beat and the battle log react at show time.
    /// </summary>
    public record BossStageTransitionStartedEvent(IFightable Boss, int NextStage) : ICombatEvent, IBattleEvent;
}
