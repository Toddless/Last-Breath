namespace Core.Context
{
    using Entity;
    using Enums;
    using Interfaces;

    public record HealContext(IFightable Source, IFightable Target) : IHealContext
    {
        public float Amount { get; set; }
        public bool ConvertToDamage { get; set; }
        public HealCause Cause { get; set; }
    }
}
