namespace Core.Events
{
    using Battle;
    using Enums;

    public record StatusEffectRemovedEvent( StatusEffects RemovedEffect) : ICombatEvent
    {

    }
}
