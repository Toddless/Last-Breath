namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// How firmly an effect holds against a dispel, and where that answer comes from. The strength is a
    /// canonical fact rather than a class one: <c>SharedData/Effects</c> names the exceptions and every
    /// effect not named is weak, so raising an effect to strong is a line of data and no code at all.
    /// The road is the one the stack ceiling travels — read at construction out of the composition — so a
    /// copy, a bonus stack and an effect built straight by an ability all answer the same number, and an
    /// effect built where no canon was ever loaded is simply weak instead of silently absolute.
    /// </summary>
    [TestClass]
    public class EffectPowerTests
    {
        /// <summary>The five seals, which the design defines as absolute: nothing dispels a seal.</summary>
        private static readonly string[] s_seals =
        [
            "Effect_Seal_Of_Slowness",
            "Effect_Seal_Of_Silence",
            "Effect_Seal_Of_Oblivion",
            "Effect_Seal_Of_Blood",
            "Effect_Seal_Of_Spirit"
        ];

        /// <summary>The shipped canon behind a composition, exactly as the game reads it — the strength is
        /// pulled from the composition at the moment an effect is built, so a walk about it has to BE one.</summary>
        [TestInitialize]
        public void ComposeTheCanon()
        {
            var canon = new EffectProvider();
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json"))
                canon.Apply(DataCatalog.Effects, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));

            GameServiceProvider.Initialize(services => services.AddSingleton<IEffectProvider>(canon));

            Assert.AreEqual(EffectPower.Absolute, GameServiceProvider.TryGet<IEffectProvider>()?.PowerOf(s_seals[0]),
                "the composition does not answer for the shipped canon, so nothing below is measuring the canonical strength");
        }

        [TestMethod]
        public void EverySealIsAbsoluteOnTheInstanceAndNotOnlyInTheFile()
        {
            IEffect[] seals =
            [
                new SlownessSeal(duration: 3, maxStacks: 1, amount: 1f),
                new SilenceSeal(duration: 3, maxStacks: 1),
                new OblivionSeal(duration: 3, maxStacks: 1),
                new BloodSeal(duration: 3, maxStacks: 1),
                new SpiritSeal(duration: 3, maxStacks: 1)
            ];

            string[] dispellable = [.. seals.Where(seal => seal.Power != EffectPower.Absolute).Select(seal => seal.Id)];

            Assert.AreEqual(0, dispellable.Length, $"seals a dispel could take: [{string.Join(", ", dispellable)}]");
        }

        [TestMethod]
        public void ARankAndFileEffectIsWeakWithoutSayingSoAnywhere()
        {
            // The default carries the whole catalog: the canon names strengths for six rows out of 51.
            var clumsiness = new Clumsiness(duration: 3, maxStacks: 5, value: 0.15f);

            Assert.AreEqual(EffectPower.Weak, clumsiness.Power, "an effect no row calls strong came out stronger than weak");
        }

        [TestMethod]
        public async Task AWeakDispelDoesNotReachTheMythicCalculationAndAStrongOneDoes()
        {
            // The one strong row, and the only one that is neither a seal nor weak. Measured with no
            // Crit Calculation buff on the bearer at all: the mythic leaves with its host, so a walk
            // that laid one would be reading the tie back to the host rather than the strength.
            var bearer = new ConditionOwner();
            var calculation = new MythicCalculationEffect(duration: 3, maxStacks: 1);
            await calculation.Apply(new EffectApplyingContext { Caster = bearer, Target = bearer, Source = "Test_Effect_Power", Damage = default });

            Assert.AreEqual(EffectPower.Strong, calculation.Power, "the canon no longer calls the mythic reading strong");

            bearer.Effects.Dispel(EffectPower.Weak, DispelScope.Target);
            Assert.IsTrue(bearer.Effects.Effects.Contains(calculation), "a weak dispel took an effect the canon calls strong");

            bearer.Effects.Dispel(EffectPower.Strong, DispelScope.Target);
            Assert.IsFalse(bearer.Effects.Effects.Contains(calculation), "a strong dispel left an effect of its own strength standing");
        }

        [TestMethod]
        public void AnEffectBuiltPastTheCanonIsWeakRatherThanUndispellable()
        {
            Assert.AreEqual(EffectPower.Weak, new Uncanonical().Power,
                "an effect the canon carries no row for was born stronger than the default");
        }

        [TestMethod]
        public void ASealDoesNotStack()
        {
            // The stack ceiling and the strength are the two halves of what makes a seal a seal: it cannot
            // be piled up and it cannot be taken off. The ceiling is read off the canon at construction,
            // so an ability asking for more still lays one.
            var overreaching = new SlownessSeal(duration: 3, maxStacks: 3, amount: 1f);

            Assert.AreEqual(1, overreaching.MaxStacks, "a seal was laid in stacks");
        }

        [TestMethod]
        public void TheCanonCallsEverySealAbsolute()
        {
            string[] dispellable = [.. s_seals.Where(seal => GameServiceProvider.TryGet<IEffectProvider>()?.PowerOf(seal) != EffectPower.Absolute)];

            Assert.AreEqual(0, dispellable.Length, $"seals the canon leaves dispellable: [{string.Join(", ", dispellable)}]");
        }

        /// <summary>An effect wearing an id the canon carries no row for.</summary>
        private sealed class Uncanonical() : Effect("Effect_Power_Probe", duration: 3, maxStacks: 1)
        {
            public override IEffect Copy() => new Uncanonical();
        }
    }
}
