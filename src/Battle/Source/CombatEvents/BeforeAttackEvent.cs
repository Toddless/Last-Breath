namespace Battle.Source.CombatEvents
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record BeforeAttackEvent(IEntity Source, IAttackContext Context) : ICombatEvent
    {
    }
}
