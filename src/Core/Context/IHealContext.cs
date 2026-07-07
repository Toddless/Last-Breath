namespace Core.Context
{
    using Enums;
    using Entity;

    public interface IHealContext
    {
        IFightable Source { get; }
        IFightable Target { get; }
        float Amount { get; set; }
        bool ConvertToDamage { get; set; }
        HealCause Cause { get; set; }
    }
}
