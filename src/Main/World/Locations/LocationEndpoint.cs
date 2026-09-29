namespace LastBreath.World.Locations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.MessageBus;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Godot;
    using Interactions;

    [GlobalClass]
    public partial class LocationEndpoint : Node2D, IInteractionSource, IInteractionOwner
    {
        private const string ReportKind = "Endpoint";
        [Export] public string EndpointId { get; set; } = "";

        /// <summary>Interaction target of this endpoint, set in the scene as its direct child, so the endpoint both owns it and offers
        /// the actions through it; the target's reach is the travel reach. Any other setup is reported and leaves the endpoint disabled.</summary>
        [Export] public InteractionTarget Target { get; private set; } = null!;

        /// <summary>Whether the last check found the target set up; a disabled endpoint offers no actions and reaches no one.</summary>
        private bool _configured;
        private bool _configurationReported;
        public Marker2D Arrival => GetNode<Marker2D>("Arrival");
        /// <summary>ID of the endpoint's interaction target, composed from <see cref="EndpointId"/>; empty while EndpointId is blank.</summary>
        public string InteractionId => InteractionIds.Endpoint(EndpointId);
        /// <summary>Always true: an endpoint that leads nowhere or is disabled already offers no actions.</summary>
        public bool IsInteractable => true;

        /// <summary>Checks the interaction target setup before the target registers; a setup error disables the endpoint.</summary>
        public override void _EnterTree() => _configured = CheckTarget();

        /// <summary>True when the endpoint is set up and the player reaches its target.</summary>
        public bool InReach(Node2D player) => _configured && float.IsFinite(InteractionReach.DistanceSquared(player, Target));

        /// <summary>Travel while the endpoint is set up and the catalog connects it to a destination; nothing otherwise.</summary>
        public IEnumerable<InteractionAction> Actions()
        {
            if (!_configured) return [];
            var root = LocationRoot.Find(this);
            var catalog = Services.GameServiceProvider.Instance.GetService<LocationCatalog>();
            return root != null && catalog.Destination(new(root.LocationId, EndpointId)) != null
                ? [new(InteractionActions.Travel, "UI_Interaction_Travel")] : [];
        }

        public async Task<InteractionResult> Execute(string actionId)
        {
            if (actionId != InteractionActions.Travel || LocationRoot.Find(this) is not { } root) return InteractionResult.Unavailable;
            var bus = Services.GameServiceProvider.Instance.GetService<IGameMessageBus>();
            var result = await bus.SendRequest<TravelRequest, TravelResult>(new(root.LocationId, EndpointId));
            return result == TravelResult.Completed ? InteractionResult.Completed : InteractionResult.Unavailable;
        }

        /// <summary>True when the endpoint owns its interaction target and offers its actions through it; any other setup is reported
        /// and disables the endpoint.</summary>
        private bool CheckTarget()
        {
            if (FindTargetProblem() is not { } problem) return true;
            ReportConfiguration(problem);
            return false;
        }

        /// <summary>Why the target cannot serve this endpoint, described for the report: no target, a target that another node owns or
        /// no node owns, or a target the endpoint does not offer its actions through; null when it serves.</summary>
        private string? FindTargetProblem() => InteractionTargetSetup.FindProblem(this, Target, ReportKind, EndpointId);

        /// <summary>Writes an error that disables the endpoint to the log, once per node.</summary>
        private void ReportConfiguration(string message)
        {
            if (_configurationReported) return;
            _configurationReported = true;
            Tracker.TrackError(message, this);
        }
    }
}
