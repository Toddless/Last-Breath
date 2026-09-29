namespace LastBreath.Tests
{
    using Core.Views.UI;
    using Core.World.Interactions;
    using World.Interactions;

    /// <summary>Session without UI that keeps the cause it was first ended for.</summary>
    public sealed class RecordingSession(InteractionTarget target) : IInteractionSession
    {
        public InteractionTarget Target { get; } = target;

        public IWindow? Window => null;

        public bool IsOpen => EndCause == null;

        /// <summary>Cause of the first <see cref="End"/>; null while the session is open.</summary>
        public InteractionSessionEndCause? EndCause { get; private set; }

        public void End(InteractionSessionEndCause cause) => EndCause ??= cause;
    }
}
