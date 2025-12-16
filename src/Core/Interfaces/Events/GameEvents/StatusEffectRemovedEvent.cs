namespace Core.Interfaces.Events.GameEvents
{
    using Enums;
    using Battle;

    public record StatusEffectRemovedEvent( StatusEffects RemovedEffect) : ICombatEvent
    {

    }
}
