namespace Core.Interfaces
{
    using Entity;
    using Enums;

    public interface IDamageContext
    {
        IEntity Source { get; }
        float Damage { get; set; }
        DamageType Type { get; set; }
        DamageCause Cause { get; set; }
        bool IsCrit { get; set; }
    }
}
