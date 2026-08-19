namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Effects;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;
    using Godot;

    /// <summary>
    /// The single point every combat chance is rolled through, and the luck that bends it. Claimed here:
    /// the rule itself (one draw, better of two when lucky, worse of two when unlucky, both kinds of source
    /// cancelling), the boundaries (an impossible chance never passes, a certain one always does), the
    /// landing of an attack now that the verdict is returned rather than published (order evade → block,
    /// and the marks that skip a branch), and the Luck effect actually making its bearer's crits lucky.
    /// </summary>
    [TestClass]
    public class ChanceRollTests
    {
        private const float EvasionScalingFactor = 10000f; // mirror of Calculations' curve constant

        [TestMethod]
        public void ALuckyRollKeepsTheBetterOfTwoDraws()
        {
            Assert.IsFalse(ChanceRoll.Roll(0.5f, new Draws(0.9f, 0.1f)), "a plain roll must be settled by the first draw alone");
            Assert.IsTrue(ChanceRoll.Roll(0.5f, new Draws(0.9f, 0.1f), ChanceLuck.Lucky), "luck must keep the better of the two draws");
        }

        [TestMethod]
        public void AnUnluckyRollKeepsTheWorseOfTwoDraws()
        {
            Assert.IsTrue(ChanceRoll.Roll(0.5f, new Draws(0.1f, 0.9f)), "a plain roll must be settled by the first draw alone");
            Assert.IsFalse(ChanceRoll.Roll(0.5f, new Draws(0.1f, 0.9f), ChanceLuck.Unlucky), "bad luck must keep the worse of the two draws");
        }

        [TestMethod]
        public void APlainRollDrawsOnceAndALuckyOneTwice()
        {
            var plain = new Draws(0.9f, 0.1f);
            var lucky = new Draws(0.9f, 0.1f);
            var unlucky = new Draws(0.9f, 0.1f);

            ChanceRoll.Roll(0.5f, plain);
            ChanceRoll.Roll(0.5f, lucky, ChanceLuck.Lucky);
            ChanceRoll.Roll(0.5f, unlucky, ChanceLuck.Unlucky);

            Assert.AreEqual(1, plain.Taken);
            Assert.AreEqual(2, lucky.Taken);
            Assert.AreEqual(2, unlucky.Taken);
        }

        [TestMethod]
        public void LuckAndBadLuckCancelToOneHonestRoll()
        {
            var parameters = new EntityParametersComponent();
            parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
            parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Unlucky);

            Assert.AreEqual(ChanceLuck.Neutral, parameters.GetChanceLuck(EntityParameter.CriticalChance),
                "a fighter both blessed and cursed on one parameter rolls honestly");

            var draws = new Draws(0.9f, 0.1f);
            Assert.IsFalse(ChanceRoll.Roll(0.5f, draws, parameters.GetChanceLuck(EntityParameter.CriticalChance)));
            Assert.AreEqual(1, draws.Taken, "the cancelled pair must cost the stream a single draw");
        }

        [TestMethod]
        public void AnImpossibleChanceNeverPassesAndACertainOneAlwaysDoes()
        {
            foreach (ChanceLuck luck in Enum.GetValues<ChanceLuck>())
            {
                Assert.IsFalse(ChanceRoll.Roll(0f, new Draws(0f, 0f), luck), $"a zero chance passed on the lowest draw ({luck})");
                Assert.IsTrue(ChanceRoll.Roll(1f, new Draws(1f, 1f), luck), $"a certain chance failed on the highest draw ({luck})");
            }
        }

        [TestMethod]
        public void LuckIsCountedBySourceAndLeavesWithTheLastOne()
        {
            var parameters = new EntityParametersComponent();
            parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
            parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);

            parameters.RemoveChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
            Assert.AreEqual(ChanceLuck.Lucky, parameters.GetChanceLuck(EntityParameter.CriticalChance),
                "one source leaving must not take the other's luck with it");

            parameters.RemoveChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);
            Assert.AreEqual(ChanceLuck.Neutral, parameters.GetChanceLuck(EntityParameter.CriticalChance));
        }

        [TestMethod]
        public void LuckIsAddressedByParameter()
        {
            var parameters = new EntityParametersComponent();
            parameters.AddChanceLuck(EntityParameter.CriticalChance, ChanceLuck.Lucky);

            Assert.AreEqual(ChanceLuck.Neutral, parameters.GetChanceLuck(EntityParameter.Evade),
                "luck is bought for a named chance, not for the fighter at large");
        }

        [TestMethod]
        public void EvasionIsRolledBeforeBlock()
        {
            var context = Attack(Defender(evade: EvasionScalingFactor, block: 1f));

            var draws = new Draws(0f, 0f);
            Assert.AreEqual(AttackResults.Evaded, Calculations.ResolveAttackOutcome(context, draws.RandFloat));
            Assert.AreEqual(1, draws.Taken, "a hit that got away is never offered to the block roll");
        }

        [TestMethod]
        public void AnUnevadableAttackIsNeverEvaded()
        {
            var context = Attack(Defender(evade: EvasionScalingFactor * 1000f, block: 0f));
            context.IsUnevadable = true;

            var draws = new Draws(0f, 0f);
            Assert.AreEqual(AttackResults.Succeed, Calculations.ResolveAttackOutcome(context, draws.RandFloat),
                "the unevadable mark must survive the move off CalculateSucceeded");
            Assert.AreEqual(1, draws.Taken, "a branch that cannot fire must not burn a roll — only the block roll is left");
        }

        [TestMethod]
        public void AnUnblockableAttackIsNeverBlocked()
        {
            var context = Attack(Defender(evade: 0f, block: 1f));
            context.IsUnblockable = true;

            Assert.AreEqual(AttackResults.Succeed, Calculations.ResolveAttackOutcome(context, new Draws(0f, 0f).RandFloat));
        }

        [TestMethod]
        public void EvasionAtItsBoundaries()
        {
            // Accuracy at or above evasion leaves no chance at all; a certain block lands on the highest draw.
            var noChance = Attack(Defender(evade: EvasionScalingFactor, block: 0f));
            noChance.RawAccuracy = EvasionScalingFactor;
            Assert.AreEqual(AttackResults.Succeed, Calculations.ResolveAttackOutcome(noChance, new Draws(0f, 0f).RandFloat));

            var certain = Attack(Defender(evade: 0f, block: 1f));
            Assert.AreEqual(AttackResults.Blocked, Calculations.ResolveAttackOutcome(certain, new Draws(1f, 1f).RandFloat));
        }

        [TestMethod]
        public void TheDefendersLuckSettlesHisEvasionRoll()
        {
            // Evade 10000 against no accuracy is an even chance: the first draw misses it, the second makes it.
            var draws = new Draws(0.9f, 0.1f);
            var context = Attack(Defender(evade: EvasionScalingFactor, block: 0f, lucky: EntityParameter.Evade));

            Assert.AreEqual(AttackResults.Evaded, Calculations.ResolveAttackOutcome(context, draws.RandFloat));
            Assert.AreEqual(2, draws.Taken);
        }

        [TestMethod]
        public async Task TheLuckEffectMakesItsBearersCriticalRollsLucky()
        {
            var owner = Fighter(criticalChance: 0.5f);
            await Luck().Apply(Laying(owner));

            Assert.IsTrue(RollsCritical(owner, new Draws(0.9f, 0.1f)),
                "'Effect_Lucky_Crit_Chance' left the crit roll honest — the better draw was not taken");
            Assert.IsFalse(RollsCritical(Fighter(criticalChance: 0.5f), new Draws(0.9f, 0.1f)),
                "the same draws must fail without the effect, or the claim above proves nothing");
        }

        [TestMethod]
        public async Task LuckLeavesWhenTheEffectDoes()
        {
            var owner = Fighter(criticalChance: 0.5f);
            IEffect luck = Luck();
            await luck.Apply(Laying(owner));
            luck.Remove();

            Assert.AreEqual(ChanceLuck.Neutral, owner.Parameters.GetChanceLuck(EntityParameter.CriticalChance));
            Assert.IsFalse(RollsCritical(owner, new Draws(0.9f, 0.1f)), "the crit roll stayed lucky after the effect expired");
        }

        [TestMethod]
        public async Task AnApplicationTheStackingRulesTurnedAwayLeavesNoLuckBehind()
        {
            // Luck is one stack: the second application only refreshes the standing one. Were the turned-away
            // instance to mark the fighter anyway, nothing would ever take that mark off him again.
            var owner = Fighter(criticalChance: 0.5f);
            IEffect standing = Luck();
            await standing.Apply(Laying(owner));
            await Luck().Apply(Laying(owner));

            standing.Remove();

            Assert.AreEqual(ChanceLuck.Neutral, owner.Parameters.GetChanceLuck(EntityParameter.CriticalChance));
        }

        [TestMethod]
        public void TheCriticalRollAtItsBoundaries()
        {
            Assert.IsFalse(RollsCritical(Fighter(criticalChance: 0f), new Draws(0f, 0f)), "a zero crit chance critted on the lowest draw");
            Assert.IsTrue(RollsCritical(Fighter(criticalChance: 1f), new Draws(1f, 1f)), "a certain crit chance failed on the highest draw");
        }

        private static bool RollsCritical(IFightable owner, Draws draws)
        {
            using var rolls = new CombatRandomScope(draws);
            var ability = new Mock<IAbility>();
            ability.Setup(a => a.ValueOr(It.IsAny<string>(), It.IsAny<float>())).Returns(0f);
            return ability.Object.RollsCritical(owner);
        }

        private static IEffect Luck() => new LuckyCritChanceEffect(duration: 3, maxStacks: 1);

        private static EffectApplyingContext Laying(IFightable owner) =>
            new() { Caster = owner, Target = owner, Source = "Test_Luck" };

        private static ConditionOwner Fighter(float criticalChance)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, 1000f);
            fighter.SetMaximum(EntityParameter.CriticalChance, criticalChance);
            fighter.CurrentHealth = 1000f;
            return fighter;
        }

        private static FakeAttack Attack(IFightable target) => new() { Target = target };

        private static IFightable Defender(float evade, float block, EntityParameter? lucky = null)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            parameters.SetupGet(p => p.Evade).Returns(evade);
            parameters.SetupGet(p => p.BlockChance).Returns(block);
            if (lucky != null) parameters.Setup(p => p.GetChanceLuck(lucky.Value)).Returns(ChanceLuck.Lucky);

            var defender = new Mock<IFightable>();
            defender.SetupGet(f => f.Parameters).Returns(parameters.Object);
            defender.SetupGet(f => f.IsAlive).Returns(true);
            return defender.Object;
        }

        /// <summary>Hands out a scripted sequence of draws and counts what was taken: which rolls a resolve
        /// burns is part of the contract — a branch that cannot fire must not shift anybody else's stream.</summary>
        private sealed class Draws(params float[] rolls) : IRandomNumberGenerator
        {
            public int Taken { get; private set; }

            public float RandFloat() => Taken < rolls.Length
                ? rolls[Taken++]
                : throw new InvalidOperationException($"the resolve drew more than the scripted {rolls.Length} roll(s)");

            public float RandFloatRange(float min, float max) => throw new NotSupportedException();
            public int RandIntRange(int min, int max) => throw new NotSupportedException();
            public float RandFloatN(float mean, float deviation) => throw new NotSupportedException();
            public uint RandInt() => throw new NotSupportedException();
            public long RandWeighted(float[] weights) => throw new NotSupportedException();
            public long RandWeighted(ReadOnlySpan<float> weights) => throw new NotSupportedException();
            public void Randomize() => throw new NotSupportedException();
        }

        /// <summary>Pure-C# stand-in for AttackContext: the real one carries the engine generator.</summary>
        private sealed class FakeAttack : IAttackContext
        {
            private readonly Dictionary<DamageType, float> _damageComponents = [];

            public RandomNumberGenerator Rnd => null!;
            public IFightable Attacker { get; init; } = null!;
            public required IFightable Target { get; init; }
            public IAttackContextScheduler AttackContextScheduler => null!;
            public AttackResults Result { get; set; }
            public float BaseDamage { get; init; }
            public IReadOnlyDictionary<DamageType, float> DamageComponents => _damageComponents;
            public float TotalDamage => _damageComponents.Values.Sum();
            public float RawCriticalChance { get; set; }
            public float RawCriticalDamage { get; set; }
            public float RawAccuracy { get; set; }
            public DamageSnapshot FinalDamage { get; set; }
            public bool IsCritical { get; set; }
            public bool ForceCriticalAttack { get; set; }
            public bool IsUnevadable { get; set; }
            public bool IsUnblockable { get; set; }
            public bool IsValid => true;
            public string? SourceAbilityId { get; set; }
            public int Index { get; set; }
            public int TotalCount { get; set; } = 1;
            public bool IsFirst => Index == 0;
            public bool IsLast => Index == TotalCount - 1;
            public int ReactionDepth => 0;
            public bool IsAnswer => false;

            public void AddDamage(DamageType type, float amount) => _damageComponents[type] = _damageComponents.GetValueOrDefault(type, 0f) + amount;
            public void SetDamage(DamageType type, float amount) => _damageComponents[type] = amount;
            public void ScaleDamage(float factor) => throw new NotSupportedException();
            public IAttackContext CreateReaction(IFightable attacker, IFightable target, float baseDamage) => throw new NotSupportedException();
            public bool Schedule() => throw new NotSupportedException();
        }
    }
}
