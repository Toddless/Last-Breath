namespace Core.Ai.World
{
    using System;
    using Entity.Components;

    public enum NpcLifeStage : byte
    {
        Alive,

        /// <summary>A non-undead body: the resurrection timer is running unless it gets burned.</summary>
        Defeated,

        // TODO:
        // Дополнить состояние цикла. Нежить через некоторое время восстает обратно.
        /// <summary>A defeated undead ("anabiosis"): lies indefinitely, only burning finishes it.</summary>
        Dormant,

        /// <summary>Burned. The spawn point that owned this NPC generates a replacement.</summary>
        FinalDead
    }

    /// <summary>The "lifecycle" section of Npc.json; defaults follow the design (1–10 minutes).</summary>
    public class NpcLifecycleConfig
    {
        public float ResurrectMinSeconds { get; init; } = 60f;
        public float ResurrectMaxSeconds { get; init; } = 600f;

        /// <summary>Parameter bonus of a maximum-timer rising (fraction, 1 = +100%).</summary>
        public float MaxStrengthBonus { get; init; } = 1f;
    }

    /// <summary>
    /// Post-defeat fate of an NPC body: humans/animals rise as undead after a rolled delay
    /// (the longer it takes, the stronger the rising), undead lie dormant; burning is final
    /// and the only way to keep a body down. Pure logic — the node ticks it and reacts to events.
    /// </summary>
    public class NpcLifecycle(NpcLifecycleConfig config, IRandomNumberGenerator rnd) : INpcLifecycle
    {
        public NpcLifeStage Stage { get; private set; } = NpcLifeStage.Alive;

        /// <summary>The rolled rise delay of the current Defeated stage (save system reads it).</summary>
        public float ResurrectDelay { get; private set; }

        /// <summary>Seconds already lain of the current Defeated stage (save system reads it).</summary>
        public float Elapsed { get; private set; }

        /// <summary>
        /// Fired once when the timer completes; the argument is the parameter bonus of the rising
        /// (StrengthFraction × MaxStrengthBonus, e.g. 0.4 = +40% to the boosted parameters).
        /// </summary>
        public event Action<float>? ResurrectionReady;

        /// <summary>The first health zero-out. Undead go dormant, everyone else starts the rise timer.</summary>
        public void OnDefeated(bool isUndead)
        {
            if (Stage != NpcLifeStage.Alive) return;

            if (isUndead)
            {
                Stage = NpcLifeStage.Dormant;
                return;
            }

            Stage = NpcLifeStage.Defeated;
            Elapsed = 0;
            ResurrectDelay = rnd.RandFloatRange(config.ResurrectMinSeconds, config.ResurrectMaxSeconds);
        }

        /// <summary>Save-load path: puts a freshly built body straight into a lying stage with its timer.</summary>
        public void RestoreState(NpcLifeStage stage, float resurrectDelay, float elapsed)
        {
            if (stage is not (NpcLifeStage.Defeated or NpcLifeStage.Dormant)) return;
            Stage = stage;
            ResurrectDelay = resurrectDelay;
            Elapsed = elapsed;
        }

        public void Tick(float delta)
        {
            if (Stage != NpcLifeStage.Defeated) return;

            Elapsed += delta;
            if (Elapsed < ResurrectDelay) return;

            Stage = NpcLifeStage.Alive;
            ResurrectionReady?.Invoke(StrengthFraction() * config.MaxStrengthBonus);
        }

        /// <summary>True while the body lies and can be burned (both fresh corpses and dormant undead).</summary>
        public bool CanBeBurned => Stage is NpcLifeStage.Defeated or NpcLifeStage.Dormant;

        public bool TryBurn()
        {
            if (!CanBeBurned) return false;
            Stage = NpcLifeStage.FinalDead;
            return true;
        }

        /// <summary>How strong the rising is: 0 at the minimum possible delay, 1 at the maximum.</summary>
        public float StrengthFraction()
        {
            float range = config.ResurrectMaxSeconds - config.ResurrectMinSeconds;
            return range <= 0 ? 1f : Math.Clamp((ResurrectDelay - config.ResurrectMinSeconds) / range, 0f, 1f);
        }
    }
}
