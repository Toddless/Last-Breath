namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    public record AbilityActivationEvent(IAbility Ability, string SelectionId) : ICombatEvent, IGameEvent, IBattleEvent;
}
