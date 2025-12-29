namespace Core.Interfaces.Events.GameEvents
{
    using Enums;

    public record PlayerChangesStanceEvent(Stance Stance) : IBattleEvent;
}
