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

        /// <summary>Amount of this hit that was soaked by the target's barrier. Health damage = <see cref="Damage"/> - this.</summary>
        float AbsorbedByBarrier { get; set; }
    }
}
