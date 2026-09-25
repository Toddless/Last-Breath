namespace Core.Ai.World
{
    using System;
    using Entity.Components;

    /// <summary>
    /// The "lifecycle" section of a peaceful key resident; defaults are the designed placeholders
    /// (a knocked-down villager is back on their feet within minutes, not hours).
    /// </summary>
    public class VillagerLifecycleConfig
    {
        private const float DefaultRecoverMinSeconds = 30f;
        private const float DefaultRecoverMaxSeconds = 180f;

        public float RecoverMinSeconds { get; init; } = DefaultRecoverMinSeconds;
        public float RecoverMaxSeconds { get; init; } = DefaultRecoverMaxSeconds;
    }

    /// <summary>
    /// Post-defeat fate of a peaceful key resident (a smith, a trader): the body lies for a rolled
    /// delay and then stands up ALIVE — same creature, same faction, no strength gained. Such a body
    /// cannot be burned and the cycle has no final death, so a settlement keeps its people however
    /// often they are knocked down. Pure logic — the node ticks it and reacts to the event.
    /// </summary>
    public class VillagerLifecycle(VillagerLifecycleConfig config, IRandomNumberGenerator rnd) : IAliveRiseLifecycle
    {
        private readonly BodyRiseTimer _timer = new(rnd);

        public NpcLifeStage Stage { get; private set; } = NpcLifeStage.Alive;

        /// <summary>The rolled recovery delay of the current Defeated stage (save system reads it).</summary>
        public float ResurrectDelay => _timer.Delay;

        /// <summary>Seconds already lain of the current Defeated stage (save system reads it).</summary>
        public float Elapsed => _timer.Elapsed;

        /// <summary>Always false: a resident body is never burned, however long it lies.</summary>
        public bool CanBeBurned => false;

        public event Action? ReviveReady;

        /// <summary>
        /// The first health zero-out: the body goes down and the recovery timer starts.
        /// <paramref name="isUndead"/> cannot change the outcome — a resident is never undead and
        /// never rises as one, so both values start the same timer. A repeated call while the body
        /// already lies changes nothing, the running timer included.
        /// </summary>
        public void OnDefeated(bool isUndead)
        {
            if (Stage != NpcLifeStage.Alive) return;

            Stage = NpcLifeStage.Defeated;
            _timer.Roll(config.RecoverMinSeconds, config.RecoverMaxSeconds);
        }

        /// <summary>
        /// Save-load path: puts a freshly built body back into lying with its timer. The cycle has a
        /// single lying stage, so any saved stage other than Alive means "lies and recovers" —
        /// a body saved as dormant or burned by an older cycle gets up instead of lying forever.
        /// </summary>
        public void RestoreState(NpcLifeStage stage, float resurrectDelay, float elapsed)
        {
            if (stage == NpcLifeStage.Alive) return;

            Stage = NpcLifeStage.Defeated;
            _timer.Restore(resurrectDelay, elapsed);
        }

        public void Tick(float delta)
        {
            if (Stage != NpcLifeStage.Defeated) return;
            if (!_timer.Advance(delta)) return;

            Stage = NpcLifeStage.Alive;
            ReviveReady?.Invoke();
        }

        /// <summary>Always refuses: there is no fire that keeps a key resident down for good.</summary>
        public bool TryBurn() => false;
    }
}
