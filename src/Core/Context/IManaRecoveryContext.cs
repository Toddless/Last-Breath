namespace Core.Context
{
    using Entity;
    using Enums;

    /// <summary>Any mana gain, regardless of source (regen, refunds, on-attack restores), goes through this context.</summary>
    public interface IManaRecoveryContext
    {
        IFightable Source { get; }
        IFightable Target { get; }
        float Amount { get; set; }
        RecoveryCause Cause { get; set; }
    }
}
