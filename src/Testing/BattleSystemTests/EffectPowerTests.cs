namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Services;

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

        /// <summary>The one strong row the design list does not carry: the mythic reading is off the list
        /// entirely (see EffectCanonTests.s_offTheList), so it is named apart from the transcription.</summary>
        private const string OffTheListStrong = "Effect_Mythic_Calculation";

        /// <summary>Every row the design list marks "Сила: Сильный", transcribed from the list rather than
        /// read back out of the canon.</summary>
        private static readonly string[] s_strong =
        [
            "Effect_Fury",
            "Effect_Burning_Fury",
            "Effect_Primal_Fury",
            "Effect_Healing_Fury",
            "Effect_Evade_First_Death",
            "Effect_Lucky_Crit_Chance",
            "Effect_Fragility",
            "Effect_Curse",
            "Effect_Heal_Reduction",
            "Effect_Frostbite"
        ];

        /// <summary>The shipped canon behind a composition, exactly as the game reads it — the strength is
        /// pulled from the composition at the moment an effect is built, so a walk about it has to BE one.</summary>
        [TestInitialize]
        public void ComposeTheCanon()
        {
            EffectProviders.ComposeShipped();

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
            // The default carries the whole catalog: the canon names strengths for sixteen rows out of 52.
            var clumsiness = new Clumsiness(duration: 3, maxStacks: 5, value: 0.15f);

            Assert.AreEqual(EffectPower.Weak, clumsiness.Power, "an effect no row calls strong came out stronger than weak");
        }

        [TestMethod]
        public async Task AWeakDispelDoesNotReachTheMythicCalculationAndAStrongOneDoes()
        {
            // A strong row, neither a seal nor weak. Measured with no Crit Calculation buff on the bearer
            // at all: the mythic leaves with its host, so a walk that laid one would be reading the tie
            // back to the host rather than the strength.
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

        [TestMethod]
        public void TheCanonRaisesTheseRowsAboveWeakAndNoOthers()
        {
            // Read as a SET and compared both ways, because a walk over the named ids only catches a
            // strength going missing. A "power" typed onto a row the list calls weak, or an Absolute
            // landing anywhere but a seal, is the same drift in the other direction and reaches nobody:
            // the effect simply stops answering to the dispel it was balanced against.
            IEffectProvider canon = GameServiceProvider.TryGet<IEffectProvider>()!;

            string[] declared =
            [
                .. s_seals.Select(id => $"{id}={EffectPower.Absolute}")
                    .Concat(s_strong.Append(OffTheListStrong).Select(id => $"{id}={EffectPower.Strong}"))
                    .Order(StringComparer.Ordinal)
            ];
            string[] canonical =
            [
                .. canon.KnownIds.Where(id => canon.PowerOf(id) != EffectPower.Weak)
                    .Select(id => $"{id}={canon.PowerOf(id)}")
                    .Order(StringComparer.Ordinal)
            ];

            string[] lost = [.. declared.Except(canonical, StringComparer.Ordinal)];
            string[] unaccounted = [.. canonical.Except(declared, StringComparer.Ordinal)];

            Assert.AreEqual(0, lost.Length, $"strengths named here that the canon does not carry: [{string.Join(", ", lost)}]");
            Assert.AreEqual(0, unaccounted.Length, $"strengths the canon carries that nothing here names: [{string.Join(", ", unaccounted)}]");
        }

        /// <summary>An effect wearing an id the canon carries no row for.</summary>
        private sealed class Uncanonical() : Effect("Effect_Power_Probe", duration: 3, maxStacks: 1)
        {
            public override IEffect Copy() => new Uncanonical();
        }
    }
}
