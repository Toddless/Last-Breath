namespace Core.Context
{
    using Entity;

    public record ManaRecoveryContext(IFightable Source, IFightable Target) : IManaRecoveryContext
    {
        public float Amount { get; set; }
    }
}
