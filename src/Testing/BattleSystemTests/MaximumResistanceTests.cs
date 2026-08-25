namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Core;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers;
    using Moq;

    /// <summary>The resistance cap is a parameter of its own. The total stays uncapped, mitigation reads
    /// min(total, maximum), and resistance shred eats the total — so everything above the maximum is a
    /// reserve that pays for the shred before the mitigating value moves at all.</summary>
    [TestClass]
    public class MaximumResistanceTests
    {
        private const float Hit = 100f;
        private const float Default = 0.75f;

        /// <summary>Mitigation by type rolls nothing, and no hit here has an ability origin, so the
        /// suppression roll that shares the call is never reached.</summary>
        private static readonly IRandomNumberGenerator NoRolls = Mock.Of<IRandomNumberGenerator>();

        [TestMethod]
        public void EveryEntity_IsBornWithTheDefaultMaximum()
        {
            var untouched = new EntityParametersComponent();
            var seeded = Combatant().Parameters;

            foreach (var maximum in ResistanceParameters.Maximums)
            {
                Assert.AreEqual(Default, untouched.GetValueForParameter(maximum), 0.0001f,
                    $"{maximum} of a fresh entity is not the default cap");
                Assert.AreEqual(Default, seeded.GetValueForParameter(maximum), 0.0001f,
                    $"a seeding pass that never names {maximum} stripped the default cap");
            }
        }

        /// <summary>Player and NPC both seed every parameter of the enum at spawn, naming zero for whatever
        /// their data leaves out. That pass must not be what decides whether resistances work.</summary>
        [TestMethod]
        public void ASeedingPassNamingAValue_KeepsIt()
        {
            var parameters = Combatant().Parameters;

            parameters.SetBaseValueForParameter(EntityParameter.FireResistanceMaximum, 0.5f);

            Assert.AreEqual(0.5f, parameters.GetValueForParameter(EntityParameter.FireResistanceMaximum), 0.0001f);
        }

        [TestMethod]
        public void Mitigation_ReadsTheTotalCutDownToTheMaximum()
        {
            var target = Combatant();
            Add(target, EntityParameter.FireResistance, 0.98f);
            Add(target, EntityParameter.PoisonResistance, 0.98f);

            Assert.AreEqual(0.98f, target.Parameters.GetValueForParameter(EntityParameter.FireResistance), 0.0001f,
                "the total is capped — an overcap has to survive in the parameter to be a reserve at all");
            Assert.AreEqual(25f, Mitigated(target, DamageType.Fire), 0.01f, "a 98% total mitigated above its 75% maximum");
            Assert.AreEqual(25f, Mitigated(target, DamageType.Poison), 0.01f, "poison answers to the same cap");
        }

        /// <summary>The owner's example: 98% total under the 75% cap, a −35% shred leaves 63% — and the
        /// overcap paid for the first 23 of those 35 points.</summary>
        [TestMethod]
        public void Shred_EatsTheTotal_SoTheOvercapPaysForItFirst()
        {
            var overcapped = Combatant();
            Add(overcapped, EntityParameter.FireResistance, 0.98f);
            Add(overcapped, EntityParameter.FireResistance, -0.35f);

            var bare = Combatant();
            Add(bare, EntityParameter.FireResistance, Default);
            Add(bare, EntityParameter.FireResistance, -0.35f);

            Assert.AreEqual(0.63f, overcapped.Parameters.GetValueForParameter(EntityParameter.FireResistance), 0.0001f);
            Assert.AreEqual(37f, Mitigated(overcapped, DamageType.Fire), 0.01f, "98 − 35 = 63 effective");
            Assert.AreEqual(60f, Mitigated(bare, DamageType.Fire), 0.01f, "without an overcap the same shred bites in full");
        }

        [TestMethod]
        public void Maximum_IsMovedByAModifier()
        {
            var target = Combatant();
            Add(target, EntityParameter.FireResistance, 0.98f);
            Add(target, EntityParameter.FireResistanceMaximum, 0.1f);

            Assert.AreEqual(15f, Mitigated(target, DamageType.Fire), 0.01f, "the raised cap released 85% of the 98% total");
        }

        /// <summary>A cap of 1 would turn mitigation into healing, so the parameter that raises the cap
        /// has a ceiling of its own.</summary>
        [TestMethod]
        public void Maximum_HasACeilingOfItsOwn()
        {
            var target = Combatant();
            Add(target, EntityParameter.FireResistance, 2f);
            Add(target, EntityParameter.FireResistanceMaximum, 1f);

            Assert.AreEqual(0.9f, target.Parameters.GetValueForParameter(EntityParameter.FireResistanceMaximum), 0.0001f);
            Assert.IsTrue(Mitigated(target, DamageType.Fire) > 0f, "a hit was turned into healing");
        }

        /// <summary>Penetration bites into what actually mitigates, not into the total: an overcap is a
        /// reserve against shred, and buying it must not also buy protection from penetration.</summary>
        [TestMethod]
        public void Penetration_BitesIntoTheEffectiveResistance()
        {
            var target = Combatant();
            Add(target, EntityParameter.FireResistance, 0.98f);
            var source = Combatant();
            Add(source, EntityParameter.FireResistancePenetration, 0.5f);

            Assert.AreEqual(62.5f, Mitigated(target, DamageType.Fire, source), 0.01f,
                "penetration was applied to the total instead of to the capped value");
        }

        [TestMethod]
        public void AllResistanceMaximum_FoldsIntoEveryMember()
        {
            var target = Combatant();
            Add(target, EntityParameter.AllResistanceMaximum, 0.05f);

            foreach (var maximum in ResistanceParameters.Maximums)
                Assert.AreEqual(0.8f, target.Parameters.GetValueForParameter(maximum), 0.0001f,
                    $"{maximum} did not take the bucket");
        }

        /// <summary>What the sheet says: the mitigating value, and the uncapped total beside it only while
        /// there is an overcap to speak of — brackets that always repeat the same number are noise.</summary>
        [TestMethod]
        public void TheSheetShowsTheTotalInBracketsOnlyOverTheCap()
        {
            var formats = new Mock<IParameterFormatProvider>();
            formats.Setup(f => f.GetUnit(It.IsAny<EntityParameter>())).Returns(ParameterUnit.Percent);

            Assert.AreEqual("75% (98%)", Shown(formats.Object, 0.98f));
            Assert.AreEqual("75%", Shown(formats.Object, Default));
            Assert.AreEqual("40%", Shown(formats.Object, 0.4f));
            Assert.AreEqual("0%", Shown(formats.Object, 0f));
        }

        private static string Shown(IParameterFormatProvider formats, float total) =>
            ParameterValueText.FormatResistance(formats, EntityParameter.FireResistance, total, Default);

        private static void Add(Combatants target, EntityParameter parameter, float value) =>
            target.Modifiers.AddModifier(new SimpleModifier(parameter, ModifierValueType.Flat, value, Guid.NewGuid().ToString()));

        private static float Mitigated(Combatants target, DamageType type, Combatants? source = null)
        {
            var context = new DamageContext { Source = (source ?? Combatant()).Fighter, Cause = DamageCause.Effect };
            context.Add(type, Hit);

            Calculations.CalculateMitigation(context, target.Fighter, NoRolls);
            return context.DamageComponents[type];
        }

        /// <summary>A fighter over the real pair of components, seeded the way a spawn seeds one: every
        /// parameter of the enum named, zero for everything the data leaves out.</summary>
        private static Combatants Combatant()
        {
            var modifiers = new ParameterModifiersComponent();
            var parameters = new EntityParametersComponent();
            parameters.Initialize(modifiers.GetModifiers);
            modifiers.ModifiersChanged += parameters.OnParameterModifiersChange;
            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
                parameters.SetBaseValueForParameter(parameter, 0f);

            var fighter = new Mock<IFightable>();
            fighter.SetupGet(f => f.Parameters).Returns(parameters);
            fighter.SetupGet(f => f.IsAlive).Returns(true);
            fighter.SetupGet(f => f.InstanceId).Returns(Guid.NewGuid().ToString());
            return new Combatants(fighter.Object, parameters, modifiers);
        }

        private sealed record Combatants(IFightable Fighter, IEntityParametersComponent Parameters, ParameterModifiersComponent Modifiers);
    }
}
