namespace Core.World.Interactions
{
    using System;
    using MessageBus;

    public readonly record struct InteractionHandle(string LocationId, string ObjectId, Guid Binding);
    public record InteractionAction(string Id, string LabelKey, bool Enabled = true, string? ReasonKey = null, bool ExplicitChoice = false);

    /// <summary>How an interaction request ended.</summary>
    public enum InteractionOutcome
    {
        /// <summary>The action's effect is done when the request returns.</summary>
        Completed,

        /// <summary>The action handed over to a session or its own process that keeps running after the request.</summary>
        Started,

        /// <summary>The action did not happen; a reason key, when set, says why.</summary>
        Refused
    }

    public record InteractionResult(InteractionOutcome Outcome, string? ReasonKey = null)
    {
        public static readonly InteractionResult Completed = new(InteractionOutcome.Completed);
        public static readonly InteractionResult Started = new(InteractionOutcome.Started);
        public static readonly InteractionResult Unavailable = new(InteractionOutcome.Refused, "UI_Interaction_Unavailable");

        /// <summary>True for Completed and Started.</summary>
        public bool Success => Outcome != InteractionOutcome.Refused;

        /// <summary>Completed when the operation succeeded, refused otherwise.</summary>
        public InteractionResult(bool success, string? reasonKey = null)
            : this(success ? InteractionOutcome.Completed : InteractionOutcome.Refused, reasonKey)
        {
        }
    }
    public record ExecuteInteractionRequest(InteractionHandle Target, string ActionId) : IRequest<InteractionResult>;
    public record ContainerTransferRequest(InteractionHandle Target, string? SlotId = null) : IRequest<InteractionResult>;

    /// <summary>Why the interaction service ended a session.</summary>
    public enum InteractionSessionEndCause
    {
        /// <summary>Another session, or an action taking over from it, replaced the session.</summary>
        Replaced,

        /// <summary>The target was freed, left the tree, was queued for deletion or unregistered, or became unavailable.</summary>
        TargetLost,

        /// <summary>The actor is gone or cannot act: dead, fighting, travelling, loading, outside the world UI or held by another blocking window.</summary>
        ActorUnavailable,

        /// <summary>The actor no longer reaches the target: another space, the distance or an obstacle.</summary>
        OutOfReach,

        /// <summary>Code ended the session without a more specific cause.</summary>
        Cancelled
    }

    public static class InteractionActions
    {
        public const string Interact = "interact";
        public const string TakeAll = "container_take_all";
        public const string Talk = "talk";
        public const string Attack = "attack";
        public const string Open = "open";
        public const string Travel = "travel";
    }
}
