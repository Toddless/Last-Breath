namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// "Сила духа": a blow landed on the bearer may hand him back a share of his WHOLE mana pool, and every
    /// other way he recovers mana is worth less.
    /// <para>What a "blow" is, is the load-bearing half. Only what STRUCK him counts — an attack of the
    /// pipeline, or a hit an ability dealt him itself, both of which reach him once and are rolled for once.
    /// A damage-over-turn tick is damage without a blow behind it and is refused before the roll: a poison
    /// that paid every turn it burned would turn the keystone into a mana fountain nobody aimed.</para>
    /// <para>The roll is the fight's own, and it is taken before the pool is read: an empty pool (Агностик
    /// converted it away) leaves the keystone paying nothing without moving anybody else's rolls and without
    /// anything falling over.</para>
    /// </summary>
    [TestClass]
    public class StrengthOfSpiritPassiveTests
    {
        private const float PercentOfMaxMana = 0.05f;
        private const float RecoveryPenalty = -0.25f;
        private const float MaxMana = 200f;
        private const float ManaRecoveryBase = 100f;
        private const float Precision = 0.0001f;

        /// <summary>A keystone that always pays and one that never does: the verdict is the chance itself,
        /// so a walk about WHAT pays never has to arrange a draw.</summary>
        private const float Always = 1f;

        private const float Never = 0f;

        // ---- what a blow returns ----------------------------------------------------------------------

        [TestMethod]
        public void ABlowHandsBackAShareOfTheWholePool()
        {
            ConditionOwner bearer = Bearer();
            Wear(bearer, SpiritThat(Always));

            Strike(bearer, DamageCause.Attack);

            Assert.AreEqual(MaxMana * PercentOfMaxMana, bearer.CurrentMana, Precision,
                "the share is of the MAXIMUM pool and not of what is left in it");
        }

        /// <summary>Both roads a direct hit travels: the attack pipeline, and an ability landing its own hit
        /// (a projectile, a chain jump, a splash all arrive as one).</summary>
        [TestMethod]
        public void EveryDirectBlowPays()
        {
            foreach (DamageCause cause in new[] { DamageCause.Attack, DamageCause.Ability })
            {
                ConditionOwner bearer = Bearer();
                Wear(bearer, SpiritThat(Always));

                Strike(bearer, cause);

                Assert.AreEqual(MaxMana * PercentOfMaxMana, bearer.CurrentMana, Precision, $"a hit caused by {cause} paid nothing");
            }
        }

        /// <summary>The combination with the damage-over-turn effects: a tick pays nothing AND is refused
        /// before the roll, so a burning bearer does not quietly drain the fight's stream either.</summary>
        [TestMethod]
        public void ATickNeitherPaysNorRolls()
        {
            ConditionOwner bearer = Bearer();
            Wear(bearer, SpiritThat(Always));
            var stream = new CountingRandom();

            using (new CombatRandomScope(stream)) Strike(bearer, DamageCause.Effect);

            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "a poison tick fed the keystone");
            Assert.AreEqual(0, stream.Count, "a tick was rolled for instead of being refused outright");
        }

        /// <summary>Everything else that damages a fighter without striking him.</summary>
        [TestMethod]
        public void DamageWithNoBlowBehindItPaysNothing()
        {
            foreach (DamageCause cause in new[] { DamageCause.Passive, DamageCause.Item, DamageCause.Environment })
            {
                ConditionOwner bearer = Bearer();
                Wear(bearer, SpiritThat(Always));

                Strike(bearer, cause);

                Assert.AreEqual(0f, bearer.CurrentMana, Precision, $"damage caused by {cause} paid the keystone");
            }
        }

        [TestMethod]
        public void ABlowLandedOnSomebodyElsePaysNothing()
        {
            ConditionOwner bearer = Bearer();
            Wear(bearer, SpiritThat(Always));

            bearer.CombatEvents.Publish(new DamageTakenEvent(Blow(DamageCause.Attack), Bearer(), default));

            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "the keystone paid for a blow its bearer never took");
        }

        /// <summary>The roll is the fight's own stream and not one the passive keeps to itself — a keystone
        /// rolling privately is a keystone no seeded run reproduces.</summary>
        [TestMethod]
        public void TheRollIsTakenOnTheCombatStream()
        {
            ConditionOwner bearer = Bearer();
            Wear(bearer, SpiritThat(Always));
            var stream = new CountingRandom();

            using (new CombatRandomScope(stream)) Strike(bearer, DamageCause.Attack);

            Assert.AreEqual(1, stream.Count, "one blow must burn exactly one roll of the fight's own stream");
        }

        [TestMethod]
        public void ARefusedRollPaysNothingAndStillBurnsItsDraw()
        {
            ConditionOwner bearer = Bearer();
            Wear(bearer, SpiritThat(Never));
            var stream = new CountingRandom();

            using (new CombatRandomScope(stream)) Strike(bearer, DamageCause.Attack);

            Assert.AreEqual(0f, bearer.CurrentMana, Precision);
            Assert.AreEqual(1, stream.Count, "a blow that pays nothing must still take its draw — the stream may not depend on the verdict");
        }

        // ---- the price --------------------------------------------------------------------------------

        [TestMethod]
        public void ManaRecoveryIsCut()
        {
            ConditionOwner bearer = Bearer();

            Wear(bearer, SpiritThat(Always));

            Assert.AreEqual(ManaRecoveryBase * (1 + RecoveryPenalty), bearer.Parameters.ManaRecovery, Precision,
                "the keystone was worn without its price");
        }

        [TestMethod]
        public void TheKeystoneLeavesNothingBehind()
        {
            ConditionOwner bearer = Bearer();
            ISkill spirit = SpiritThat(Always);
            IPassiveSkillsComponent roster = Wear(bearer, spirit);

            roster.RemoveSkill(spirit);
            Strike(bearer, DamageCause.Attack);

            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "a refunded keystone kept paying for blows");
            Assert.AreEqual(ManaRecoveryBase, bearer.Parameters.ManaRecovery, Precision, "the price outlived the keystone that charged it");
        }

        // ---- the combination with the pool conversion --------------------------------------------------

        /// <summary>Агностик pours the mana pool into health, so there is no maximum left to take a share of.
        /// The blow lands, the keystone is paid nothing and nobody falls over it.</summary>
        [TestMethod]
        public void WithThePoolConvertedAwayTheBlowReturnsNothing()
        {
            ConditionOwner bearer = Bearer();
            IPassiveSkillsComponent roster = Wear(bearer, SpiritThat(Always));
            roster.AddSkill(new AgnosticPassiveSkill(0.25f));
            var stream = new CountingRandom();

            using (new CombatRandomScope(stream)) Strike(bearer, DamageCause.Attack);

            Assert.AreEqual(0f, bearer.Parameters.MaxMana, Precision, "the conversion left a pool behind and the walk proves nothing");
            Assert.AreEqual(0f, bearer.CurrentMana, Precision, "a share of an empty pool is nothing");
            Assert.AreEqual(1, stream.Count,
                "an emptied pool moved the fight's stream — how much mana a bearer has may not decide anybody's rolls");
        }

        // ---- the registration -------------------------------------------------------------------------

        [TestMethod]
        public void TheRegistryBuildsTheKeystoneFromItsFields()
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(StrengthOfSpiritPassiveSkill.PassiveId,
                new RecordProperties(StrengthOfSpiritPassiveSkill.PassiveId, new Dictionary<string, float>
                {
                    ["chance"] = 0.35f,
                    ["percentOfMaxMana"] = PercentOfMaxMana,
                    ["recoveryPenalty"] = RecoveryPenalty
                }));

            Assert.IsInstanceOfType<StrengthOfSpiritPassiveSkill>(skill, "the registry does not answer to the keystone's id");
        }

        private static ISkill SpiritThat(float chance) => new StrengthOfSpiritPassiveSkill(chance, PercentOfMaxMana, RecoveryPenalty);

        private static ConditionOwner Bearer()
        {
            var bearer = new ConditionOwner();
            bearer.SetMaximum(EntityParameter.Mana, MaxMana);
            bearer.SetMaximum(EntityParameter.ManaRecovery, ManaRecoveryBase);
            bearer.CurrentMana = 0f;

            return bearer;
        }

        /// <summary>The roster a fighter's passives actually live on, so wearing and refunding here is the
        /// attach the game makes and not a call written for the walk.</summary>
        private static IPassiveSkillsComponent Wear(ConditionOwner bearer, ISkill skill)
        {
            var roster = new PassiveSkillsComponent(bearer);
            roster.AddSkill(skill);

            return roster;
        }

        private static void Strike(ConditionOwner bearer, DamageCause cause) =>
            bearer.CombatEvents.Publish(new DamageTakenEvent(Blow(cause), bearer, VitalsSnapshot.From(bearer)));

        private static IDamageContext Blow(DamageCause cause)
        {
            var blow = new DamageContext { Source = new ConditionOwner(), Cause = cause };
            blow.Add(DamageType.Physical, 25f);

            return blow;
        }
    }
}
