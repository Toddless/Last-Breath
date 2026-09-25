namespace Core.Events
{
    using Enums;

    public record PlayerChangesStanceEvent(Stance Stance) : IBattleEvent;
}
