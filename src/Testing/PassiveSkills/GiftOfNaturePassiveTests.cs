namespace LastBreathTest.PassiveSkills
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Modifiers.Context;

    /// <summary>
    /// "Дар природы": every elemental damage the bearer deals is worth more, and his attacks stop carrying a
    /// physical part at all.
    /// <para>The price is the load-bearing half. It is paid on the BLOW and not on the parameter, so physical
    /// damage stays the number it always was and everything counted per point of it keeps being paid; only
    /// what is still physical when an attack lands is lost.</para>
    /// <para>And it is paid LAST. Every rule entitled to a share of the physical component — a mythic mark's
    /// conversion today, a keystone splitting it three ways tomorrow — takes its share at
    /// <see cref="ContextModifierPriority.Absolute"/> or earlier, so what has already become an element
    /// arrives as that element and survives. Only the untouched remainder is denied.</para>
    /// </summary>
    [TestClass]
    public class GiftOfNaturePassiveTests
    {
        private const float ElementalBonus = 0.35f;

        /// <summary>A base every elemental line here is measured from.</summary>
        private const float ElementBase = 100f;

        private const float PhysicalBlow = 100f;
        private const float FirePart = 40f;
        private const float Precision = 0.0001f;

        // ---- the bonus --------------------------------------------------------------------------------

        [TestMethod]
        public void EveryElementIsWorthMore()
        {
            var carrier = Carrier();

            carrier.Wear(GiftOfNature());

            foreach (EntityParameter element in new[] { EntityParameter.FireDamage, EntityParameter.ColdDamage, EntityParameter.LightningDamage })
                Assert.AreEqual(ElementBase * (1 + ElementalBonus), carrier.Value(element), Precision,
                    $"{element} was left out of the elemental bucket the keystone pays into");
        }

        /// <summary>"Increased BY 35%" is MORE by the project's convention, and the two only look alike while
        /// nothing else raises the element: beside an existing +100% the difference is 270 against 235.</summary>
        [TestMethod]
        public void TheBonusMultipliesInsteadOfJoiningTheIncreases()
        {
            var carrier = Carrier();
            new SimpleModifier(EntityParameter.FireDamage, ModifierValueType.Increase, 1f, "fixture").ApplyTo(carrier.Fighter);

            carrier.Wear(GiftOfNature());

            Assert.AreEqual(ElementBase * 2 * (1 + ElementalBonus), carrier.Value(EntityParameter.FireDamage), Precision,
                "the bonus was summed with the increases instead of multiplying what they produced");
        }

        /// <summary>The bucket is the elements and nothing wider: what a fighter hits with otherwise is his own.</summary>
        [TestMethod]
        public void NothingOutsideTheElementsIsRaised()
        {
            var carrier = Carrier();
            carrier.Set(EntityParameter.PhysicalDamage, ElementBase);
            carrier.Set(EntityParameter.SpellDamage, ElementBase);

            carrier.Wear(GiftOfNature());

            Assert.AreEqual(ElementBase, carrier.Value(EntityParameter.PhysicalDamage), Precision);
            Assert.AreEqual(ElementBase, carrier.Value(EntityParameter.SpellDamage), Precision);
        }

        // ---- the price --------------------------------------------------------------------------------

        [TestMethod]
        public void AnAttackLandsWithNoPhysicalPartAndKeepsTheRest()
        {
            var carrier = Carrier();
            carrier.Wear(GiftOfNature());

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, PhysicalBlow), (DamageType.Fire, FirePart));

            Assert.AreEqual(0f, Part(blow, DamageType.Physical), Precision, "an attack still carried a physical part");
            Assert.AreEqual(FirePart, Part(blow, DamageType.Fire), Precision, "the price reached past the physical part");
        }

        /// <summary>The component is EMPTIED and not removed: everything downstream — mitigation, the log, the
        /// combat text — still sees what the hit was made of, and reads a physical part worth nothing rather
        /// than a hit that never had one.</summary>
        [TestMethod]
        public void TheDeniedComponentStaysInTheHitAsAZero()
        {
            var carrier = Carrier();
            carrier.Wear(GiftOfNature());

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, PhysicalBlow));

            Assert.IsTrue(blow.DamageComponents.ContainsKey(DamageType.Physical),
                "the physical part was taken out of the hit instead of being emptied");
            Assert.AreEqual(0f, blow.DamageComponents[DamageType.Physical], Precision);
        }

        /// <summary>The synergy with the added-elemental marks: the share is measured at Late, while the
        /// physical part is still standing, and what it added is kept. 100 physical with a 30% added cold
        /// leaves 30 cold and nothing else — everything counted per point of physical goes on being paid.</summary>
        [TestMethod]
        public void AnAddedElementalShareIsStillMeasuredFromThePhysicalThatIsAboutToGo()
        {
            var carrier = Carrier();
            carrier.Wear(GiftOfNature());
            carrier.Handler.Add(new AddedElementalDamageContextModifier(carrier.Fighter, DamageType.Cold, () => 0.3f, DamageCause.Attack));

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, PhysicalBlow));

            Assert.AreEqual(PhysicalBlow * 0.3f, Part(blow, DamageType.Cold), Precision,
                "the added share was measured after the denial and the mark was paid for nothing");
            Assert.AreEqual(0f, Part(blow, DamageType.Physical), Precision);
        }

        /// <summary>The parameter is untouched: the price is the blow's and never the figure's, so a line paid
        /// per point of physical damage is worth beside this keystone exactly what it was worth without it.</summary>
        [TestMethod]
        public void PhysicalDamageIsStillTheNumberItWas()
        {
            var carrier = Carrier();
            carrier.Set(EntityParameter.PhysicalDamage, ElementBase);

            carrier.Wear(GiftOfNature());

            Assert.AreEqual(ElementBase, carrier.Value(EntityParameter.PhysicalDamage), Precision);
        }

        /// <summary>Attacks are what the bargain names. An ability that deals physical damage itself keeps it.</summary>
        [TestMethod]
        public void DamageAnAbilityDealsItselfKeepsItsPhysicalPart()
        {
            var carrier = Carrier();
            carrier.Wear(GiftOfNature());

            IDamageContext blow = carrier.Dealt(DamageCause.Ability, (DamageType.Physical, PhysicalBlow));

            Assert.AreEqual(PhysicalBlow, Part(blow, DamageType.Physical), Precision);
        }

        [TestMethod]
        public void SomebodyElsesAttackIsUntouched()
        {
            var bearer = Carrier();
            var stranger = new KeystoneCarrier();
            bearer.Wear(GiftOfNature());

            var blow = new DamageContext { Source = stranger.Fighter, Cause = DamageCause.Attack };
            blow.Add(DamageType.Physical, PhysicalBlow);
            bearer.Handler.Apply(blow);

            Assert.AreEqual(PhysicalBlow, Part(blow, DamageType.Physical), Precision,
                "the denial was not gated on whose blow it is");
        }

        [TestMethod]
        public void TheKeystoneLeavesNothingBehind()
        {
            var carrier = Carrier();
            ISkill gift = GiftOfNature();

            carrier.Wear(gift);
            carrier.TakeOff(gift);

            Assert.AreEqual(ElementBase, carrier.Value(EntityParameter.FireDamage), Precision, "the elemental line outlived the keystone");
            Assert.AreEqual(PhysicalBlow,
                Part(carrier.Dealt(DamageCause.Attack, (DamageType.Physical, PhysicalBlow)), DamageType.Physical), Precision,
                "a refunded keystone kept denying the physical part");
        }

        // ---- the order against conversions -------------------------------------------------------------

        /// <summary>The combination with the mythic marks: half the blow has become fire before the denial is
        /// asked anything, and that half is fire's by then. Only the remainder is lost.</summary>
        [TestMethod]
        public void WhatAConversionTookArrivesAsItsElementAndSurvives()
        {
            var carrier = Carrier();
            carrier.Wear(GiftOfNature());
            // Added AFTER the denial on purpose: the pipeline's order is the priority scale's and never the
            // order things were hung in, and a conversion that ran second would find nothing left to take.
            carrier.Handler.Add(new ElementalConversionContextModifier(carrier.Fighter, DamageType.Fire, () => 0.5f));

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, PhysicalBlow));

            Assert.AreEqual(PhysicalBlow / 2, Part(blow, DamageType.Fire), Precision,
                "the denial ran before the conversion and emptied the component it was to be paid from");
            Assert.AreEqual(0f, Part(blow, DamageType.Physical), Precision,
                "the unconverted remainder survived a keystone that denies it");
        }

        /// <summary>The scale itself, since the combination above rests on it: a rule that ENDS a component
        /// stands past the last word on a number, which is where every conversion stands.</summary>
        [TestMethod]
        public void ADenialStandsPastEveryConversion() =>
            Assert.IsTrue(ContextModifierPriority.Denial > ContextModifierPriority.Absolute,
                "a denial sharing the conversions' slot would be ordered by nothing at all");

        // ---- the registration -------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneFromItsField()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(GiftOfNaturePassiveSkill.PassiveId,
                new RecordProperties(GiftOfNaturePassiveSkill.PassiveId,
                    new Dictionary<string, float> { ["elementalBonus"] = ElementalBonus }));

            Assert.IsInstanceOfType<GiftOfNaturePassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        /// <summary>What a blow carries of one type — nothing where the type is absent, so a component a
        /// mutation never created is reported as the zero it is worth instead of throwing.</summary>
        private static float Part(IDamageContext blow, DamageType type) => blow.DamageComponents.GetValueOrDefault(type);

        private static ISkill GiftOfNature() => new GiftOfNaturePassiveSkill(ElementalBonus);

        private static KeystoneCarrier Carrier()
        {
            var carrier = new KeystoneCarrier();
            foreach (EntityParameter element in new[] { EntityParameter.FireDamage, EntityParameter.ColdDamage, EntityParameter.LightningDamage })
                carrier.Set(element, ElementBase);

            return carrier;
        }
    }
}
