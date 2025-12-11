namespace Battle.Source.CombatEvents
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record TurnEndEvent(IEntity Source) : ICombatEvent
    {
    }
}
