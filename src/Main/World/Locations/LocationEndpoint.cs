namespace LastBreath.World.Locations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.MessageBus;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Godot;
    using Interactions;

    [GlobalClass]
    public partial class LocationEndpoint : Node2D, IInteractionSource, IInteractionOwner
    {
        public const string InteractAction = InteractionActions.Interact;
        [Export] public string EndpointId { get; set; } = "";
        [Export] public float InteractionRadius { get; set; } = 180;
        public Marker2D Arrival => GetNode<Marker2D>("Arrival");
        /// <summary>ID of the endpoint's interaction target, composed from <see cref="EndpointId"/>; empty while EndpointId is blank.</summary>
        public string InteractionId => InteractionIds.Endpoint(EndpointId);
        /// <summary>Always true: an endpoint that leads nowhere already offers no actions.</summary>
        public bool IsInteractable => true;

        /// <summary>Adds the interaction target, which takes its identity from this endpoint when it enters the tree.</summary>
        public override void _Ready()
        {
            var target = GD.Load<PackedScene>("res://World/Interactions/InteractionTarget.tscn").Instantiate<InteractionTarget>();
            target.Name = "InteractionTarget";
            target.Reach = InteractionRadius;
            AddChild(target);
        }

        public bool InReach(Node2D player) => GetNodeOrNull<InteractionTarget>("InteractionTarget") is { } target
            && float.IsFinite(InteractionReach.DistanceSquared(player, target));

        public IEnumerable<InteractionAction> Actions()
        {
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
    }
}
