namespace LastBreathTest.PassiveSkills
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Enums;
    using Core.Modifiers.Context;

    /// <summary>
    /// "Триединство": the bearer's attacks carry no physical part of their own — all of it lands as cold,
    /// fire and lightning in equal thirds. A conversion and not an addition, so a blow is worth exactly what
    /// it was and only what it is made of changes.
    /// <para>It takes what is LEFT of the physical part rather than a share of it, and that is what puts it
    /// behind every conversion owed a share and ahead of every rule that ENDS a component. The order is a
    /// slot on the scale and never the order things were hung in: the same two rules must split a blow the
    /// same way whichever of them was attached first.</para>
    /// </summary>
    [TestClass]
    public class TrinityPassiveTests
    {
        private const float Blow = 90f;

        /// <summary>What a mythic mark carries into its own element before the keystone sees the blow.</summary>
        private const float MythicShare = 0.4f;

        private const float Precision = 0.0001f;

        private static readonly DamageType[] s_elements = [DamageType.Fire, DamageType.Cold, DamageType.Lightning];

        // ---- the conversion ----------------------------------------------------------------------------

        [TestMethod]
        public void ThePhysicalPartIsSplitEvenlyBetweenTheThreeElements()
        {
            var carrier = Carrier();

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            foreach (DamageType element in s_elements)
                Assert.AreEqual(Blow / 3, Part(blow, element), Precision,
                    $"{element} was not paid an even third of the physical part");
        }

        /// <summary>Variant B and not an addition: the physical part is SPENT, so the blow is worth what it
        /// always was. The component is emptied rather than removed — everything downstream still reads what
        /// the hit was made of.</summary>
        [TestMethod]
        public void ThePhysicalPartIsSpentAndTheBlowIsWorthWhatItWas()
        {
            var carrier = Carrier();

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            Assert.IsTrue(blow.DamageComponents.ContainsKey(DamageType.Physical),
                "the physical part was taken out of the hit instead of being emptied");
            Assert.AreEqual(0f, Part(blow, DamageType.Physical), Precision, "the keystone added the elements and kept the physical part");
            Assert.AreEqual(Blow, blow.TotalDamage, Precision, "a conversion may not change what a blow is worth");
        }

        /// <summary>The elements the keystone splits into are the ones the mitigation rules name, so a blow
        /// meets a resistance for every part of it.</summary>
        [TestMethod]
        public void EveryPartTheKeystoneMakesIsAnElementMitigationKnows()
        {
            foreach (DamageType element in s_elements)
                Assert.IsTrue(Calculations.ElementalChannels.ContainsKey(element),
                    $"{element} is split into but no resistance answers for it");
        }

        /// <summary>Physical is what the keystone names: bleeding is dealt by armour's rule and not by it.</summary>
        [TestMethod]
        public void NothingButThePhysicalPartIsTouched()
        {
            var carrier = Carrier();

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Bleed, Blow));

            Assert.AreEqual(Blow, Part(blow, DamageType.Bleed), Precision);
        }

        // ---- the order against the other conversions ---------------------------------------------------

        /// <summary>The contract with the mythic marks: a mark converting a share of physical into its own
        /// element is OWED that share and takes it first; the keystone splits what is left. 90 physical with
        /// a 40% mark leaves 36 fire and 54 to be thirded, so fire ends on 54 and the other two on 18.</summary>
        [TestMethod]
        public void AMythicMarkTakesItsShareFirstAndTheKeystoneThirdsTheRemainder()
        {
            var carrier = Carrier();
            carrier.Handler.Add(Mythic(carrier));

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            float remainder = Blow * (1 - MythicShare);
            Assert.AreEqual((Blow * MythicShare) + (remainder / 3), Part(blow, DamageType.Fire), Precision,
                "the mark's own share and its third of the remainder must both land on fire");
            Assert.AreEqual(remainder / 3, Part(blow, DamageType.Cold), Precision);
            Assert.AreEqual(remainder / 3, Part(blow, DamageType.Lightning), Precision);
            Assert.AreEqual(0f, Part(blow, DamageType.Physical), Precision);
            Assert.AreEqual(Blow, blow.TotalDamage, Precision);
        }

        /// <summary>And the split is the scale's, not the order of attachment: hanging the mark BEFORE the
        /// keystone must leave the blow made of exactly the same parts.</summary>
        [TestMethod]
        public void TheSplitDoesNotDependOnWhichRuleWasAttachedFirst()
        {
            var late = Carrier();
            late.Handler.Add(Mythic(late));

            var early = new KeystoneCarrier();
            early.Handler.Add(Mythic(early));
            early.Wear(new TrinityPassiveSkill());

            IDamageContext first = late.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));
            IDamageContext second = early.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            foreach (DamageType element in s_elements)
                Assert.AreEqual(Part(first, element), Part(second, element), Precision,
                    $"{element} was split by the order of attachment instead of by the scale");
        }

        /// <summary>The scale the contract rests on: a rule taking the remainder stands past every rule owed
        /// a share, and short of every rule that ends a component.</summary>
        [TestMethod]
        public void TheRemainderSlotStandsBetweenTheSharesAndTheDenials()
        {
            Assert.IsTrue(ContextModifierPriority.Remainder > ContextModifierPriority.Absolute,
                "a conversion owed a share must have taken it before the remainder is measured");
            Assert.IsTrue(ContextModifierPriority.Denial > ContextModifierPriority.Remainder,
                "a rule that ends a component would empty it before the keystone could carry it away");
        }

        // ---- beside the Gift of Nature -----------------------------------------------------------------

        /// <summary>The pair the wedge is built on: the physical part is carried away as three elements
        /// before the Gift's denial is asked anything, so the denial finds the component already spent and
        /// nothing of the blow is lost. What the Gift pays for the elements is a PARAMETER and not a share of
        /// a hit: it multiplies the elemental damage an attack is SEEDED with (weapon prefixes) and never
        /// reaches what the keystone converts inside the pipeline.</summary>
        [TestMethod]
        public void BesideTheGiftOfNatureThePhysicalPartSurvivesAsElementsAndIsWorthWhatItWas()
        {
            var carrier = Carrier();
            carrier.Set(EntityParameter.FireDamage, 100f);
            carrier.Wear(new GiftOfNaturePassiveSkill(0.35f));

            IDamageContext blow = carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow));

            foreach (DamageType element in s_elements)
                Assert.AreEqual(Blow / 3, Part(blow, element), Precision,
                    $"{element} was denied along with the physical part it came from");
            Assert.AreEqual(Blow, blow.TotalDamage, Precision,
                "the elemental bonus is a parameter and does not multiply a blow already made");
            Assert.AreEqual(135f, carrier.Value(EntityParameter.FireDamage), Precision,
                "the bonus belongs to the elemental damage an attack is seeded with, and that is where it must be");
        }

        // ---- what the bargain does not reach -----------------------------------------------------------

        [TestMethod]
        public void WhatAnAbilityDealsItselfKeepsItsPhysicalPart()
        {
            var carrier = Carrier();

            IDamageContext blow = carrier.Dealt(DamageCause.Ability, (DamageType.Physical, Blow));

            Assert.AreEqual(Blow, Part(blow, DamageType.Physical), Precision);
        }

        [TestMethod]
        public void SomebodyElsesAttackIsUntouched()
        {
            var bearer = Carrier();
            var stranger = new KeystoneCarrier();

            var blow = new DamageContext { Source = stranger.Fighter, Cause = DamageCause.Attack };
            blow.Add(DamageType.Physical, Blow);
            bearer.Handler.Apply(blow);

            Assert.AreEqual(Blow, Part(blow, DamageType.Physical), Precision,
                "the conversion was not gated on whose blow it is");
        }

        [TestMethod]
        public void TheKeystoneLeavesNothingBehind()
        {
            var carrier = new KeystoneCarrier();
            ISkill trinity = new TrinityPassiveSkill();

            carrier.Wear(trinity);
            carrier.TakeOff(trinity);

            Assert.AreEqual(Blow,
                Part(carrier.Dealt(DamageCause.Attack, (DamageType.Physical, Blow)), DamageType.Physical), Precision,
                "a refunded keystone kept converting");
        }

        // ---- the registration --------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneWithoutFields()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(TrinityPassiveSkill.PassiveId, RecordProperties.Empty);

            Assert.IsInstanceOfType<TrinityPassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        /// <summary>What a blow carries of one type — nothing where the type is absent.</summary>
        private static float Part(IDamageContext blow, DamageType type) => blow.DamageComponents.GetValueOrDefault(type);

        private static IDamageModifier Mythic(KeystoneCarrier carrier) =>
            new ElementalConversionContextModifier(carrier.Fighter, DamageType.Fire, () => MythicShare);

        private static KeystoneCarrier Carrier()
        {
            var carrier = new KeystoneCarrier();
            carrier.Wear(new TrinityPassiveSkill());

            return carrier;
        }
    }
}
