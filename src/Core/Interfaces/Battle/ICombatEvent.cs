namespace Core.Interfaces.Battle
{
    using Entity;

    public interface ICombatEvent
    {
        IEntity Source { get; }
    }
}
