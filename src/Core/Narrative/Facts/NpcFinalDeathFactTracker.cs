namespace Core.Narrative.Facts
{
    using Events;
    using Save;

    /// <summary>
    /// Writes final deaths into the facts registry: one flag per NPC definition id. Final death is
    /// the point of no return (burning/permanent removal), so systems that must know a creature is
    /// gone for good — twin-boss assists, unique respawns — read this fact, not EntityDiedEvent.
    /// </summary>
    public class NpcFinalDeathFactTracker
    {
        private readonly IWorldFactsService _facts;
        private readonly ILoadScope _loadScope;

        public NpcFinalDeathFactTracker(IGameEventBus gameEventBus, IWorldFactsService facts, ILoadScope loadScope)
        {
            _facts = facts;
            _loadScope = loadScope;
            gameEventBus.Subscribe<NpcFinalDeathEvent>(OnNpcFinalDeath);
        }

        private void OnNpcFinalDeath(NpcFinalDeathEvent evnt)
        {
            if (_loadScope.IsLoading) return; // restored worlds replay despawns through the same event
            _facts.SetFact(FactKeys.NpcFinalDeath(evnt.NpcId));
        }
    }
}
