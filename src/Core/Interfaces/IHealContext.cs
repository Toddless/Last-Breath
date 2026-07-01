namespace Core.Interfaces
{
    using Entity;
    using Enums;

    public interface IHealContext
    {
        IEntity Source { get; }
        IEntity Target { get; }
        float Amount { get; set; }
        bool ConvertToDamage { get; set; }
        HealCause Cause { get; set; }
    }
}
