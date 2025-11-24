namespace Core.Interfaces.Entity
{
    using Components;

    public interface IPlayer
    {
        IEntityAttribute Dexterity { get; }
        IEntityAttribute Strength { get; }
        IEntityAttribute Intelligence { get; }
    }
}
