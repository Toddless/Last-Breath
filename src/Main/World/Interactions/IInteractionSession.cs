namespace LastBreath.World.Interactions
{
    using Core.Views.UI;
    using Core.World.Interactions;

    /// <summary>An interaction bound to one target that the interaction service keeps open until it ends.</summary>
    public interface IInteractionSession
    {
        /// <summary>Target the session is bound to.</summary>
        InteractionTarget Target { get; }

        /// <summary>UI window the session owns, never counted as a blocking window against its own session; null for a session without UI.</summary>
        IWindow? Window { get; }

        /// <summary>Whether the session is still open on its own terms.</summary>
        bool IsOpen { get; }

        /// <summary>Ends the session for the cause and closes its own UI without calling back into the service; later calls do nothing.</summary>
        void End(InteractionSessionEndCause cause);
    }
}
