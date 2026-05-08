namespace Core.Interfaces
{
    using Entity;

    public interface IHealContext
    {
        IEntity Source { get; }
        IEntity Target { get; }
        float Amount { get; set; }
        bool ConvertToDamage { get; set; }
    }
}
