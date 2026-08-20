namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Riders;
    using Battle.Source.Effects;
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
    /// What an effect remembers about where it came from.
    ///
    /// <para><b>Nothing in the book reads it yet, and that is the decision.</b> Both records that looked
    /// like customers — prolonging poison with a hit, and the deep freeze holding the target's effects up
    /// a turn — are DOMAIN by the owner's word (2026-08-19): they read what is on the target, whoever put
    /// it there, and their cards say so. The trace is the foundation the day a card says "your poison",
    /// the same way dispel strength was in place before anything was written to need it. So these walks
    /// are about the MECHANISM and lean on no record: everything below is laid by hand, and a balance
    /// pass that rewrites every augment in the game cannot make one of them fail.</para>
    ///
    /// <para><b>Whose, not merely which.</b> The ability id alone would make two fighters carrying one
    /// ability into one poisoner. The instance behind it belongs to a single fighter, so it is what the
    /// question is actually answered on.</para>
    /// </summary>
    [TestClass]
    public class AbilityTraceTests
    {
        /// <summary>An attacking ability that names poison nowhere: everything about the poison below is
        /// laid by hand, so nothing but the trace can be doing the sorting.</summary>
        private const string Attacker = "Ability_Series_Of_Attacks";

        private const int PoisonTurns = 3;
        private const float Blow = 100f;

        /// <summary>The share of the blow the stacks below tick with. Nothing here measures a tick, but a
        /// stack that ticks for nothing is never laid at all, so the walks need SOME share named.</summary>
        private const float TickShare = 0.5f;

        [TestMethod]
        public async Task WhatNoAbilityLaidBelongsToNoAbility()
        {
            // A passive, an item grant and a boss stage all lay effects with no cast behind them. The
            // honest answer is nothing at all, and nothing at all is nobody's "mine" — otherwise the
            // first record to ask "is this mine?" would be handed everything unclaimed on the field.
            var victim = new Fighter();
            IEffect stray = await Poison(new Fighter(), victim, AbilityTrace.None);

            Assert.IsFalse(stray.Trace.IsFromAbility, "a stack nobody cast claims to have come out of a cast");
            Assert.IsFalse(stray.Trace.IsFrom(Attacking()), "an untraced stack answered to an ability that never laid it");
        }

        [TestMethod]
        public async Task TheSameAbilityInTwoFightersIsTwoOwners()
        {
            // The case the record id alone cannot answer. Two fighters carry the same ability; one of
            // them poisons the target. The other one's copy of that ability is not the poisoner, and a
            // record on it may not hold that poison up.
            Ability mine = Attacking();
            Ability his = Attacking();
            var victim = new Fighter();

            IEffect stack = await Poison(new Fighter(), victim, mine.Trace);

            Assert.AreEqual(mine.Id, his.Id, "the two stand-ins are not even the same ability, so the walk proves nothing");
            Assert.IsTrue(stack.Trace.IsFrom(mine), "the poisoner did not recognise his own stack");
            Assert.IsFalse(stack.Trace.IsFrom(his),
                "the other fighter's copy of the same ability claimed a stack it never laid");
        }

        [TestMethod]
        public async Task MineIsPickedOutOfAPileThatIsMostlyNotMine()
        {
            // What the mechanism is FOR, walked without a record: three stacks on one victim — this
            // ability's, another fighter's out of the same ability, and one nobody cast — and the reader
            // that wants only its own gets exactly one of them. This is the whole of what a card saying
            // "your poison" would need on the day one says it.
            Ability mine = Attacking();
            var caster = new Fighter();
            var victim = new Fighter();

            IEffect ours = await Poison(caster, victim, mine.Trace);
            await Poison(caster, victim, Attacking().Trace);
            await Poison(caster, victim, AbilityTrace.None);

            var claimed = victim.Object.Effects.GetBy(effect => effect.Trace.IsFrom(mine)).ToList();

            Assert.AreEqual(3, victim.Object.Effects.GetBy(effect => effect.Status == StatusEffects.Poison).Count(),
                "the three stacks did not all land, so the pick below proves nothing");
            Assert.AreEqual(1, claimed.Count, "the ability claimed a number of stacks other than the one it laid");
            Assert.AreSame(ours, claimed[0], "the ability claimed a stack that was not the one it laid");
        }

        [TestMethod]
        public async Task ADomainReadingIsUntouchedByTheTrace()
        {
            // The state the book is actually shipped in. Every reader in the game today asks what is ON
            // the target and never whose it is, and the trace riding along must not change one of those
            // answers — a stack is a stack whoever cast it.
            var caster = new Fighter();
            var victim = new Fighter();

            await Poison(caster, victim, Attacking().Trace);
            await Poison(caster, victim, Attacking().Trace);
            await Poison(caster, victim, AbilityTrace.None);

            var onTarget = victim.Object.Effects
                .GetBy(effect => effect.Status == StatusEffects.Poison)
                .OfType<DamageOverTurnEffect>()
                .ToList();

            Assert.AreEqual(3, onTarget.Count, "a domain reading began sorting the payload by who laid it");
            Assert.AreEqual(3, onTarget.Count(stack => stack.Duration == PoisonTurns), "the stacks did not land alike");
        }

        [TestMethod]
        public async Task TheTraceSurvivesACopyAndABonusStack()
        {
            // A copy carries no trace of its own — it takes the one it is applied WITH, which is how
            // spreads, transfers and the extra stacks a mutator grants stay claimable by the cast behind
            // them. A bonus stack is the sharpest case: nothing outside the effect lays it.
            Ability mine = Attacking();
            var caster = new Fighter();
            var victim = new Fighter();

            var original = new DamageOverTurnEffect(PoisonTurns, StatusEffects.Poison, DamageOverTurnEffect.NoCeilingOfItsOwn, TickShare);
            EffectApplyingContext laying = Laying(caster, victim, mine.Trace);
            await original.Apply(laying);
            await original.Copy().Apply(laying);
            await original.Copy().Apply(laying with { IsBonusStack = true });

            var stacks = victim.Object.Effects.GetBy(effect => effect.Status == StatusEffects.Poison).ToList();

            Assert.AreEqual(3, stacks.Count, "the copies never landed, so their traces prove nothing");
            foreach (IEffect stack in stacks)
                Assert.IsTrue(stack.Trace.IsFrom(mine), "a copy landed without the cast that made it");
        }

        [TestMethod]
        public async Task ACastOfManyBlowsTransfersTheCorpsesPoisonOnce()
        {
            // The transfer rider waits on a touched target's death, and a cast of many blows touches its
            // target many times. It used to subscribe on EVERY blow — its own doc said "once per target"
            // and the code did nothing of the kind — so a five-hit series left five closures waiting, and
            // the corpse's poison jumped five times over: five stacks out of one, on one victim.
            var owner = new Fighter();
            var victim = new Fighter();
            var heir = new Fighter();
            var rider = new TransferPoisonOnDeathRider();
            AbilityImpact blow = Swing(Attacking(), owner, victim, FieldWith(owner, heir));

            await rider.Apply(blow);
            await rider.Apply(blow);
            await rider.Apply(blow);
            await Poison(owner, victim, AbilityTrace.None);

            victim.Object.CombatEvents.Publish(new EntityDiedEvent(victim.Object, owner.Object));

            Assert.AreEqual(1, heir.Object.Effects.GetBy(effect => effect.Status == StatusEffects.Poison).Count(),
                "the blows of one cast each moved the corpse's poison, so one stack became several");
        }

        [TestMethod]
        public void TheTraceCarriesWhichWhoseAndWhen()
        {
            // The third name. The ability instance says whose, the record id says which, and the cast id
            // says WHEN — the vocabulary the replay already groups a cast's events by, kept here so that
            // anything wanting one firing's payload has it without a fourth id being invented.
            Ability ability = Attacking();

            Assert.AreEqual(Attacker, ability.Trace.AbilityId, "the trace does not carry the record the design talks in");
            Assert.AreEqual(ability.InstanceId, ability.Trace.AbilityInstanceId, "the trace does not say whose ability it was");

            var firstFiring = new AbilityTrace(Attacker, "cast-one", ability.InstanceId);
            var secondFiring = new AbilityTrace(Attacker, "cast-two", ability.InstanceId);

            Assert.IsTrue(firstFiring.IsFrom(ability), "a firing of the ability did not answer to the ability");
            Assert.IsTrue(secondFiring.IsFrom(ability), "the second firing stopped being the same ability's");
            Assert.IsFalse(firstFiring.IsFromSameCastAs(secondFiring), "two firings of one ability read as one");
            Assert.IsFalse(AbilityTrace.None.IsFromSameCastAs(AbilityTrace.None),
                "two payloads nobody cast were read as coming out of one firing");
        }

        /// <summary>A stand-in for the ability under test: the shipped record, built by the registry the
        /// game builds with, so its id and instance are the real thing.</summary>
        private static Ability Attacking()
        {
            (AbilityProvider book, _) = ShippedAbilityData.Load();
            return (Ability)book.CreateAbility(Attacker);
        }

        /// <summary>One poison stack laid under the trace named — the ability's own, a stranger's, or
        /// none at all for what a passive would leave behind.</summary>
        private static async Task<IEffect> Poison(Fighter caster, Fighter victim, AbilityTrace trace)
        {
            var stack = new DamageOverTurnEffect(PoisonTurns, StatusEffects.Poison, maxStacks: 99, percentFromDamage: TickShare);
            await stack.Apply(Laying(caster, victim, trace));
            return stack;
        }

        /// <summary>One landing blow of a cast, as a delivery hands one to the riders.</summary>
        private static AbilityImpact Swing(IAbility source, Fighter caster, Fighter victim, IBattleField field) =>
            new(caster.Object, victim.Object, field, Succeeded: true)
            {
                Source = source,
                Kind = ImpactKind.Attack
            };

        /// <summary>A field whose enemies of the owner are exactly the fighters named.</summary>
        private static IBattleField FieldWith(Fighter owner, params Fighter[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(arena => arena.GetEnemies(owner.Object))
                .Returns([.. enemies.Select(enemy => enemy.Object)]);

            return field.Object;
        }

        private static EffectApplyingContext Laying(Fighter caster, Fighter victim, AbilityTrace trace) =>
            new()
            {
                Caster = caster.Object,
                Target = victim.Object,
                Source = $"Test_{Guid.NewGuid()}",
                Damage = DamageSnapshot.Of(DamageType.Physical, Blow),
                Trace = trace
            };

        /// <summary>A fightable an effect can actually land on: real effects and modifier pipelines,
        /// only the entity itself is a mock.</summary>
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
            }

            public IFightable Object => _mock.Object;
        }
    }
}
