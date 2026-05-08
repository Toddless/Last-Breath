namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    public record AbilityActivatedEvent(IAbility Ability) : IBattleEvent, ICombatEvent, IGameEvent;
}
