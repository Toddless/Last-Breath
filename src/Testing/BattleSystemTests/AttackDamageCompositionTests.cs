namespace LastBreathTest.BattleSystemTests
{
    using Core;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;

    /// <summary>Attack damage as a component dictionary (Physical seed + weapon elementals, crit per
    /// component) and the mitigation rules per damage type: elementals — resistance × source penetration;
    /// Physical and Bleed — armor; Burning — fire resistance regardless of the ignore-resists flag;
    /// Pure and Poison pass untouched.</summary>
    [TestClass]
    public class AttackDamageCompositionTests
    {
        private const float ArmorScalingFactor = 10000f; // mirror of Calculations' curve constant

        /// <summary>Mitigation by type rolls nothing. Suppression shares the call and demands a stream,
        /// but none of the hits here has an ability origin, so it never reaches this one.</summary>
        private static readonly IRandomNumberGenerator NoRolls = Mock.Of<IRandomNumberGenerator>();

        [TestMethod]
        public void AttackContext_SeedsPhysicalAndWeaponElementals()
        {
            var attacker = Fighter((EntityParameter.FireDamage, 40f), (EntityParameter.ColdDamage, 25f));

            var context = new AttackContext(attacker.Object, Fighter().Object, 100f, null!, Mock.Of<Core.Battle.IAttackContextScheduler>());

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Physical]);
            Assert.AreEqual(40f, context.DamageComponents[DamageType.Fire]);
            Assert.AreEqual(25f, context.DamageComponents[DamageType.Cold]);
            Assert.IsFalse(context.DamageComponents.ContainsKey(DamageType.Lightning), "zero elementals must not seed empty components");
            Assert.AreEqual(165f, context.TotalDamage);
        }

        [TestMethod]
        public void Crit_ScalesEveryComponent()
        {
            var attacker = Fighter((EntityParameter.FireDamage, 40f));
            var context = new AttackContext(attacker.Object, Fighter().Object, 100f, null!, Mock.Of<Core.Battle.IAttackContextScheduler>())
            {
                IsCritical = true,
                RawCriticalDamage = 2f
            };

            Calculations.CalculateInitialAttackDamage(context);

            Assert.AreEqual(200f, context.DamageComponents[DamageType.Physical]);
            Assert.AreEqual(80f, context.DamageComponents[DamageType.Fire]);
        }

        [TestMethod]
        public void ComposeAttackDamage_CarriesEveryComponent()
        {
            var attacker = Fighter((EntityParameter.LightningDamage, 30f));
            var context = new AttackContext(attacker.Object, Fighter().Object, 100f, null!, Mock.Of<Core.Battle.IAttackContextScheduler>())
            {
                IsCritical = true,
                SourceAbilityId = "Ability_Test"
            };
            context.AddDamage(DamageType.Physical, 50f); // ability bonus damage joins the physical bucket

            var damage = Calculations.ComposeAttackDamage(context);

            Assert.AreEqual(DamageCause.Attack, damage.Cause);
            Assert.IsTrue(damage.IsCrit);
            Assert.AreEqual("Ability_Test", damage.SourceAbilityId);
            Assert.AreEqual(150f, damage.DamageComponents[DamageType.Physical]);
            Assert.AreEqual(30f, damage.DamageComponents[DamageType.Lightning]);
        }

        [TestMethod]
        public void Mitigation_ElementalUsesResistanceScaledBySourcePenetration()
        {
            var source = Fighter((EntityParameter.FireResistancePenetration, 0.5f));
            var target = Fighter((EntityParameter.FireResistance, 0.8f));
            var context = Damage(source, DamageType.Fire, 100f);

            Calculations.CalculateMitigation(context, target.Object, NoRolls);

            // 0.8 resistance × (1 − 0.5 penetration) = 0.4 → 60 damage through
            Assert.AreEqual(60f, context.DamageComponents[DamageType.Fire], 0.001f);
        }

        [TestMethod]
        public void Mitigation_BurningUsesFireResistance_AndIgnoresTheIgnoreResistancesFlag()
        {
            var target = Fighter((EntityParameter.FireResistance, 0.5f));
            var context = Damage(Fighter(), DamageType.Burning, 100f);
            context.Add(DamageType.Fire, 100f);
            context.IgnoreResistances = true;

            Calculations.CalculateMitigation(context, target.Object, NoRolls);

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Fire], 0.001f, "the flag skips elemental resistances");
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Burning], 0.001f, "burning ticks are resisted regardless — the flag is an attack-side mark");
        }

        [TestMethod]
        public void Mitigation_BleedUsesArmorWithSourcePenetration()
        {
            var target = Fighter((EntityParameter.Armor, ArmorScalingFactor)); // curve → 50% reduction
            var context = Damage(Fighter(), DamageType.Bleed, 100f);

            Calculations.CalculateMitigation(context, target.Object, NoRolls);
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Bleed], 0.001f);

            var piercingSource = Fighter((EntityParameter.ArmorPenetration, 1f));
            var pierced = Damage(piercingSource, DamageType.Bleed, 100f);
            Calculations.CalculateMitigation(pierced, target.Object, NoRolls);
            Assert.AreEqual(100f, pierced.DamageComponents[DamageType.Bleed], 0.001f, "full armor penetration applies to bleed like to a physical hit");
        }

        [TestMethod]
        public void Mitigation_PoisonAndPurePassUntouched()
        {
            var target = Fighter(
                (EntityParameter.Armor, ArmorScalingFactor),
                (EntityParameter.FireResistance, 0.8f));
            var context = Damage(Fighter(), DamageType.Poison, 100f);
            context.Add(DamageType.Pure, 100f);

            Calculations.CalculateMitigation(context, target.Object, NoRolls);

            Assert.AreEqual(100f, context.DamageComponents[DamageType.Poison]);
            Assert.AreEqual(100f, context.DamageComponents[DamageType.Pure]);
        }

        [TestMethod]
        public void DotTakenReduction_MaskReducesOnlyTheMatchingStatus()
        {
            var owner = Fighter();
            owner.SetupGet(f => f.InstanceId).Returns("owner");
            var modifier = new DotDamageTakenReductionContextModifier(owner.Object, () => 0.5f, DamageType.Burning);
            var context = Damage(Fighter(), DamageType.Burning, 100f);
            context.Add(DamageType.Bleed, 100f);

            modifier.Apply(context);

            Assert.AreEqual(50f, context.DamageComponents[DamageType.Burning]);
            Assert.AreEqual(100f, context.DamageComponents[DamageType.Bleed], "a burning-only knob must not touch bleed");
        }

        private static DamageContext Damage(Mock<IFightable> source, DamageType type, float amount)
        {
            source.SetupGet(f => f.InstanceId).Returns("source");
            var context = new DamageContext { Source = source.Object, Cause = DamageCause.Effect };
            context.Add(type, amount);
            return context;
        }

        private static Mock<IFightable> Fighter(params (EntityParameter Parameter, float Value)[] values)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            foreach ((EntityParameter parameter, float value) in values)
                parameters.Setup(p => p.GetValueForParameter(parameter)).Returns(value);

            // ApplyArmor reads the named properties, not GetValueForParameter — mirror the values there.
            parameters.SetupGet(p => p.Armor).Returns(values.FirstOrDefault(v => v.Parameter == EntityParameter.Armor).Value);
            parameters.SetupGet(p => p.ArmorPenetration).Returns(values.FirstOrDefault(v => v.Parameter == EntityParameter.ArmorPenetration).Value);

            var fighter = new Mock<IFightable>();
            fighter.SetupGet(f => f.Parameters).Returns(parameters.Object);
            fighter.SetupGet(f => f.IsAlive).Returns(true);
            return fighter;
        }
    }
}
