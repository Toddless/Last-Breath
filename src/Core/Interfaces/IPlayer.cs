namespace Core.Interfaces
{
    using Entity;

    public interface IPlayer : IEntity
    {
        string Name { get; }
    }
}
