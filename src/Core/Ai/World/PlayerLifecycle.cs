namespace Core.Ai.World
{
    using System;
    using System.Collections.Generic;
    using Components;
    using Time;

    public enum PlayerLifeStage : byte
    {
        Alive,

        /// <summary>Lying at the defeat spot: the revive timer runs in ACCELERATED game time.</summary>
        Defeated,

        /// <summary>The corpse was burned by a passer-by — game over.</summary>
        FinalDead
    }

    /// <summary>The res://Data/Player/PlayerLifecycle.json values; defaults are the designed placeholders.</summary>
    public class PlayerLifecycleConfig
    {
        public float LieGameHours { get; init; } = 4f;
        public float ReviveHealthPercent { get; init; } = 0.1f;
        public float ReviveManaPercent { get; init; } = 0.1f;

        /// <summary>World fast-forward while the player lies dead (Engine.TimeScale).</summary>
        public float DeadTimeScale { get; init; } = 8f;

        /// <summary>Chance for a humanoid passer-by to burn the corpse; each NPC rolls ONCE per death.</summary>
        public float BurnChance { get; init; } = 0.15f;
        public float BurnRadius { get; init; } = 120f;
    }

    /// <summary>
    /// Post-defeat fate of the PLAYER: the body lies at the defeat spot for several game hours
    /// (seconds of real time — the world is fast-forwarded), then revives with a fraction of
    /// health; humanoid passers-by may burn the corpse, which is the final death (game over).
    /// Pure logic — the player node ticks it. The timer counts GAME minutes via the world clock,
    /// so any time acceleration is picked up automatically.
    /// </summary>
    public class PlayerLifecycle(PlayerLifecycleConfig config, IWorldClock clock, IRandomNumberGenerator rnd)
    {
        private readonly HashSet<string> _rolledPassersBy = [];
        private double _reviveAtGameMinutes;

        public PlayerLifeStage Stage { get; private set; } = PlayerLifeStage.Alive;
        public PlayerLifecycleConfig Config => config;

        public event Action? ReviveReady;
        public event Action? Burned;

        public void OnDefeated()
        {
            if (Stage != PlayerLifeStage.Alive) return;
            Stage = PlayerLifeStage.Defeated;
            _rolledPassersBy.Clear();
            _reviveAtGameMinutes = TotalGameMinutes() + (config.LieGameHours * 60f);
        }

        public void Tick()
        {
            if (Stage != PlayerLifeStage.Defeated) return;
            if (TotalGameMinutes() < _reviveAtGameMinutes) return;

            Stage = PlayerLifeStage.Alive;
            ReviveReady?.Invoke();
        }

        /// <summary>
        /// A passer-by reached the corpse: rolls the burn chance ONCE per NPC per death
        /// (a bystander standing next to the body must not re-roll every frame).
        /// True = the corpse burned; <see cref="Burned"/> has fired.
        /// </summary>
        public bool TryBurnRoll(string npcInstanceId)
        {
            if (Stage != PlayerLifeStage.Defeated) return false;
            if (!_rolledPassersBy.Add(npcInstanceId)) return false;
            if (rnd.RandFloat() > config.BurnChance) return false;

            Stage = PlayerLifeStage.FinalDead;
            Burned?.Invoke();
            return true;
        }

        private double TotalGameMinutes() => (clock.Day * 1440.0) + clock.MinuteOfDay;
    }
}
