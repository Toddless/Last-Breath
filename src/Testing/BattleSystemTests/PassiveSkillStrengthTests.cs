namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Entity.Components;
    using Moq;

    /// <summary>
    /// The <see cref="ISkill.IsStronger"/> contract as <c>PassiveSkillsComponent.AddSkill</c> reads it:
    /// <c>true</c> means "I am the stronger one, throw the incoming skill away". An inverted comparison
    /// compiles and only shows up in battle as a silently downgraded passive, so every implementation
    /// is pinned here — a collision must be won by the stronger instance in BOTH insertion orders.
    /// </summary>
    [TestClass]
    public class PassiveSkillStrengthTests
    {
        /// <summary>One (stronger, weaker) pair per passive that carries a strength field.
        /// The pair differs only in the field that decides the strength of the effect.</summary>
        public static IEnumerable<object[]> RankedPassives()
        {
            yield return Pair(new AcceleratorPassiveSkill(3), new AcceleratorPassiveSkill(1));
            yield return Pair(new BastionPassiveSkill(0.5f), new BastionPassiveSkill(0.2f));
            yield return Pair(new BleedingPassiveSkill(0.6f, 3, 5), new BleedingPassiveSkill(0.2f, 3, 5));
            yield return Pair(new BloodthirstyPassiveSkill(3, 0.3f), new BloodthirstyPassiveSkill(3, 0.1f));
            yield return Pair(new BurningPassiveSkill(0.6f, 3, 5), new BurningPassiveSkill(0.2f, 3, 5));
            yield return Pair(new CounterAttackPassiveSkill(0.8f), new CounterAttackPassiveSkill(0.3f));
            yield return Pair(new CreatorsNaturePassiveSkill(0.05f, 0.05f, 0.5f),
                new CreatorsNaturePassiveSkill(0.05f, 0.05f, 0.2f));
            yield return Pair(new CriticalLeechPassiveSkill(0.3f), new CriticalLeechPassiveSkill(0.1f));
            yield return Pair(new CriticalManaLeechPassiveSkill(0.3f), new CriticalManaLeechPassiveSkill(0.1f));
            yield return Pair(new CurrentHealthRegenerationPassiveSkill(0.1f),
                new CurrentHealthRegenerationPassiveSkill(0.03f));
            yield return Pair(new DecompositionPassiveSkill(3, 5, 0.1f), new DecompositionPassiveSkill(3, 5, 0.05f));
            yield return Pair(new EchoPassiveSkill(0.5f, 2), new EchoPassiveSkill(0.2f, 2));
            yield return Pair(new ExecutePassiveSkill(0.4f), new ExecutePassiveSkill(0.15f));
            yield return Pair(new FirstStrikePassiveSkill(0.6f), new FirstStrikePassiveSkill(0.25f));
            yield return Pair(new GiftFromTheGoddessPassiveSkill(0.3f), new GiftFromTheGoddessPassiveSkill(0.1f));
            yield return Pair(new IceMeteorPassiveSkill(120f), new IceMeteorPassiveSkill(40f));
            yield return Pair(new ManaBurnPassiveSkill(0.25f), new ManaBurnPassiveSkill(0.05f));
            yield return Pair(new ManaOnAttackPassiveSkill(15f), new ManaOnAttackPassiveSkill(5f));
            yield return Pair(new ManaRegenerationPassiveSkill(0.1f), new ManaRegenerationPassiveSkill(0.03f));
            yield return Pair(new ManaResonancePassiveSkill(0.3f), new ManaResonancePassiveSkill(0.15f));
            yield return Pair(new ManaToBarrierPassiveSkill(0.5f), new ManaToBarrierPassiveSkill(0.2f));
            yield return Pair(new MeteorPassiveSkill(120f), new MeteorPassiveSkill(40f));
            yield return Pair(new PoisonedClaws(0.5f, 4), new PoisonedClaws(0.2f, 4));
            yield return Pair(new PorcupinePassiveSkill(0.4f, 0.5f), new PorcupinePassiveSkill(0.15f, 0.5f));
            yield return Pair(new RegenerationPassiveSkill(0.1f), new RegenerationPassiveSkill(0.03f));
            yield return Pair(new ResonancePassiveSkill(0.08f, 0.1f), new ResonancePassiveSkill(0.04f, 0.1f));
            yield return Pair(new RighteousWrathPassiveSkill(0.5f, 3, 3, 2),
                new RighteousWrathPassiveSkill(0.2f, 3, 3, 2));
            yield return Pair(new ServantHellPassiveSkill(0.4f), new ServantHellPassiveSkill(0.15f));
            yield return Pair(new SilentFuryPassive(0.4f), new SilentFuryPassive(0.15f));
            yield return Pair(new SoulDevouringPassiveSkill(40f), new SoulDevouringPassiveSkill(10f));
            // Two copies of one authored stat passive, told apart only by how far their lines move: the
            // numbers are the record's, so copies differ only where a scaling hand has been.
            yield return Pair(StatPassive(0.4f), StatPassive(0.1f));
            // Same step, fatter bonus: the pair that separates the two fields of Trapped Beast.
            yield return Pair(new TrappedBeastPassiveSkill(0.1f, 0.4f), new TrappedBeastPassiveSkill(0.1f, 0.1f));
            yield return Pair(new VampireAttackPassiveSkill(0.3f), new VampireAttackPassiveSkill(0.1f));
        }

        /// <summary>Passives without a strength field: every instance is interchangeable, so none of them
        /// may claim to be stronger and block its own replacement.</summary>
        public static IEnumerable<object[]> ParameterlessPassives()
        {
            yield return Pair(new ArmorPiercingPassiveSkill(), new ArmorPiercingPassiveSkill());
            yield return Pair(new ChainAttackPassiveSkill(), new ChainAttackPassiveSkill());
            yield return Pair(new IncinerationPassiveSkill(), new IncinerationPassiveSkill());
            yield return Pair(new LuckyCriticalChancePassiveSkill(), new LuckyCriticalChancePassiveSkill());
            yield return Pair(new NoCriticalHitsPassiveSkill(), new NoCriticalHitsPassiveSkill());
            yield return Pair(new SoullessPassiveSkill(), new SoullessPassiveSkill());
            yield return Pair(new TrueStrikePassiveSkill(), new TrueStrikePassiveSkill());
            yield return Pair(new UnshackledPassiveSkill(), new UnshackledPassiveSkill());
        }

        [TestMethod]
        [DynamicData(nameof(RankedPassives), DynamicDataSourceType.Method)]
        public void StrongerPassiveWinsTheCollisionInBothOrders(string name, ISkill strong, ISkill weak)
        {
            Assert.IsTrue(strong.IsStronger(weak), $"{name}: the stronger instance must report itself stronger");
            Assert.IsFalse(weak.IsStronger(strong), $"{name}: the weaker instance must not report itself stronger");

            Assert.AreSame(strong, Survivor(weak, strong), $"{name}: strong added last must replace weak");
            Assert.AreSame(strong, Survivor(strong, weak), $"{name}: weak added last must not evict strong");
        }

        [TestMethod]
        [DynamicData(nameof(ParameterlessPassives), DynamicDataSourceType.Method)]
        public void ParameterlessPassiveNeverBlocksItsOwnReplacement(string name, ISkill first, ISkill second)
        {
            Assert.IsFalse(first.IsStronger(second), $"{name}: identical instances — neither can be stronger");

            Assert.AreSame(second, Survivor(first, second), $"{name}: the incoming grant must take the slot");
            Assert.AreEqual(1, Skills(first, second).Count, $"{name}: a collision must leave exactly one instance");
        }

        /// <summary>Trapped Beast reads two fields that pull in opposite directions: a smaller
        /// missing-health step fires more often, so the smaller step is the stronger skill.</summary>
        [TestMethod]
        public void TrappedBeastRanksTheSmallerHealthStepHigher()
        {
            var strong = new TrappedBeastPassiveSkill(0.1f, 0.2f);
            var weak = new TrappedBeastPassiveSkill(0.4f, 0.2f);

            Assert.IsTrue(strong.IsStronger(weak));
            Assert.IsFalse(weak.IsStronger(strong));
            Assert.AreSame(strong, Survivor(strong, weak));
            Assert.AreSame(strong, Survivor(weak, strong));
        }

        [TestMethod]
        public void EveryPassiveSkillIsPinnedByAStrengthCase()
        {
            HashSet<Type> covered =
            [
                ..RankedPassives().Select(row => ((ISkill)row[1]).GetType()),
                ..ParameterlessPassives().Select(row => ((ISkill)row[1]).GetType())
            ];

            List<Type> declared = typeof(Skill).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(typeof(Skill)))
                .ToList();

            CollectionAssert.AreEquivalent(declared, covered.ToList(),
                "a new passive skill must arrive with a strength case here — an inverted IsStronger is invisible to the compiler");
        }

        private static object[] Pair(ISkill stronger, ISkill weaker) => [stronger.GetType().Name, stronger, weaker];

        /// <summary>One authored stat passive, built the way a record builds it — same id both times, so
        /// the two copies collide the way every other pair here does.</summary>
        private static StatPassiveSkill StatPassive(float value)
        {
            const string id = "Passive_Skill_Stats_Ranked";
            return StatPassiveSkill.Create(id,
                new RecordProperties(id, new Dictionary<string, float> { ["Armor:Increase"] = value }));
        }

        /// <summary>The skill left in the slot after the given skills collided on their shared Id.</summary>
        private static ISkill? Survivor(params ISkill[] added) => Component(added).GetSkill(added[0].Id);

        private static IReadOnlyList<ISkill> Skills(params ISkill[] added) => Component(added).Skills;

        /// <summary>Suppressed on purpose: <c>AddSkill</c> resolves the collision before it attaches
        /// anything, so the policy is testable without a live owner behind the mock.</summary>
        private static IPassiveSkillsComponent Component(params ISkill[] added)
        {
            var component = new PassiveSkillsComponent(Mock.Of<IFightable>());
            component.Suppress();
            foreach (ISkill skill in added) component.AddSkill(skill);
            return component;
        }
    }
}
