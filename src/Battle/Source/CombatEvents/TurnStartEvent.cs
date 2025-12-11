namespace Battle.Source.CombatEvents
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record TurnStartEvent(IEntity Source) : ICombatEvent
    {

    }
}
