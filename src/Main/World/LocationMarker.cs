namespace LastBreath.World
{
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Narrative.Facts;
    using Godot;
    using Services;

    /// <summary>
    /// A discoverable location: the player walking into the area flags it in the world facts —
    /// no markers, no compass, the old-school way. Fires once per playthrough (the facts
    /// registry is checked, so a rediscovery after load stays silent).
    /// </summary>
    [GlobalClass]
    public partial class LocationMarker : Area2D
    {
        [Export] private string _locationId = string.Empty;
        private IWorldFactsService? _facts;
        private IGameEventBus? _events;

        public override void _Ready()
        {
            _facts = GameServiceProvider.Instance.GetService<IWorldFactsService>();
            _events = GameServiceProvider.Instance.GetService<IGameEventBus>();
            BodyEntered += OnBodyEntered;
        }

        private void OnBodyEntered(Node2D body)
        {
            if (_locationId.Length == 0 || body is not IPlayer) return;
            if (_facts!.IsSet(FactKeys.LocationDiscovered(_locationId))) return;

            _events!.Publish(new LocationDiscoveredEvent(_locationId));
        }
    }
}
