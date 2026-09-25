namespace Core.Context
{
    using Entity;
    using Enums;

    public record ManaRecoveryContext(IFightable Source, IFightable Target) : IManaRecoveryContext
    {
        public float Amount { get; set; }
        public RecoveryCause Cause { get; set; }
    }
}
