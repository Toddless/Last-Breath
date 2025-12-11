namespace Battle.Source.CombatEvents
{
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record AbilityActivatedEvent(IEntity Source, IAbility Ability) : ICombatEvent
    {

    }
}
