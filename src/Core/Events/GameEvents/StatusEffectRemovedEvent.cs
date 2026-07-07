namespace Core.Events.GameEvents
{
    using Battle;
    using Enums;

    public record StatusEffectRemovedEvent( StatusEffects RemovedEffect) : ICombatEvent
    {

    }
}
