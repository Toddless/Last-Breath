namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Enums;

    public record StatusEffectRemovedEvent( StatusEffects RemovedEffect) : ICombatEvent
    {

    }
}
