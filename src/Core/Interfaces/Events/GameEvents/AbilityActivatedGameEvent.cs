namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;

    public record AbilityActivatedGameEvent(IAbility Ability) : ICombatEvent, IGameEvent
    {
    }
}
