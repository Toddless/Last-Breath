namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enemy;
    using Playground.Script.Enums;

    public class ManaRecoveryRule : IRecoveryRule
    {
        public bool ShouldRecover(RecoveryEventContext context) => context.Type == RecoveryEventType.OnTurnEnd && context.Character.Stance == Stance.Intelligence;
    }
}
