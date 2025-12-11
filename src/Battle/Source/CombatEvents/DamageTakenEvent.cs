namespace Battle.Source.CombatEvents
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record DamageTakenEvent(IEntity Source, IAttackContext Context) : ICombatEvent
    {
    }
}
