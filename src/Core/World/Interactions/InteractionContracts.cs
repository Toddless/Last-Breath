namespace Core.World.Interactions
{
    using System;
    using MessageBus;

    public readonly record struct InteractionHandle(string LocationId, string ObjectId, Guid Binding);
    public record InteractionAction(string Id, string LabelKey, bool Enabled = true, string? ReasonKey = null, bool ExplicitChoice = false);

    /// <summary>How an interaction request ended.</summary>
    public enum InteractionOutcome
    {
        /// <summary>The action's effect is done when the request returns; when only part of it happened, a reason key says why the rest did not.</summary>
        Completed,

        /// <summary>The action handed over to a session or its own process that keeps running after the request.</summary>
        Started,

        /// <summary>The action did not happen; a reason key, when set, says why.</summary>
        Refused
    }

    /// <summary>How a request ended; a reason key is what the player is told after a command that ran.</summary>
    public record InteractionResult(InteractionOutcome Outcome, string? ReasonKey = null)
    {
        public static readonly InteractionResult Completed = new(InteractionOutcome.Completed);
        public static readonly InteractionResult Started = new(InteractionOutcome.Started);
        public static readonly InteractionResult Unavailable = new(InteractionOutcome.Refused, InteractionReasonKeys.Unavailable);

        /// <summary>Refused without a reason: the command was dropped before it ran, so the player is told nothing.</summary>
        public static readonly InteractionResult Dropped = new(InteractionOutcome.Refused);

        /// <summary>True for Completed and Started.</summary>
        public bool Success => Outcome is InteractionOutcome.Completed or InteractionOutcome.Started;
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

    /// <summary>Localization keys of the reasons an interaction result carries.</summary>
    public static class InteractionReasonKeys
    {
        /// <summary>The action cannot run now.</summary>
        public const string Unavailable = "UI_Interaction_Unavailable";

        /// <summary>The bag is full; what did not fit stays in the container.</summary>
        public const string ContainerFull = "UI_Container_Full";
    }

    /// <summary>Object IDs of interaction targets: a prefix per owner kind keeps their IDs apart within a location.</summary>
    public static class InteractionIds
    {
        private const string ChestPrefix = "chest/";
        private const string EndpointPrefix = "endpoint/";
        private const string NpcPrefix = "npc/";

        /// <summary>ID of a chest's target, from the chest's object ID; empty for a blank one.</summary>
        public static string Chest(string? objectId) => Compose(ChestPrefix, objectId);

        /// <summary>ID of a location endpoint's target, from its endpoint ID; empty for a blank one.</summary>
        public static string Endpoint(string? endpointId) => Compose(EndpointPrefix, endpointId);

        /// <summary>ID of an NPC's target, from the NPC's instance ID; empty for a blank one.</summary>
        public static string Npc(string? instanceId) => Compose(NpcPrefix, instanceId);

        /// <summary>The prefix followed by the raw ID; empty for a blank raw ID, which no target registers under.</summary>
        private static string Compose(string prefix, string? rawId) => string.IsNullOrWhiteSpace(rawId) ? string.Empty : prefix + rawId;
    }
}
