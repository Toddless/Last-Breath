namespace Core.Services
{
    using System;
    using System.Threading;
    using Events;

    /// <summary>
    /// Counts every spawn-point NPC alive in the world. A reservation survives faction changes
    /// (the risen undead still walks the map) and frees itself on the final death event —
    /// wild risen NPCs release their slot without any spawn point owning them.
    /// </summary>
    public class NpcPopulationService : INpcPopulationService, IDisposable, Session.ISessionResettable
    {
        private const int DefaultGlobalLimit = 20;

        private readonly IGameEventBus _gameEventBus;
        private readonly Lock _sync = new();

        public NpcPopulationService(IGameEventBus gameEventBus)
        {
            _gameEventBus = gameEventBus;
            _gameEventBus.Subscribe<NpcFinalDeathEvent>(OnFinalDeath);
        }

        public int GlobalLimit { get; set; } = DefaultGlobalLimit;
        public int CurrentCount { get; private set; }

        public bool TryReserve()
        {
            lock (_sync)
            {
                if (CurrentCount >= GlobalLimit) return false;
                CurrentCount++;
                return true;
            }
        }

        public void ReserveOutsideLimit()
        {
            lock (_sync)
            {
                CurrentCount++;
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                CurrentCount = 0;
            }
        }

        /// <summary>The old scene's NPC nodes die without final-death events; the counter must not survive them.</summary>
        public void ResetSession() => Reset();

        public void Dispose() => _gameEventBus.Unsubscribe<NpcFinalDeathEvent>(OnFinalDeath);

        private void OnFinalDeath(NpcFinalDeathEvent _)
        {
            lock (_sync)
            {
                if (CurrentCount > 0) CurrentCount--;
            }
        }
    }
}
