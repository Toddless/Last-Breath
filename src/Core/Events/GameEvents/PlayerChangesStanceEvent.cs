namespace Core.Events.GameEvents
{
    using Enums;

    public record PlayerChangesStanceEvent(Stance Stance) : IBattleEvent;
}
