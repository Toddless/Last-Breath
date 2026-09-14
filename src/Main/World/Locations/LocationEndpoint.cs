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
    public partial class LocationEndpoint : Node2D, IInteractionSource
    {
        public const string InteractAction = InteractionActions.Interact;
        [Export] public string EndpointId { get; set; } = "";
        [Export] public float InteractionRadius { get; set; } = 180;
        public Marker2D Arrival => GetNode<Marker2D>("Arrival");

        public override void _Ready()
        {
            var target = GD.Load<PackedScene>("res://World/Interactions/InteractionTarget.tscn").Instantiate<InteractionTarget>();
            target.Name = "InteractionTarget";
            target.ObjectId = "endpoint/" + EndpointId;
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
