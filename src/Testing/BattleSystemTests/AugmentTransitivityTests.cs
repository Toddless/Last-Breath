namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.SeriesOfAttacks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;
    using SeriesOfAttacksCast = Battle.Source.Abilities.SeriesOfAttacks.SeriesOfAttacks;

    /// <summary>
    /// Two augments bought separately have to add up on their own. One of them says a landing impact
    /// poisons what it touched, the other says poison lasts a turn longer, and neither was written with
    /// the other in mind — the composition has to fall out of ARITHMETIC, because the alternative is a
    /// line of code for every pair a player can put in two sockets.
    ///
    /// What makes it arithmetic is two things meeting. The applier lays its numbers on the ability as
    /// parameters of its own instead of keeping them, so there is something for an amplifier to
    /// decorate; and the rider reads those parameters at the moment it lays a stack rather than at the
    /// moment it was built, so what the amplifier did is felt. Take either half away and the pair goes
    /// silent: before this step the applier's turns were nailed into its constructor and the amplifier's
    /// decorator landed on a key nothing had registered.
    ///
    /// The seating below is done by hand rather than through <see cref="AugmentFit"/>: the amplifier's
    /// record carries the tag "poison" and a series of attacks carries none, so the pair does not meet
    /// on the board yet. Which tags an ability effectively wears once an applier is seated on it is the
    /// granted-tags step of wave C in <c>Docs/PLAN-Augments.md</c>; what is asked here is the other
    /// half — that the numbers compose once they are on one ability.
    /// </summary>
    [TestClass]
    public class AugmentTransitivityTests
    {
        /// <summary>An ability that attacks and names poison nowhere — the applier brings all of it.</summary>
        private const string Attacker = "Ability_Series_Of_Attacks";

        /// <summary>The applier: every successful impact puts a stack on its target.</summary>
        private const string Applier = "Augment_Poison_Attack_Series";

        /// <summary>The amplifier: poison lasts longer. Written for no applier in particular.</summary>
        private const string Amplifier = "Augment_Poison_Duration";

        /// <summary>A third augment on the same ability, buying impacts rather than poison — the
        /// multiplier of the pair above.</summary>
        private const string MoreAttacks = "Augment_Additional_Attacks";

        private const string TurnsProperty = "poisonDuration";

        /// <summary>The parameter the applier lends the ability — the one an amplifier of poison
        /// duration stands on, and the one that must outlive any single copy of the applier.</summary>
        private const string TurnsKey = AbilityParameter.PoisonDuration;
        private const string AttacksProperty = "amountAttacks";

        private const float Blow = 100f;

        /// <summary>Share of the blow one tick of a stack carries. The applier's record does not name it,
        /// so the figure lives as the registry's fallback — and it is pinned here because a potency that
        /// silently became zero would leave every walk above green over stacks that tick for nothing.</summary>
        private const float Potency = 0.7f;

        [TestMethod]
        public async Task TheAmplifierLengthensTheStacksTheApplierLays()
        {
            // (a) of the wave's DoD. The first swing pins what the applier is worth alone, so the second
            // measures the amplifier and not the applier. Before the lazy form this case failed on the
            // second assert: the stack came out at the applier's own turns, whatever else was seated.
            var bench = new Bench();
            var caster = new Brawler();
            bench.Seat(Applier);

            IEffect bare = await bench.PoisonSwing(caster, new Brawler());
            Assert.AreEqual(bench.Turns, bare.Duration, "the applier did not even lay the stack its own record declares");
            var ticking = bare as IDamageOverTurnEffect;
            Assert.IsNotNull(ticking, "the applier laid something that does not tick");
            Assert.AreEqual(Blow * Potency, ticking.DamagePerTick,
                "the stack ticks for something other than its share of the blow — the potency moved onto the ability and lost its value on the way");

            bench.Seat(Amplifier);
            IEffect amplified = await bench.PoisonSwing(caster, new Brawler());

            Assert.AreEqual(bench.Turns + bench.Extension, amplified.Duration,
                "the amplifier is seated on the same ability and the stack lasts exactly as long as it did without it");
        }

        [TestMethod]
        public async Task EveryAttackTheSeriesGainsIsOneMoreLengthenedStack()
        {
            // (b). The pair above, multiplied by a third augment that knows nothing of poison: it buys
            // attacks, and every attack is an impact, and every impact is a stack — so a series one
            // attack longer poisons its target one more time, at the amplified length. Three augments,
            // and no code aware of any pair among them.
            using var rolls = new CombatRandomScope(new ShortestSeries());
            var bench = new Bench();
            bench.Seat(Applier);
            bench.Seat(Amplifier);

            var target = new Brawler();
            int attacks = bench.Ability.MinAttacks;
            await bench.Series(target);
            List<IEffect> before = target.Poison;
            Assert.AreEqual(attacks, before.Count, "the series laid a number of stacks other than one per attack");

            bench.Seat(MoreAttacks);
            var second = new Brawler();
            await bench.Series(second);
            List<IEffect> after = second.Poison;

            Assert.AreEqual(attacks + (int)bench.AddedAttacks, bench.Ability.MinAttacks, "the third augment never reached the attack count");
            Assert.AreEqual(before.Count + (int)bench.AddedAttacks, after.Count,
                "the added attacks landed without laying stacks of their own");
            foreach (IEffect stack in after)
                Assert.AreEqual(bench.Turns + bench.Extension, stack.Duration,
                    "a stack of the longer series came out at the applier's own turns — the amplifier reached only some of them");
        }

        [TestMethod]
        public async Task TakingTheAmplifierOutLeavesTheApplierWorkingAtItsOwnTurns()
        {
            // (c). The mirror, and what tells a composition from an ability that quietly kept the sum:
            // the amplifier leaves and the very next stack is the applier's own length again, while the
            // applier itself goes on laying stacks. An extraction that took the poison with it would be
            // the two having been welded together after all.
            var bench = new Bench();
            var caster = new Brawler();
            bench.Seat(Applier);
            bench.Seat(Amplifier);
            IEffect amplified = await bench.PoisonSwing(caster, new Brawler());
            Assert.AreEqual(bench.Turns + bench.Extension, amplified.Duration, "the pair never composed, so the removal below proves nothing");

            bench.Unseat(Amplifier);
            IEffect bare = await bench.PoisonSwing(caster, new Brawler());

            Assert.AreEqual(bench.Turns, bare.Duration, "the amplifier is back in the bag and its turns are still being paid out");
        }

        [TestMethod]
        public async Task TheLentTurnsStayWhileEitherOfTwoAppliersIsStillSeated()
        {
            // A number an upgrade lends the ability belongs to every upgrade standing on it, so it goes
            // back only with the LAST of them. Two copies of one augment on one ability is an ordinary
            // board — the shipped tree already carries two socket nodes of one tier on Ares Blessing, and
            // the ornaments of wave E hand out pairs on purpose — and on the first bookkeeping the second
            // copy registered nothing, the first one to leave took the key away, and the applier left
            // behind went on riding every impact reading a parameter nobody had registered: two
            // not-found reports per hit and a stack of zero turns ticking for zero damage.
            var bench = new Bench();
            IAbilityAugment first = bench.Seat(Applier);
            IAbilityAugment second = bench.Seat(Applier);
            Assert.AreEqual(bench.Turns, bench.Ability[TurnsKey], "two copies of the applier left the ability with turns neither of them declares");
            Assert.AreEqual(bench.Turns, (await bench.PoisonSwing(new Brawler(), new Brawler(), expected: 2)).Duration,
                "the pair of copies poisons nothing at all");

            bench.Unseat(first);

            Assert.AreEqual(bench.Turns, bench.Ability[TurnsKey],
                "one copy is still seated and the turns it stands on are already gone");

            bench.Unseat(second);

            Assert.AreEqual(0f, bench.Ability[TurnsKey],
                "the last copy left and the parameter it lent stayed on the ability");
        }

        [TestMethod]
        public async Task EveryCopyOfAnApplierRidesForItself()
        {
            // The other half of the pair above. A copy is a purchase: two of them ride twice on one
            // impact and lay two stacks, and pulling one takes away ITS rider and leaves the other's
            // where it is. Riders used to be held by the record's name, so the second copy installed
            // nothing at all and the first to leave took the only one with it — the player paid twice
            // for one rider and lost it by unseating either half.
            var bench = new Bench();
            IAbilityAugment first = bench.Seat(Applier);
            bench.Seat(Applier);

            Assert.AreEqual(2, bench.Ability.ImpactRiders.Count, "the second copy rode along on the first one's rider");

            bench.Unseat(first);

            Assert.AreEqual(1, bench.Ability.ImpactRiders.Count,
                "one copy left and took the other copy's rider with it");

            // And the one still seated goes on working: one copy, one stack per swing.
            await bench.PoisonSwing(new Brawler(), new Brawler(), expected: 1);
        }

        /// <summary>
        /// One series of attacks out of the shipped data, and the shipped augments seated on it by the
        /// registry that builds them for the game. Everything is the real thing: what a walk about
        /// composition would prove over a stand-in upgrade is that the stand-in composes.
        /// </summary>
        private sealed class Bench
        {
            private readonly AbilityAugmentCatalog _catalog;
            private readonly AbilityProvider _book;
            private readonly Dictionary<string, IAbilityAugment> _seated = new(StringComparer.Ordinal);

            internal Bench()
            {
                (_book, _catalog) = ShippedAbilityData.Load();
                Ability = (SeriesOfAttacksCast)_book.CreateAbility(Attacker);
            }

            internal SeriesOfAttacksCast Ability { get; }

            /// <summary>Turns of a stack as the applier's record declares them.</summary>
            internal float Turns => Declared(Applier, TurnsProperty);

            /// <summary>Turns the amplifier's record adds.</summary>
            internal float Extension => Declared(Amplifier, TurnsProperty);

            /// <summary>Attacks the third augment's record adds to the series.</summary>
            internal float AddedAttacks => Declared(MoreAttacks, AttacksProperty);

            /// <summary>Seats one copy of a record and hands the copy back — two sockets may hold two
            /// copies of one augment, and then the id names neither of them.</summary>
            internal IAbilityAugment Seat(string augmentId)
            {
                IAbilityAugment augment = Built(augmentId);
                augment.Apply(Ability);
                Assert.IsTrue(augment.Learned, $"'{augmentId}' refused the ability it was seated on");
                _seated[augmentId] = augment;
                return augment;
            }

            internal void Unseat(string augmentId)
            {
                Assert.IsTrue(_seated.Remove(augmentId, out IAbilityAugment? upgrade), $"'{augmentId}' was never seated");
                Unseat(upgrade!);
            }

            internal void Unseat(IAbilityAugment augment) => augment.Remove(Ability);

            /// <summary>One landing impact of the ability, handed to its riders the way the attack
            /// pipeline hands one over, and the stack it left behind.</summary>
            internal async Task<IEffect> PoisonSwing(Brawler caster, Brawler victim, int expected = 1)
            {
                await Ability.ApplyImpactRiders(new AbilityImpact(caster.Object, victim.Object, Mock.Of<IBattleField>(), Damage: DamageSnapshot.Of(DamageType.Physical, Blow))
                {
                    Source = Ability,
                    Kind = ImpactKind.Attack
                });

                List<IEffect> poison = victim.Poison;
                Assert.AreEqual(expected, poison.Count, $"the swing left a number of poison stacks other than {expected}");
                return poison[0];
            }

            /// <summary>The whole series, resolved through its own delivery — the road that decides how
            /// many impacts there are.</summary>
            internal async Task Series(Brawler target)
            {
                var owner = new Brawler();
                await new SoAsDefaultExecutionStrategy().Execute(Ability, owner.Object, [target.Object], FieldOf(owner, target));
            }

            private static IBattleField FieldOf(Brawler owner, params Brawler[] enemies)
            {
                var field = new Mock<IBattleField>();
                field.Setup(battlefield => battlefield.GetEnemies(It.IsAny<IFightable>())).Returns([.. enemies.Select(enemy => enemy.Object)]);
                field.Setup(battlefield => battlefield.GetAllies(It.IsAny<IFightable>())).Returns([owner.Object]);

                return field.Object;
            }

            private float Declared(string augmentId, string property) => Record(augmentId).UpgradeProperties[property];

            private IAbilityAugment Built(string augmentId)
            {
                IAbilityAugment? upgrade = _book.CreateUpgrade(Record(augmentId));
                Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");
                return upgrade;
            }

            private AbilityAugmentData Record(string augmentId)
            {
                AbilityAugmentData? record = _catalog.Find(augmentId);
                Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
                return record;
            }
        }

        /// <summary>
        /// A fighter who actually resolves the blows he is dealt: effects and modifier pipelines are the
        /// real ones, and a received attack is marked landed for what it carried — which is what the
        /// impact riders are then handed. A target that never resolves anything hands them evaded swings,
        /// and an applier that correctly does nothing on those would look like an applier that is broken.
        /// </summary>
        private sealed class Brawler
        {
            private readonly Mock<IFightable> _mock = new();

            internal Brawler()
            {
                var effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
                _mock.Setup(fighter => fighter.Attack(It.IsAny<IAttackContext>()))
                    .Returns((IAttackContext context) =>
                    {
                        context.Attacker.CombatEvents.Publish(new BeforeAttackEvent(context));
                        return Task.CompletedTask;
                    });
                _mock.Setup(fighter => fighter.ReceiveAttack(It.IsAny<IAttackContext>()))
                    .Returns((IAttackContext context) =>
                    {
                        context.Result = AttackResults.Succeed;
                        context.FinalDamage = DamageSnapshot.From(context.DamageComponents);
                        context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
                        return Task.CompletedTask;
                    });
            }

            internal IFightable Object => _mock.Object;

            /// <summary>Every poison stack he is carrying.</summary>
            internal List<IEffect> Poison => Object.Effects.GetBy(effect => effect.Status == StatusEffects.Poison).ToList();
        }

        /// <summary>Every count comes back at the bottom of its range and every chance at the top, so the
        /// series is the shortest one it may roll and nothing crits: what is being counted is impacts
        /// against stacks, and a roll that moved either would be measuring the dice.</summary>
        private sealed class ShortestSeries : IRandomNumberGenerator
        {
            public float RandFloat() => 1f;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }
    }
}
