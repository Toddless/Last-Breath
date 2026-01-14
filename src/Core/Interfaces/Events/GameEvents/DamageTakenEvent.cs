namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;
    using Enums;

    public record DamageTakenEvent(IEntity From, IEntity DamageTaken, float Damage, DamageType Type, DamageSource Source, bool IsCritical = false) : ICombatEvent, IBattleEvent;
}
