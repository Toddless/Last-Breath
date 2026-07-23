namespace Core.Context
{
    using Entity;
    using Enums;

    public record HealContext(IFightable Source, IFightable Target) : IHealContext
    {
        public float Amount { get; set; }
        public bool ConvertToDamage { get; set; }
        public RecoveryCause Cause { get; set; }
    }
}
