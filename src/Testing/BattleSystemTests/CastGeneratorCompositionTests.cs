namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;

    /// <summary>
    /// Where the randomness a cast rolls on comes from. The activation context names the abstraction, so a
    /// cast can be driven anywhere — but the running game still has to roll on the engine generator: it is
    /// the stream the rest of combat rolls on, and a pure-C# one in production would quietly change what
    /// every "X% chance the cast is free" line means.
    /// </summary>
    [TestClass]
    public class CastGeneratorCompositionTests
    {
        private const string SilentAbility = "Ability_Test_Silent_Free_Cast";
        private const float Mana = 100f;
        private const float Cost = 50f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void TheGameBuildsItsCastStreamOnTheEngineGenerator()
        {
            MethodInfo? engine = typeof(Ability).GetMethod(nameof(Ability.EngineCastRandom), BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(engine, $"{nameof(Ability)} no longer names the generator the running game rolls its casts on");
            Assert.AreEqual(typeof(GodotRandomNumberGenerator), engine.ReturnType,
                "the game's cast stream stopped being the engine one — casts would roll on randomness the rest of combat knows nothing about");
            Assert.AreEqual(engine, Ability.CastRandomSource.Method,
                "the cast stream is built from something other than the engine source: outside the sandboxes a cast must roll on Godot's generator");
        }

        [TestMethod]
        public async Task ACastRollsOnTheGeneratorTheSourceHandsOut()
        {
            // The other half of the pin: the source is not decoration, it is where the stream comes from.
            // The cast is driven for real here — which is what the abstraction in the context bought.
            var rolls = new CountingRandom();
            using var scope = new CastRandomScope(rolls);
            var caster = Caster();
            caster.ModifierHandler.Add(new ChanceFreeCastActivationContextModifier(() => 1f));
            var ability = SilentCast.Named(SilentAbility);
            ability.SetOwner(caster);

            await ability.Execute([], Mock.Of<IBattleField>());

            Assert.AreEqual(1, rolls.Count, "the chance mutator rolled on a generator the source never handed out");
            Assert.AreEqual(Mana, caster.CurrentMana, Tolerance, "a guaranteed free cast still charged the caster");
        }

        private static ConditionOwner Caster()
        {
            var caster = new ConditionOwner();
            caster.SetMaximum(EntityParameter.Mana, Mana);
            caster.CurrentMana = Mana;

            return caster;
        }

        /// <summary>Counts what was asked of it: which generator a cast actually rolled on is invisible
        /// from the outside otherwise.</summary>
        private sealed class CountingRandom : IRandomNumberGenerator
        {
            private readonly IRandomNumberGenerator _rolls = new DefaultRandomNumberGenerator(seed: 1);

            public int Count { get; private set; }

            public float RandFloat()
            {
                Count++;
                return _rolls.RandFloat();
            }

            public float RandFloatRange(float min, float max)
            {
                Count++;
                return _rolls.RandFloatRange(min, max);
            }

            public int RandIntRange(int min, int max)
            {
                Count++;
                return _rolls.RandIntRange(min, max);
            }

            public float RandFloatN(float mean, float deviation)
            {
                Count++;
                return _rolls.RandFloatN(mean, deviation);
            }

            public uint RandInt()
            {
                Count++;
                return _rolls.RandInt();
            }

            public long RandWeighted(float[] weights)
            {
                Count++;
                return _rolls.RandWeighted(weights);
            }

            public long RandWeighted(ReadOnlySpan<float> weights)
            {
                Count++;
                return _rolls.RandWeighted(weights);
            }

            public void Randomize() => _rolls.Randomize();
        }

        /// <summary>A cast with no delivery of its own: what it pays for itself is the whole of it.</summary>
        private sealed class SilentCast(AbilityBaseData data) : Ability(data)
        {
            public static SilentCast Named(string id) => new(new AbilityBaseData
            {
                Id = id,
                CostValue = (int)Cost,
                CostsType = Costs.Mana
            });

            public override IAbility Copy() => new SilentCast(Data);

            protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => Task.CompletedTask;
        }
    }
}
