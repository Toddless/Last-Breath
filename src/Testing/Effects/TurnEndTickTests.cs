namespace LastBreathTest.Effects
{
    using System;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>
    /// The bearer's end of turn, and the two things about it that used to be nobody's to rely on.
    ///
    /// <para><b>It can be waited for.</b> The tick was an <c>async void</c>: it handed control back at
    /// its first await, so everything the fighter does to close his turn — the recovery, the three
    /// announcements — ran BEFORE the damage-over-time damage landed, and no caller up to the turn loop
    /// could wait for it even in principle. A tick that kills would then kill after the turn had been
    /// declared over, past the gate the presentation waits on.</para>
    ///
    /// <para><b>It happens in one order.</b> Every effect counts itself down and banks what it means to
    /// tick; only then is the whole tick dealt, in one blow per status per caster. A poison on its last
    /// turn still poisons, because the tick is booked before the turn is spent.</para>
    ///
    /// <para><b>It costs a turn, and only one.</b> A duration of N buys N ends of the BEARER's turn and
    /// the effect is off him after the Nth — what the description promises and what the fight plays are
    /// the same number. The effect used to be checked for expiry BEFORE it was counted down, so it sat
    /// on the bearer through an (N+1)-th turn: a free turn of every buff, every seal and every skipped
    /// turn of control in the game.</para>
    /// </summary>
    [TestClass]
    public class TurnEndTickTests
    {
        private const float Blow = 100f;
        private const float Potency = 0.5f;

        /// <summary>The duration the contract is walked on: long enough to tell "one turn short" from
        /// "one turn long", short enough to spell every turn of it out.</summary>
        private const int PinnedTurns = 3;

        [TestMethod]
        public async Task TheTickIsDealtBeforeTheEndOfTurnIsOver()
        {
            // What "awaitable" is worth. The caller holds the task; when it comes back the damage has
            // landed. Under async void the assertion below read the health as it was before the tick.
            var bearer = new Fighter();
            await Poison(bearer, turns: 3);
            float before = bearer.Damage;

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(0f, before, "the poison ticked before the turn ended at all");
            Assert.AreEqual(Blow * Potency, bearer.Damage, 0.001f,
                "the end of turn came back with its damage still on the way");
        }

        [TestMethod]
        public async Task TheLastTurnOfAPoisonStillPoisons()
        {
            // The order, where it shows. The stack has one turn left: it books its tick and then spends
            // the turn. Counting down first would leave it expired with nothing banked, and the player
            // would be paying for a turn of poison he never gets.
            var bearer = new Fighter();
            IEffect stack = await Poison(bearer, turns: 1);

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency, bearer.Damage, 0.001f, "the poison spent its last turn without ticking on it");
            Assert.AreEqual(0, stack.Duration, "the stack did not spend the turn it ticked on");
        }

        [TestMethod]
        public async Task EveryTurnDealsExactlyOneTickAndNoMore()
        {
            // The tally is emptied by the tick that paid it out. A banked tick surviving its own turn
            // would compound: every turn would deal that turn's poison and every turn before it.
            var bearer = new Fighter();
            await Poison(bearer, turns: 3);

            await bearer.Object.Effects.TriggerTurnEnd();
            await bearer.Object.Effects.TriggerTurnEnd();
            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency * 3, bearer.Damage, 0.001f,
                "three turns of one stack came to something other than three ticks");
        }

        [TestMethod]
        public async Task AnEffectOfThreeTurnsStandsForThreeEndsOfTurnAndNoMore()
        {
            // The contract itself, on an effect that does nothing but last: laid by the bearer on
            // himself, so the nearest end of turn is the one closing the turn it was laid in.
            var bearer = new Fighter();
            IEffect marked = await Lay(new Marker(PinnedTurns), bearer, bearer);

            for (int turn = 1; turn <= PinnedTurns; turn++)
            {
                Assert.IsTrue(bearer.Object.Effects.Effects.Contains(marked), $"the effect was gone before its turn {turn}");
                await bearer.Object.Effects.TriggerTurnEnd();
            }

            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(marked), "three turns of effect outlived three ends of turn");
        }

        [TestMethod]
        public async Task AnEffectOfOneTurnIsSpentByTheFirstEndOfTurn()
        {
            // The shortest duration there is, and the one the off-by-one doubled: a one-turn stun used
            // to skip two turns.
            var bearer = new Fighter();
            IEffect marked = await Lay(new Marker(1), bearer, bearer);

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(marked), "one turn of effect outlived one end of turn");
        }

        [TestMethod]
        public async Task AnEffectLaidWithNoTurnsAtAllIsOffTheBearerAtTheFirstEndOfTurn()
        {
            // The clamp on the countdown. A duration mutator can scale a one-turn effect down to
            // nothing, and an unclamped countdown takes it to −1, where the expiry test never fires
            // again: a buff the fight has no way left to take off.
            var bearer = new Fighter();
            IEffect marked = await Lay(new Marker(0), bearer, bearer);
            Assert.IsTrue(bearer.Object.Effects.Effects.Contains(marked), "the effect never landed, so its leaving proves nothing");

            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(marked), "an effect with no turns on it outlived an end of turn");
        }

        [TestMethod]
        public async Task AnEffectOnTheEnemyIsSpentByHisTurnsAndNotTheCastersOwn()
        {
            // The other side of the same contract. Only the bearer's own ends of turn count, so a
            // debuff laid on an enemy starts spending itself when HIS turn closes, not when the
            // caster's does — and the caster can close as many turns as he likes without touching it.
            var caster = new Fighter();
            var bearer = new Fighter();
            IEffect marked = await Lay(new Marker(PinnedTurns), caster, bearer);

            for (int turn = 0; turn <= PinnedTurns; turn++) await caster.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(PinnedTurns, marked.Duration, "the caster's own turns spent a debuff standing on somebody else");

            for (int turn = 0; turn < PinnedTurns; turn++) await bearer.Object.Effects.TriggerTurnEnd();

            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(marked), "three turns of debuff outlived three of the bearer's turns");
        }

        [TestMethod]
        public async Task AThreeTurnPoisonTicksThreeTimesAndNothingOnAFourthTurn()
        {
            // The damaging half of the contract, and the trap in it: the stack leaves on the turn it
            // ticks for the last time, and the removal cancels pending ticks. Taking the tally away
            // with the stack would make every damaging effect a turn's worth of damage weaker.
            // The two halves are one change: reverting the countdown alone reddens THIS walk at four
            // ticks, and only reverting both puts the whole contract back where it was.
            var bearer = new Fighter();
            await Poison(bearer, turns: PinnedTurns);

            for (int turn = 0; turn <= PinnedTurns; turn++) await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency * PinnedTurns, bearer.Damage, 0.001f,
                "a three-turn poison came to something other than three ticks");
        }

        [TestMethod]
        public async Task AnExtensionOnTheLastTurnBuysAnotherTick()
        {
            // The edge the boundary moved onto: an effect down to its final turn is still standing and
            // still extendable, and the turn it is given is a turn it actually ticks on.
            var bearer = new Fighter();
            IEffect poison = await Poison(bearer, turns: 2);
            await bearer.Object.Effects.TriggerTurnEnd();
            Assert.AreEqual(1, poison.Duration, "the poison did not spend its first turn, so the extension below proves nothing");

            Assert.AreEqual(1, poison.Extend(1), "an effect on its last turn refused an extension");

            await bearer.Object.Effects.TriggerTurnEnd();
            await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency * 3, bearer.Damage, 0.001f, "two turns of poison and the turn bought for it came to something else");
            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(poison), "the extended poison outlived the turn it was extended to");
        }

        [TestMethod]
        public async Task ARepeatedApplicationAtTheCeilingBuysTheFullDurationAgain()
        {
            // A re-application that lays no stack refreshes the standing one, and what it refreshes to
            // is a full duration counted from there: the stack goes on ticking for as many turns as a
            // freshly laid one would.
            var bearer = new Fighter();
            IEffect poison = await Poison(bearer, turns: PinnedTurns, maxStacks: 1);
            await bearer.Object.Effects.TriggerTurnEnd();

            await Poison(bearer, turns: PinnedTurns, maxStacks: 1);

            Assert.AreEqual(PinnedTurns, poison.Duration, "the standing stack was not refreshed to a full duration");
            Assert.AreEqual(1, bearer.Object.Effects.Effects.Count, "the re-application laid a second stack past the ceiling");

            for (int turn = 0; turn < PinnedTurns; turn++) await bearer.Object.Effects.TriggerTurnEnd();

            Assert.AreEqual(Blow * Potency * (PinnedTurns + 1), bearer.Damage, 0.001f,
                "the refreshed stack did not tick a full duration on top of the turn it had already spent");
            Assert.IsFalse(bearer.Object.Effects.Effects.Contains(poison), "the refreshed stack outlived the duration it was refreshed to");
        }

        /// <summary>One poison stack on the bearer, ticking a share of a blow.</summary>
        private static async Task<IEffect> Poison(Fighter bearer, int turns, int maxStacks = DamageOverTurnEffect.NoCeilingOfItsOwn) =>
            await Lay(new DamageOverTurnEffect(turns, StatusEffects.Poison, maxStacks, Potency), bearer, bearer);

        /// <summary>The one road every walk here lays an effect by: a cast of the caster's, landing on
        /// the bearer, carrying a blow for whatever feeds on one.</summary>
        private static async Task<IEffect> Lay(IEffect effect, Fighter caster, Fighter bearer)
        {
            await effect.Apply(new EffectApplyingContext
            {
                Caster = caster.Object,
                Target = bearer.Object,
                Source = $"Test_{Guid.NewGuid()}",
                Damage = DamageSnapshot.Of(DamageType.Physical, Blow)
            });

            return effect;
        }

        /// <summary>An effect with nothing to it but a duration: what the contract is measured on, where
        /// a tick or a heal would be measuring something else at the same time.</summary>
        private sealed class Marker(int duration) : Effect(id: "Effect_Test_Marker", duration, maxStacks: 1)
        {
            public override IEffect Copy() => new Marker(Duration);
        }

        /// <summary>A fightable with real effects that records what it was dealt: the tick's damage is
        /// the whole of what these walks measure, and mitigation is somebody else's subject.</summary>
        private sealed class Fighter
        {
            private readonly Mock<IFightable> _mock = new();

            public Fighter()
            {
                var effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
                _mock.Setup(fighter => fighter.TakeDamage(It.IsAny<IDamageContext>()))
                    .Returns<IDamageContext>(Record);
            }

            public IFightable Object => _mock.Object;

            /// <summary>Everything the bearer has been dealt since it was made.</summary>
            public float Damage { get; private set; }

            private Task Record(IDamageContext context)
            {
                Damage += context.TotalDamage;
                return Task.CompletedTask;
            }
        }
    }
}
