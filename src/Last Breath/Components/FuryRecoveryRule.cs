namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;

    public class FuryRecoveryRule : IRecoveryRule
    {
        public bool ShouldRecover(RecoveryEventContext context) => context.Type == RecoveryEventType.OnDamageTaken && context.Character.Stance == Stance.Strength;
    }
}
