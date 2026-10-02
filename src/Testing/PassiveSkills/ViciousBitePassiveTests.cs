namespace LastBreathTest.PassiveSkills
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// "Яростный укус": poison hits harder per unit of poison resistance held ABOVE the cap, and the poison
    /// resistance is cut in return. The bargain is the claim: the cut lands on the total, so it eats the very
    /// reserve the bonus is paid for, and a resistance that merely reaches the cap buys nothing at all.
    /// <para>The reserve is the one <see cref="ResistanceParameters"/> counts and never a second formula —
    /// what the keystone is paid for and what resistance shred bites into are one figure. Both halves of it
    /// move it: the resistance and the maximum capping it, since an essence raising the cap shrinks the
    /// reserve exactly as a lost resistance line would.</para>
    /// </summary>
    [TestClass]
    public class ViciousBitePassiveTests
    {
        private const float PerOvercap = 0.01f;
        private const float ResistancePenalty = -0.6f;

        /// <summary>The cap every fighter is born with.</summary>
        private const float DefaultMaximum = ResistanceParameters.DefaultMaximum;

        private const float Precision = 0.0001f;

        // ---- the reserve ------------------------------------------------------------------------------

        /// <summary>The keystone eating its own carrier, in numbers: a resistance that looks far over the cap
        /// is not over it once the price is paid, and the bonus is nothing.</summary>
        [TestMethod]
        public void AResistanceThePriceDropsUnderTheCapBuysNothing()
        {
            var carrier = Carrier(poisonResistance: 1.2f);

            carrier.Wear(ViciousBite());

            Assert.AreEqual(0.48f, carrier.Value(EntityParameter.PoisonResistance), Precision, "the price is a multiplicative −60% of the total");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "a total under the cap holds no reserve, so the keystone was paid for one that is not there");
        }

        /// <summary>The overfed reserve: 200% cut to 80% stands 5 points over the 75% cap, and each point is
        /// worth 1% of poison damage multiplier.</summary>
        [TestMethod]
        public void OnlyWhatSurvivesThePriceAboveTheCapIsPaidFor()
        {
            var carrier = Carrier(poisonResistance: 2f);

            carrier.Wear(ViciousBite());

            Assert.AreEqual(0.8f, carrier.Value(EntityParameter.PoisonResistance), Precision);
            Assert.AreEqual(0.05f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "five points of reserve must be worth five points of multiplier");
        }

        /// <summary>The reserve is read where the cap is read: the keystone and
        /// <see cref="ResistanceParameters"/> may not hold two answers to one question.</summary>
        [TestMethod]
        public void TheReserveIsTheOneTheResistanceRulesCount()
        {
            var carrier = Carrier(poisonResistance: 2f);
            carrier.Wear(ViciousBite());

            float reserve = ResistanceParameters.Overcap(carrier.Parameters, EntityParameter.PoisonResistance);

            Assert.AreEqual(reserve, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision);
            Assert.AreEqual(
                carrier.Value(EntityParameter.PoisonResistance) - ResistanceParameters.Effective(carrier.Parameters, EntityParameter.PoisonResistance),
                reserve, Precision, "the reserve and what mitigates must add up to the total");
        }

        [TestMethod]
        public void AReserveIsNeverNegative()
        {
            Assert.AreEqual(0f, ResistanceParameters.Overcap(0.1f, DefaultMaximum), Precision,
                "a resistance under its cap must hold no reserve rather than a negative one");
        }

        // ---- the living measure -----------------------------------------------------------------------

        [TestMethod]
        public void MoreResistanceMovesTheBonusTheSameTurn()
        {
            var carrier = Carrier(poisonResistance: 2f);
            carrier.Wear(ViciousBite());

            carrier.Set(EntityParameter.PoisonResistance, 3f);

            Assert.AreEqual(0.45f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "300% cut to 120% stands 45 points over the cap");
        }

        /// <summary>A cap that moves moves the reserve: the maximum is the second parameter the line watches,
        /// and the essences to come are what will move it.</summary>
        [TestMethod]
        public void ARaisedMaximumShrinksTheBonusTheSameTurn()
        {
            var carrier = Carrier(poisonResistance: 2f);
            carrier.Wear(ViciousBite());

            carrier.Set(EntityParameter.PoisonResistanceMaximum, 0.8f);

            Assert.AreEqual(0f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision,
                "the cap caught up with the total and the reserve is gone");
        }

        [TestMethod]
        public void TheKeystoneLeavesNothingBehind()
        {
            var carrier = Carrier(poisonResistance: 2f);
            ISkill bite = ViciousBite();

            carrier.Wear(bite);
            carrier.TakeOff(bite);

            Assert.AreEqual(2f, carrier.Value(EntityParameter.PoisonResistance), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.PoisonDamageMultiplier), Precision);
        }

        /// <summary>Taking the price away from the reading would make the keystone free: the same 120% that
        /// buys nothing while the price is paid would buy 45 points of multiplier without it.</summary>
        [TestMethod]
        public void WithoutThePriceTheSameResistanceWouldBuyTheReserveTheKeystoneRefuses()
        {
            var carrier = Carrier(poisonResistance: 1.2f);
            carrier.Wear(ViciousBite());

            float paid = carrier.Value(EntityParameter.PoisonDamageMultiplier);

            Assert.AreEqual(0f, paid, Precision);
            Assert.AreEqual(0.45f, ResistanceParameters.Overcap(1.2f, DefaultMaximum), Precision,
                "the reserve before the price is what the keystone must NOT be paid for");
        }

        // ---- the registration -------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneFromItsFields()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(ViciousBitePassiveSkill.PassiveId,
                new RecordProperties(ViciousBitePassiveSkill.PassiveId, new Dictionary<string, float>
                {
                    ["perOvercap"] = PerOvercap,
                    ["resistancePenalty"] = ResistancePenalty
                }));

            Assert.IsInstanceOfType<ViciousBitePassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        private static ISkill ViciousBite() => new ViciousBitePassiveSkill(PerOvercap, ResistancePenalty);

        private static KeystoneCarrier Carrier(float poisonResistance)
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.PoisonResistance, poisonResistance);

            return carrier;
        }
    }
}
