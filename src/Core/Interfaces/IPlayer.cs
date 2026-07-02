namespace Core.Interfaces
{
    using Entity;

    public interface IPlayer : IFightable
    {
        string Name { get; }
    }
}
