namespace Core.Entity
{
    using Enums;

    public interface IPlayer : IFightable
    {
        Fractions Fractions { get; }
        string PlayerName { get; }
    }
}
