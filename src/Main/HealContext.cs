namespace LastBreath
{
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Interfaces;

    public record HealContext(IFightable Source, IFightable Target) : IHealContext
    {
        public float Amount { get; set; }
        public bool ConvertToDamage { get; set; }
        public HealCause Cause { get; set; }
    }
}
