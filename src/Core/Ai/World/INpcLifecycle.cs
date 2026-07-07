namespace Core.Ai.World
{
    using System;

    public interface INpcLifecycle
    {
        NpcLifeStage Stage { get; }

        /// <summary>The rolled rise delay of the current Defeated stage (save system reads it).</summary>
        float ResurrectDelay { get; }

        /// <summary>Seconds already lain of the current Defeated stage (save system reads it).</summary>
        float Elapsed { get; }

        /// <summary>True while the body lies and can be burned (both fresh corpses and dormant undead).</summary>
        bool CanBeBurned { get; }

        /// <summary>
        /// Fired once when the timer completes; the argument is the parameter bonus of the rising
        /// (StrengthFraction × MaxStrengthBonus, e.g. 0.4 = +40% to the boosted parameters).
        /// </summary>
        event Action<float>? ResurrectionReady;

        /// <summary>The first health zero-out. Undead go dormant, everyone else starts the rise timer.</summary>
        void OnDefeated(bool isUndead);

        /// <summary>Save-load path: puts a freshly built body straight into a lying stage with its timer.</summary>
        void RestoreState(NpcLifeStage stage, float resurrectDelay, float elapsed);

        void Tick(float delta);
        bool TryBurn();

        /// <summary>How strong the rising is: 0 at the minimum possible delay, 1 at the maximum.</summary>
        float StrengthFraction();
    }
}
