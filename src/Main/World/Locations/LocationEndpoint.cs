namespace LastBreath.World.Locations
{
    using Core.MessageBus;
    using Core.Services;
    using Core.World.Locations;
    using Godot;
    using Services;

    [GlobalClass]
    public partial class LocationEndpoint : Node2D
    {
        public const string InteractAction = "interact";
        [Export] public string EndpointId { get; set; } = "";
        [Export] public float InteractionRadius { get; set; } = 180;
        public Marker2D Arrival => GetNode<Marker2D>("Arrival");

        public bool InReach(Node2D player) => Core.World.Spaces.SpatialAccess.SharesSpace(this, player)
            && player.GlobalPosition.DistanceTo(GlobalPosition) <= InteractionRadius;

        public override void _UnhandledInput(InputEvent e)
        {
            if (!e.IsActionPressed(InteractAction) || e.IsEcho()) return;
            var provider = Services.GameServiceProvider.Instance;
            if (provider.GetService<IPlayerAccessor>().Player is not Node2D player || !InReach(player)) return;
            if (LocationRoot.Find(this) is not { IsPreparing: false } root) return;
            GetViewport().SetInputAsHandled();
            _ = provider.GetService<IGameMessageBus>().SendRequest<TravelRequest, TravelResult>(new(root.LocationId, EndpointId));
        }
    }
}
