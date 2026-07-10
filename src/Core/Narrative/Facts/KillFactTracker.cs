namespace Core.Narrative.Facts
{
    using Entity;
    using Events;
    using Events.GameEvents;
    using Save;
    using Services;

    /// <summary>
    /// Writes player kills into the facts registry: one counter per NPC definition id, one per
    /// faction. Counts every death by the player's hand (not the final undead death) — a quest
    /// asks to kill, not to cremate. Nobody watching is fine: facts are memory, not reputation.
    /// </summary>
    public class KillFactTracker
    {
        private readonly IWorldFactsService _facts;
        private readonly IPlayerAccessor _playerAccessor;
        private readonly ILoadScope _loadScope;

        public KillFactTracker(IGameEventBus gameEventBus, IWorldFactsService facts, IPlayerAccessor playerAccessor, ILoadScope loadScope)
        {
            _facts = facts;
            _playerAccessor = playerAccessor;
            _loadScope = loadScope;
            gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        }

        private void OnEntityDied(EntityDiedEvent evnt)
        {
            if (_loadScope.IsLoading) return; // restored bodies drop health through the normal property
            if (evnt.Killer == null || !ReferenceEquals(evnt.Killer, _playerAccessor.Player)) return;
            if (evnt.Entity is not INpc npc) return;

            _facts.Add(FactKeys.KillCount(npc.Id));
            _facts.Add(FactKeys.FactionKillCount(npc.Fraction));
        }
    }
}
