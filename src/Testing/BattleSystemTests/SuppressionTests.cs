namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Battle.Abilities;
    using Core.Battle.DamageResolution;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    /// <summary>Suppression: the defender's chance (<c>SuppressChance</c>) to take a fraction (<c>Suppress</c>)
    /// less of an incoming hit. Pins the rule itself (one roll per hit, every component scaled, every damage
    /// cause covered), its place in the pipeline (after mitigation, before the absorption chain) and the fact
    /// that a defender without the stat never burns a roll. Both call shapes are covered: the seeded/scripted
    /// stream tests take the explicit-generator overload, the two ProductionCall tests use the two-argument form
    /// the four combat call sites actually use, so the fallback generator inside Calculations is executed too.</summary>
    [TestClass]
    public class SuppressionTests
    {
        private const float ArmorScalingFactor = 10000f; // mirror of Calculations' curve constant

        [TestMethod]
        public void FullChance_ReducesEveryComponentByTheSuppressFraction()
        {
            var target = Defender((EntityParameter.SuppressChance, 1f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f), (DamageType.Poison, 40f));

            Calculations.CalculateMitigation(context, target.Object, new ScriptedRandom(0f));

            Assert.AreEqual(50f, context.DamageComponents[DamageType.Physical], 0.001f);
            Assert.AreEqual(20f, context.DamageComponents[DamageType.Poison], 0.001f, "suppression is type-agnostic — even unmitigated poison is cut");
        }

        [TestMethod]
        public void ZeroChance_LeavesDamageUntouchedAndBurnsNoRoll()
        {
            var target = Defender((EntityParameter.SuppressChance, 0f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f));
            var rnd = new ScriptedRandom(0f);

            Calculations.CalculateMitigation(context, target.Object, rnd);

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Physical], 0.001f);
            Assert.AreEqual(0, rnd.Draws, "a defender without the stat must not shift anybody else's rolls");
        }

        [TestMethod]
        public void ProductionCall_WithoutAGenerator_SuppressesAtFullChance()
        {
            // The four combat call sites pass no generator, so this is the branch that actually runs in battle:
            // Calculations falls back to its own generator. At chance 1 the outcome cannot depend on the stream,
            // so the assertion stays deterministic while still executing the fallback.
            var target = Defender((EntityParameter.SuppressChance, 1f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f));

            Calculations.CalculateMitigation(context, target.Object);

            Assert.AreEqual(50f, context.DamageComponents[DamageType.Physical], 0.001f, "the fallback generator must roll, not skip the mechanic");
        }

        [TestMethod]
        public void ProductionCall_WithoutAGenerator_LeavesDamageUntouchedAtZeroChance()
        {
            var target = Defender((EntityParameter.SuppressChance, 0f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f));

            Calculations.CalculateMitigation(context, target.Object);

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Physical], 0.001f);
        }

        [TestMethod]
        public void FailedRoll_LeavesDamageUntouched()
        {
            var target = Defender((EntityParameter.SuppressChance, 0.3f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Ability, (DamageType.Physical, 100f));

            Calculations.CalculateMitigation(context, target.Object, new ScriptedRandom(0.9f));

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Physical], 0.001f);
        }

        [TestMethod]
        public void OneRollPerHit_NotPerComponent()
        {
            var target = Defender((EntityParameter.SuppressChance, 0.5f), (EntityParameter.Suppress, 0.5f));
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f), (DamageType.Pure, 100f), (DamageType.Poison, 100f));
            // A per-component roll would succeed on the first draw and fail on the next two.
            var rnd = new ScriptedRandom(0f, 0.99f, 0.99f);

            Calculations.CalculateMitigation(context, target.Object, rnd);

            Assert.AreEqual(1, rnd.Draws, "a hit is either suppressed or it is not");
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Physical], 0.001f);
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Pure], 0.001f);
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Poison], 0.001f);
        }

        [TestMethod]
        public void EveryDamageCauseIsSuppressed()
        {
            foreach (var cause in Enum.GetValues<DamageCause>())
            {
                var target = Defender((EntityParameter.SuppressChance, 1f), (EntityParameter.Suppress, 0.25f));
                var context = Damage(cause, (DamageType.Physical, 100f));

                Calculations.CalculateMitigation(context, target.Object, new ScriptedRandom(0f));

                Assert.AreEqual(75f, context.DamageComponents[DamageType.Physical], 0.001f, $"cause {cause} must obey suppression");
            }
        }

        [TestMethod]
        public void PartialChance_FiresAtTheDeclaredRateOnASeededStream()
        {
            const int hits = 10000;
            const float chance = 0.3f;
            var rnd = new DefaultRandomNumberGenerator(seed: 20260730);
            var target = Defender((EntityParameter.SuppressChance, chance), (EntityParameter.Suppress, 0.5f));

            int suppressed = 0;
            for (int i = 0; i < hits; i++)
            {
                var context = Damage(DamageCause.Effect, (DamageType.Physical, 100f));
                Calculations.CalculateMitigation(context, target.Object, rnd);
                if (context.DamageComponents[DamageType.Physical] < 100f) suppressed++;
            }

            float rate = suppressed / (float)hits;
            Assert.AreEqual(chance, rate, 0.02f, $"expected ~{chance} of hits suppressed, got {rate}");
        }

        [TestMethod]
        public void SuppressionRunsAfterMitigationAndBeforeTheAbsorptionChain()
        {
            // 200 physical: armor halves it to 100, suppression halves it again to 50, and only that 50
            // reaches the barrier. Were suppression to run after the chain, the barrier would eat 100.
            var target = Defender(
                (EntityParameter.SuppressChance, 1f),
                (EntityParameter.Suppress, 0.5f),
                (EntityParameter.Armor, ArmorScalingFactor));
            WithAbsorption(target, currentHealth: 500f, barrier: 100f);
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 200f));

            Calculations.CalculateMitigation(context, target.Object, new ScriptedRandom(0f));
            Assert.AreEqual(50f, context.TotalDamage, 0.001f, "armor then suppression");

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object, context.TotalDamage);

            Assert.AreEqual(50f, context.AbsorbedByBarrier, 0.001f, "the barrier soaks the SUPPRESSED number");
            Assert.AreEqual(0f, remaining, 0.001f);
            Assert.AreEqual(50f, target.Object.CurrentBarrier, 0.001f, "half the barrier survives the hit");
        }

        private static DamageContext Damage(DamageCause cause, params (DamageType Type, float Amount)[] components)
        {
            var source = new Mock<IFightable>();
            source.SetupGet(f => f.InstanceId).Returns("source");
            source.SetupGet(f => f.Parameters).Returns(new Mock<IEntityParametersComponent>().Object);

            var context = new DamageContext { Source = source.Object, Cause = cause };
            foreach ((DamageType type, float amount) in components)
                context.Add(type, amount);
            return context;
        }

        private static Mock<IFightable> Defender(params (EntityParameter Parameter, float Value)[] values)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            foreach ((EntityParameter parameter, float value) in values)
                parameters.Setup(p => p.GetValueForParameter(parameter)).Returns(value);

            // ApplyArmor reads the named properties, not GetValueForParameter — mirror the values there.
            parameters.SetupGet(p => p.Armor).Returns(values.FirstOrDefault(v => v.Parameter == EntityParameter.Armor).Value);
            parameters.SetupGet(p => p.ArmorPenetration).Returns(values.FirstOrDefault(v => v.Parameter == EntityParameter.ArmorPenetration).Value);

            var defender = new Mock<IFightable>();
            defender.SetupGet(f => f.Parameters).Returns(parameters.Object);
            defender.SetupGet(f => f.IsAlive).Returns(true);
            return defender;
        }

        private static void WithAbsorption(Mock<IFightable> defender, float currentHealth, float barrier)
        {
            var effects = new Mock<IEffectsComponent>();
            effects.Setup(e => e.GetBy(It.IsAny<Func<IEffect, bool>>())).Returns(new List<IEffect>());

            defender.SetupGet(f => f.CurrentHealth).Returns(currentHealth);
            defender.SetupProperty(f => f.CurrentBarrier, barrier);
            defender.SetupGet(f => f.Effects).Returns(effects.Object);
        }

        /// <summary>Hands out a fixed sequence of rolls and counts how many were taken — the stream itself is
        /// part of the contract: suppression must draw exactly once per hit and never for a defender without the stat.</summary>
        private sealed class ScriptedRandom(params float[] rolls) : IRandomNumberGenerator
        {
            private int _draws;

            public int Draws => _draws;

            public float RandFloat() => _draws < rolls.Length
                ? rolls[_draws++]
                : throw new InvalidOperationException($"Suppression drew more than the scripted {rolls.Length} roll(s)");

            public float RandFloatRange(float min, float max) => throw new NotSupportedException();
            public int RandIntRange(int min, int max) => throw new NotSupportedException();
            public float RandFloatN(float mean, float deviation) => throw new NotSupportedException();
            public uint RandInt() => throw new NotSupportedException();
            public long RandWeighted(float[] weights) => throw new NotSupportedException();
            public long RandWeighted(ReadOnlySpan<float> weights) => throw new NotSupportedException();
            public void Randomize() => throw new NotSupportedException();
        }
    }
}
