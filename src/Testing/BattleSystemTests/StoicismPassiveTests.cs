namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>
    /// "Стоицизм": hard control never lands on the bearer, and his own evasion never saves him from an
    /// attack. Two halves of one bargain, and what is pinned here is that each of them reaches exactly as
    /// far as it was written to.
    /// <para>The price is the load-bearing half: it is taken at the roll and not on the parameter, so
    /// evasion stays a number everything else keeps being paid for — a keystone counted per point of it
    /// (Death Dance) is worth what it always was beside a stoic. Block and the resistances are not the
    /// bargain and are left alone.</para>
    /// <para>The mask is named outright rather than asked of a composition: a walk composes no services, and
    /// the shipped rules are free to name another status without any claim here changing its mind.</para>
    /// </summary>
    [TestClass]
    public class StoicismPassiveTests
    {
        private const float EvadeBase = 6000f;
        private const float CriticalDamagePerEvade = 0.0001f;
        private const float Precision = 0.0001f;

        /// <summary>The statuses this walk calls hard control, standing in for the shipped rules file.</summary>
        private const StatusEffects HardControl = StatusEffects.Stun | StatusEffects.Freeze | StatusEffects.Paralysis;

        // ---- the immunity ----------------------------------------------------------------------------

        [TestMethod]
        public void HardControlIsRefusedOnArrival()
        {
            var carrier = new KeystoneCarrier();
            carrier.Wear(Stoicism());

            foreach (StatusEffects status in new[] { StatusEffects.Stun, StatusEffects.Freeze, StatusEffects.Paralysis })
                Assert.IsTrue(carrier.Incoming(status).Rejected, $"{status} landed on a fighter nothing controls");
        }

        [TestMethod]
        public void AStatusOutsideTheMaskLandsAsUsual()
        {
            var carrier = new KeystoneCarrier();
            carrier.Wear(Stoicism());

            Assert.IsFalse(carrier.Incoming(StatusEffects.Bleed).Rejected,
                "the immunity is to control and not to everything that can be laid on a fighter");
        }

        [TestMethod]
        public void TheImmunityLeavesWithTheKeystone()
        {
            var carrier = new KeystoneCarrier();
            ISkill stoicism = Stoicism();

            carrier.Wear(stoicism);
            carrier.TakeOff(stoicism);

            Assert.IsFalse(carrier.Incoming(StatusEffects.Stun).Rejected,
                "a refunded keystone left its immunity behind on the fighter");
        }

        // ---- the price ------------------------------------------------------------------------------

        [TestMethod]
        public void WearingItDeniesEvasionAndRefundingItGivesEvasionBack()
        {
            var carrier = new KeystoneCarrier();
            ISkill stoicism = Stoicism();

            carrier.Wear(stoicism);
            Assert.IsTrue(carrier.Parameters.IsChanceDenied(EntityParameter.Evade));

            carrier.TakeOff(stoicism);
            Assert.IsFalse(carrier.Parameters.IsChanceDenied(EntityParameter.Evade),
                "the price outlived the keystone that charged it");
        }

        /// <summary>The stack of evasion is untouched: the price is the verdict of the roll and never the
        /// figure, so everything that reads the figure keeps reading the same one.</summary>
        [TestMethod]
        public void EvasionIsStillTheNumberItWas()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.Evade, EvadeBase);

            carrier.Wear(Stoicism());

            Assert.AreEqual(EvadeBase, carrier.Value(EntityParameter.Evade), Precision);
        }

        /// <summary>The combination with the other Dexterity keystone: Death Dance is paid per point of
        /// evasion, and a stoic who cannot win an evasion roll still carries every point of it.</summary>
        [TestMethod]
        public void ALinePaidPerPointOfEvasionIsWorthTheSameBesideAStoic()
        {
            var carrier = new KeystoneCarrier();
            carrier.Set(EntityParameter.Evade, EvadeBase);
            new ScaledByParameterModifier(ModifierValueType.Flat, EntityParameter.CriticalDamage, EntityParameter.Evade,
                CriticalDamagePerEvade, condition: null, source: "fixture").ApplyTo(carrier.Fighter);

            float before = carrier.Value(EntityParameter.CriticalDamage);
            carrier.Wear(Stoicism());

            Assert.AreEqual(EvadeBase * CriticalDamagePerEvade, before, Precision, "the fixture line is measured per point of evasion");
            Assert.AreEqual(before, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "the denial reached the number instead of the verdict, and the dance was paid for nothing");
        }

        /// <summary>Only evasion is bought off. A stoic still raises a shield and still resists.</summary>
        [TestMethod]
        public void NothingButEvasionIsDenied()
        {
            var carrier = new KeystoneCarrier();
            carrier.Wear(Stoicism());

            foreach (EntityParameter parameter in new[]
                     {
                         EntityParameter.BlockChance, EntityParameter.SuppressChance, EntityParameter.CriticalChance,
                         EntityParameter.FireResistance, EntityParameter.PoisonResistance
                     })
                Assert.IsFalse(carrier.Parameters.IsChanceDenied(parameter), $"the keystone reached {parameter}");
        }

        // ---- the registration -------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneWithoutASingleField()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(StoicismPassiveSkill.PassiveId, RecordProperties.Empty);

            Assert.IsInstanceOfType<StoicismPassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        /// <summary>What hard control IS belongs to the rules file: the passive asks for the mask and holds
        /// no list of its own, so a composition that carries no rules covers the bearer from nothing.</summary>
        [TestMethod]
        public void WithNoRulesComposedTheImmunityCoversNothing()
        {
            var carrier = new KeystoneCarrier();
            carrier.Wear(new StoicismPassiveSkill());

            Assert.AreEqual(StatusEffects.None, ControlResistanceRules.Disabled.HardControlMask);
            Assert.IsFalse(carrier.Incoming(StatusEffects.Stun).Rejected,
                "the passive answered from a list of its own instead of the rules file");
        }

        private static ISkill Stoicism() => new StoicismPassiveSkill(HardControlMask);

        private static Func<StatusEffects> HardControlMask => () => HardControl;
    }
}
