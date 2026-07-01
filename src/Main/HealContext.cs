namespace LastBreath
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;

    public record HealContext(IEntity Source, IEntity Target) : IHealContext
    {
        public float Amount { get; set; }
        public bool ConvertToDamage { get; set; }
        public HealCause Cause { get; set; }
    }
}
