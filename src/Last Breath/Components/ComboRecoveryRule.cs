namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;

    public class ComboRecoveryRule() : IRecoveryRule
    {
        public bool ShouldRecover(RecoveryEventContext context) => context.Type == RecoveryEventType.OnHit && context.Character.Stance == Stance.Dexterity;
    }
}
