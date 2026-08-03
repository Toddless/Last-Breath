namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Battle.Source;
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
    /// Where the randomness a cast rolls on comes from, and who decides it. The activation context names
    /// the abstraction, so a cast can be driven anywhere — but the running game still has to roll on the
    /// engine generator: it is the stream the rest of combat rolls on, and a pure-C# one in production
    /// would quietly change what every "X% chance the cast is free" line means. Which is a COMPOSITION
    /// decision here, not a default of the domain, because the engine generator is a native object: a
    /// host without Godot that builds one dies whole (0xC0000005), taking every remaining test with it
    /// instead of failing one. So the domain default is the harmless implementation and the projects
    /// that boot inside the engine install the real one — both halves held below.
    /// </summary>
    [TestClass]
    public class CastGeneratorCompositionTests
    {
        private const string SilentAbility = "Ability_Test_Silent_Free_Cast";
        private const float Mana = 100f;
        private const float Cost = 50f;
        private const float Tolerance = 0.001f;

        /// <summary>The bootstraps that boot inside Godot and therefore owe their casts the engine
        /// generator. They are the Godot projects, off the test assembly's references, so the fact that
        /// they still install it is read off their source.</summary>
        private static readonly string[] s_bootstraps =
        [
            Path.Combine("Main", "Services", "GameServiceProvider.cs"),
            Path.Combine("Battle", "Services", "GameServiceProvider.cs"),
        ];

        /// <summary>The sources the game ships, found by walking up from the test binaries.</summary>
        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;
                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        [TestMethod]
        public async Task ACastNobodyPinnedAGeneratorForRollsWithoutTouchingTheEngine()
        {
            // Nothing is pinned here on purpose: this is the cast a newly written test makes without
            // knowing the seat exists. It has to end in a value rather than in a dead process — a
            // native generator built where Godot is not running kills the host past the reach of the
            // assertion that would have reported it, so the guard has to come before the cast.
            Assert.AreEqual(StaticMethod(nameof(Ability.DefaultCastRandom)), Ability.CastRandomSource.Method,
                "the cast seat defaults to something other than the engine-free source — the first unwrapped cast kills the run instead of failing a test");
            var caster = Caster();
            var ability = SilentCast.Named(SilentAbility);
            ability.SetOwner(caster);

            await ability.Execute([], Mock.Of<IBattleField>());

            Assert.AreEqual(Mana - Cost, caster.CurrentMana, Tolerance, "the cast never got as far as paying for itself");
        }

        [TestMethod]
        public void ComposingTheGamePutsTheEngineGeneratorInTheCastSeat()
        {
            MethodInfo engine = StaticMethod(nameof(Ability.EngineCastRandom));
            Assert.AreEqual(typeof(GodotRandomNumberGenerator), engine.ReturnType,
                "the game's cast stream stopped being the engine one — casts would roll on randomness the rest of combat knows nothing about");

            try
            {
                BattleSystemModuleDependencies.UseEngineCastRandom();

                Assert.AreEqual(engine, Ability.CastRandomSource.Method,
                    "composing the game no longer seats the engine source: outside the sandboxes a cast must roll on Godot's generator");
            }
            finally
            {
                // The seat goes back whatever the assertion said. Only the assignment happened here —
                // a generator nobody built is harmless, one left armed for the next test is the crash.
                Ability.CastRandomSource = Ability.DefaultCastRandom;
            }
        }

        [TestMethod]
        public void EveryProjectThatBootsInsideGodotInstallsTheEngineCastStream()
        {
            // The other half of the chain: the hook above is worth exactly as much as the bootstraps
            // that call it. A project dropping the call leaves its casts on the engine-free default,
            // and nothing in the running game would ever say so out loud.
            foreach (string relative in s_bootstraps)
            {
                string bootstrap = Path.Combine(SrcRoot, relative);
                Assert.IsTrue(File.Exists(bootstrap), $"the bootstrap is not where it lived: {bootstrap}");
                Assert.IsTrue(
                    File.ReadAllText(bootstrap).Contains(nameof(BattleSystemModuleDependencies.UseEngineCastRandom), StringComparison.Ordinal),
                    $"{relative} stopped pointing its cast stream at the engine generator — the game would roll casts on the sandbox default");
            }
        }

        [TestMethod]
        public async Task ACastRollsOnTheGeneratorTheSourceHandsOut()
        {
            // What makes the seat worth guarding at all: the source is not decoration, it is where the
            // stream comes from. The cast is driven for real here — which is what the abstraction in
            // the activation context bought.
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

        /// <summary>A seat-related method named on the ability class, or the failure that says it is gone.</summary>
        private static MethodInfo StaticMethod(string name)
        {
            MethodInfo? method = typeof(Ability).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, $"{nameof(Ability)} no longer names {name}, so nothing can say what a cast rolls on");
            return method;
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
