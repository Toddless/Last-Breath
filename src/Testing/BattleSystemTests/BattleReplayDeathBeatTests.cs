namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Presentation;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The Godot-free half of the replay road (tracker #191): the order the timeline hands beats to
    /// the director, and the two rules of the death beat — the fall HOLDS the queue for the whole
    /// Dead clip (so the battle end, which waits behind the turn gate, cannot outrun it), and death
    /// cuts the corpse's remaining beats but never the fall itself.
    /// The actual clip playback and the arena's end gate live on Node types and are checked in Godot.
    /// </summary>
    [TestClass]
    public class BattleReplayDeathBeatTests
    {
        private const string DeadClip = "Dead";

        // ---------- the fall holds the queue ----------

        [TestMethod]
        public void Hold_WithADeadClip_LastsTheWholeClip()
        {
            var animations = Animations(hasDeadClip: true, seconds: 0.75f);

            Assert.AreEqual(0.75f, DeathBeat.HoldSeconds(animations.Object), 0.0001f);
        }

        [TestMethod]
        public void Hold_AsksForTheDeadClipByName()
        {
            var animations = Animations(hasDeadClip: true, seconds: 0.75f);

            DeathBeat.HoldSeconds(animations.Object);

            animations.Verify(a => a.GetClipSeconds(DeadClip), Times.Once);
        }

        [TestMethod]
        public void Hold_WithoutADeadClip_IsZero()
        {
            // Sprite-less fighters and un-authored art must not buy a phantom pause: the
            // missing-clip placeholder wait belongs to PlayAnimationAsync, not to the fall.
            var animations = Animations(hasDeadClip: false, seconds: 0.5f);

            Assert.AreEqual(0f, DeathBeat.HoldSeconds(animations.Object), 0.0001f);
        }

        [TestMethod]
        public void Hold_ForArtWithoutADeadClip_LastsTheTopple()
        {
            // Art drawn as three static facings has no Dead clip at all; the stand-in animator
            // answers for its own fall, so the queue still holds while the body topples. A zero here
            // would let the turn gate — and the end of the battle behind it — outrun the corpse.
            var animations = new Mock<IAnimationsComponent>();
            animations.Setup(a => a.HasClip(DeadClip)).Returns(TweenAnimationRules.Handles(DeadClip));
            animations.Setup(a => a.GetClipSeconds(DeadClip)).Returns(TweenAnimationRules.Seconds(DeadClip));

            Assert.AreEqual(TweenAnimationRules.DeathSeconds, DeathBeat.HoldSeconds(animations.Object), 0.0001f);
            Assert.IsTrue(DeathBeat.HoldSeconds(animations.Object) > 0f);
        }

        [TestMethod]
        public void Hold_NeverGoesNegative()
        {
            var animations = Animations(hasDeadClip: true, seconds: -2f);

            Assert.AreEqual(0f, DeathBeat.HoldSeconds(animations.Object), 0.0001f);
        }

        // ---------- death cuts the corpse's beats, not its own fall ----------

        [TestMethod]
        public void Fall_OfAnAlreadyFallenFighter_IsNotCut()
        {
            // The invariant of #191: EntityDiedEvent is the beat that SHOWS the death. If the
            // posthumous filter cut it, the queue would hold for an animation nobody started.
            var corpse = Fighter();

            Assert.IsFalse(DeathBeat.IsPosthumous(new EntityDiedEvent(corpse), HasFallen(corpse)));
        }

        [TestMethod]
        public void Beats_OfAFallenFighter_AreCut()
        {
            var corpse = Fighter();
            var standing = Fighter();
            var fallen = HasFallen(corpse);

            foreach (object beat in BeatsOf(corpse, standing))
                Assert.IsTrue(DeathBeat.IsPosthumous(beat, fallen), $"{beat.GetType().Name} of a corpse must be cut");
        }

        [TestMethod]
        public void Beats_OfAStandingFighter_AreKept()
        {
            var standing = Fighter();
            var other = Fighter();
            var nobodyFell = HasFallen();

            foreach (object beat in BeatsOf(standing, other))
                Assert.IsFalse(DeathBeat.IsPosthumous(beat, nobodyFell), $"{beat.GetType().Name} of a live fighter must play");
        }

        [TestMethod]
        public void Beats_OfAnUnknownKind_AreKept()
        {
            // Events without a handler are skipped silently by the director; the filter must not
            // start guessing about kinds it does not know.
            Assert.IsFalse(DeathBeat.IsPosthumous(new BattleQueueDefinedEvent([]), HasFallen(Fighter())));
        }

        // ---------- the order the director drains ----------

        [TestMethod]
        public void Timeline_RecordsTheKillingHitBeforeTheDeathItCaused()
        {
            var timeline = new BattleTimeline();
            var victim = Fighter();
            var bus = new CombatEventBus();
            timeline.Attach(bus);

            bus.Publish(new DamageTakenEvent(Mock.Of<IDamageContext>(), victim, Vitals(health: 0f)));
            bus.Publish(new EntityDiedEvent(victim));

            CollectionAssert.AreEqual(
                new[] { typeof(DamageTakenEvent), typeof(EntityDiedEvent) },
                timeline.Entries.Select(entry => entry.Event.GetType()).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1 }, timeline.Entries.Select(entry => entry.Sequence).ToArray());
        }

        [TestMethod]
        public void Timeline_KeepsCausalOrderAcrossFighters()
        {
            // Every fighter publishes on its own bus; the shared timeline is the single order the
            // director replays, so a reaction on another bus must land after what provoked it.
            var timeline = new BattleTimeline();
            var attacker = Fighter();
            var victim = Fighter();
            var attackerBus = new CombatEventBus();
            var victimBus = new CombatEventBus();
            timeline.Attach(attackerBus);
            timeline.Attach(victimBus);

            attackerBus.Publish(new TurnSkippedEvent(attacker, StatusEffects.Stun));
            victimBus.Publish(new DamageTakenEvent(Mock.Of<IDamageContext>(), victim, Vitals(health: 0f)));
            victimBus.Publish(new EntityDiedEvent(victim));

            CollectionAssert.AreEqual(
                new[] { typeof(TurnSkippedEvent), typeof(DamageTakenEvent), typeof(EntityDiedEvent) },
                timeline.Entries.Select(entry => entry.Event.GetType()).ToArray());
        }

        [TestMethod]
        public void Timeline_AttachedTwice_RecordsEachEventOnce()
        {
            var timeline = new BattleTimeline();
            var bus = new CombatEventBus();
            timeline.Attach(bus);
            timeline.Attach(bus); // a latecomer re-joining, a summon re-attached

            bus.Publish(new EntityDiedEvent(Fighter()));

            Assert.AreEqual(1, timeline.Entries.Count);
        }

        [TestMethod]
        public void Timeline_KeepsRecordingUntilItIsDetached()
        {
            // The outcome locks at resolve time, but the killing cast's trailing events still have to
            // reach the director: only DetachAll — after the arena's final gate — closes the stream.
            var timeline = new BattleTimeline();
            var bus = new CombatEventBus();
            timeline.Attach(bus);
            var corpse = Fighter();

            bus.Publish(new EntityDiedEvent(corpse));
            Assert.AreEqual(1, timeline.Entries.Count);

            timeline.DetachAll();
            bus.Publish(new EntityDiedEvent(corpse));

            Assert.AreEqual(1, timeline.Entries.Count, "a detached timeline must not grow");
        }

        // ---------- helpers ----------

        /// <summary>Every beat kind the posthumous filter knows, all pointing at <paramref name="subject"/>.</summary>
        private static List<object> BeatsOf(IFightable subject, IFightable other) =>
        [
            new BeforeAttackEvent(AttackBy(subject, other)),
            new DamageTakenEvent(Mock.Of<IDamageContext>(), subject, Vitals(health: 0f)),
            new EntityHealedEvent(subject, 10f, Vitals(health: 10f)),
            new AbilityActivatedEvent(Mock.Of<IAbility>(), subject, Vitals(health: 10f), "cast-1"),
            new TurnSkippedEvent(subject, StatusEffects.Stun),
            new AttackEvadedEvent(AttackBy(subject, other)),
            new AttackBlockedEvent(AttackBy(subject, other)),
            new EffectAppliedEvent(Mock.Of<IEffect>(), subject, other),
            new AbilityStageActivatedEvent(Mock.Of<IAbility>(), subject, 1),
            new ExhaustionChangedEvent(subject, 2, 0.5f),
        ];

        private static IAttackContext AttackBy(IFightable attacker, IFightable target) =>
            Mock.Of<IAttackContext>(context => context.Attacker == attacker && context.Target == target);

        private static Func<IFightable, bool> HasFallen(params IFightable[] fallen)
        {
            var ids = fallen.Select(fighter => fighter.InstanceId).ToHashSet();
            return fighter => ids.Contains(fighter.InstanceId);
        }

        private static Mock<IAnimationsComponent> Animations(bool hasDeadClip, float seconds)
        {
            var animations = new Mock<IAnimationsComponent>();
            animations.Setup(a => a.HasClip(DeadClip)).Returns(hasDeadClip);
            animations.Setup(a => a.GetClipSeconds(DeadClip)).Returns(seconds);
            return animations;
        }

        private static VitalsSnapshot Vitals(float health) => new(health, 100f, 0f, 0f, 0f, 0f);

        private static IFightable Fighter()
        {
            var fighter = new Mock<IFightable>();
            fighter.Setup(f => f.InstanceId).Returns(Guid.NewGuid().ToString());
            return fighter.Object;
        }
    }
}
