namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public record DamageContext : IDamageContext
    {
        public required IFightable Source { get; init; }
        public float Damage { get; set; }
        public DamageType Type { get; set; }
        public DamageCause Cause { get; set; }
        public bool IsCrit { get; set; } = false;
        public float AbsorbedByBarrier { get; set; }
    }
}
