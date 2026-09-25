namespace Core.Ai.World
{
    using System;

    /// <summary>
    /// What happens to an NPC body after it is put down: it lies in the world for a rolled delay
    /// and then gets back up. Everything the cycles share lives here — the lying stage, its timer
    /// and burning; the ENDING is not part of this contract, because the two endings are not
    /// interchangeable. A cycle declares its ending by implementing <see cref="IUndeadRiseLifecycle"/>
    /// or <see cref="IAliveRiseLifecycle"/>, so a consumer can only subscribe to an outcome the
    /// cycle in hand actually has.
    /// </summary>
    public interface INpcLifecycle
    {
        NpcLifeStage Stage { get; }

        /// <summary>The rolled rise delay of the current lying stage (save system reads it).</summary>
        float ResurrectDelay { get; }

        /// <summary>Seconds already lain of the current lying stage (save system reads it).</summary>
        float Elapsed { get; }

        /// <summary>True while the body lies and this cycle allows burning it at all.</summary>
        bool CanBeBurned { get; }

        /// <summary>
        /// The first health zero-out. <paramref name="isUndead"/> states whether the body is undead
        /// at that moment; a cycle whose ending does not depend on that fact behaves the same for
        /// both values. Calling it again while the body already lies changes nothing.
        /// </summary>
        void OnDefeated(bool isUndead);

        /// <summary>
        /// Save-load path: puts a freshly built body into a lying stage with its timer. A stage this
        /// cycle does not count down is refused and the NPC stays exactly as it was built.
        /// </summary>
        void RestoreState(NpcLifeStage stage, float resurrectDelay, float elapsed);

        /// <summary>Advances the countdown by the elapsed seconds; does nothing while the body is not counting down.</summary>
        void Tick(float delta);

        /// <summary>
        /// Burns the lying body, which is the final death. False when the body is not lying
        /// or when this cycle has no final death at all.
        /// </summary>
        bool TryBurn();
    }

    /// <summary>
    /// A cycle whose body comes back AS UNDEAD: it changes faction and returns stronger the longer
    /// it lay. Only such a cycle can report a rising, so only through this interface can one be awaited.
    /// </summary>
    public interface IUndeadRiseLifecycle : INpcLifecycle
    {
        /// <summary>
        /// Fired once when the timer completes and the body rises as undead; the argument is the
        /// parameter bonus of the rising (e.g. 0.4 = +40% to the boosted parameters).
        /// </summary>
        event Action<float>? ResurrectionReady;
    }

    /// <summary>
    /// A cycle whose body gets up ALIVE — the same creature of the same faction, which is why the
    /// world keeps its people however often they are knocked down.
    /// </summary>
    public interface IAliveRiseLifecycle : INpcLifecycle
    {
        /// <summary>
        /// Fired once when the timer completes and the body gets up alive. Carries no strength:
        /// only a rising undead is reforged by the delay it lay.
        /// </summary>
        event Action? ReviveReady;
    }
}
