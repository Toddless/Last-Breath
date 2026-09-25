namespace Core.Narrative.Facts
{
    using Events;

    /// <summary>Flags discovered locations in the facts registry. The marker publishes once per
    /// playthrough; SetFact is idempotent anyway, so a duplicate event is harmless.</summary>
    public class LocationFactTracker
    {
        private readonly IWorldFactsService _facts;

        public LocationFactTracker(IGameEventBus gameEventBus, IWorldFactsService facts)
        {
            _facts = facts;
            gameEventBus.Subscribe<LocationDiscoveredEvent>(evnt => _facts.SetFact(FactKeys.LocationDiscovered(evnt.LocationId)));
        }
    }
}
