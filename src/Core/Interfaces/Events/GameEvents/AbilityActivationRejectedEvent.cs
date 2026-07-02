namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    /// <summary>Published when an ability activation was blocked by the gate (e.g. the owner is paralyzed). UI feedback hook.</summary>
    public record AbilityActivationRejectedEvent(IAbility Ability) : ICombatEvent;
}
