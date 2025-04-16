namespace Playground.Components.Interfaces
{
    public interface IRecoveryRule
    {
        bool ShouldRecover(RecoveryEventContext context);
    }
}
