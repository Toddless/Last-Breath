namespace Battle.Source.Presentation
{
    using System;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Events;

    /// <summary>
    /// The Godot-free half of the death beat. <see cref="BattleDirector"/> owns "how the fall looks";
    /// the two rules that make the fall a BEAT instead of a side effect live here, where a test can
    /// reach them without a scene tree:
    /// <list type="bullet">
    /// <item>the fall HOLDS the playback queue for the length of the Dead clip, so the turn gate —
    /// and the end of the battle behind it — never outruns the corpse (tracker #191);</item>
    /// <item>death cuts the corpse's remaining beats, but never the fall itself.</item>
    /// </list>
    /// </summary>
    public static class DeathBeat
    {
        /// <summary>The Dead clip by the animation naming convention (see AnimationsComponent).</summary>
        public const string Animation = "Dead";

        /// <summary>
        /// How long the queue waits while the corpse falls, in UNSCALED seconds — the caller divides
        /// by the current playback speed, so an abort fast-forward shortens the fall instead of
        /// holding the quit for it. No clip (sprite-less fighters, art not authored yet) means there
        /// is nothing to wait for: zero, never the missing-clip placeholder pause.
        /// </summary>
        public static float HoldSeconds(IAnimationsComponent animations) =>
            animations.HasClip(Animation) ? Math.Max(animations.GetClipSeconds(Animation), 0f) : 0f;

        /// <summary>
        /// Death cuts the corpse's remaining beats: everything recorded after the killing hit that
        /// involves an already-fallen entity is dropped. Logic already prevents posthumous actions,
        /// so this is a presentation-side guarantee, not a rules check.
        /// <see cref="EntityDiedEvent"/> is deliberately absent from the map: the fall IS the beat
        /// that shows the death, and cutting it would delete the very animation the queue is held for.
        /// </summary>
        public static bool IsPosthumous(object evnt, Func<IFightable, bool> hasFallen) => evnt switch
        {
            BeforeAttackEvent attack => hasFallen(attack.Context.Attacker),
            DamageTakenEvent damage => hasFallen(damage.Target),
            EntityHealedEvent healed => hasFallen(healed.Healed),
            AbilityActivatedEvent ability => hasFallen(ability.Caster),
            TurnSkippedEvent skipped => hasFallen(skipped.Fighter),
            AttackEvadedEvent evaded => hasFallen(evaded.Context.Attacker),
            AttackBlockedEvent blocked => hasFallen(blocked.Context.Attacker),
            EffectAppliedEvent applied => hasFallen(applied.Target),
            AbilityStageActivatedEvent stage => hasFallen(stage.Caster),
            ExhaustionChangedEvent exhaustion => hasFallen(exhaustion.Fighter),
            _ => false,
        };
    }
}
