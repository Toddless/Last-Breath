namespace Core.World.Interactions
{
    using System;
    using MessageBus;

    public readonly record struct InteractionHandle(string LocationId, string ObjectId, Guid Binding);
    public record InteractionAction(string Id, string LabelKey, bool Enabled = true, string? ReasonKey = null, bool ExplicitChoice = false);
    public record InteractionResult(bool Success, string? ReasonKey = null)
    {
        public static readonly InteractionResult Completed = new(true);
        public static readonly InteractionResult Unavailable = new(false, "UI_Interaction_Unavailable");
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
