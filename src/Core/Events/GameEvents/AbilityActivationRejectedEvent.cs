namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;

    /// <summary>Published when an ability activation was blocked by the gate (e.g. the owner is paralyzed). UI feedback hook.</summary>
    public record AbilityActivationRejectedEvent(IAbility Ability) : ICombatEvent;
}
