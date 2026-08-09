namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using static AugmentBench;

    /// <summary>
    /// The shared vocabulary, walked from both ends. A key of <see cref="AbilityParameter"/> is a
    /// CONCEPT, and an ability carries it because it said so — so the same record has to work on every
    /// ability that made the declaration and to do nothing at all on every ability that did not.
    ///
    /// Both halves fail silently without a walk. A record standing on one ability's private key is
    /// offered to a dozen abilities by its tags and quietly moves nothing on eleven of them; a record
    /// standing on a word two abilities happened to spell the same way moves a number nobody offered it
    /// for. Neither shows up as an error anywhere — the augment is bought, worn, saved and displayed
    /// either way, which is exactly why the claims are written down here rather than left to reading.
    ///
    /// And the third: two records reaching for one concept are ONE offer at two strengths. That is the
    /// price of the vocabulary and it is deliberate — a build wearing two amplifiers of the same number
    /// gets the better one, not their sum.
    /// </summary>
    [TestClass]
    public class AbilityCommonParameterTests
    {
        /// <summary>Two abilities that both declare <see cref="AbilityParameter.Duration"/> — a blessing
        /// on the caster and a coating on his weapons. Nothing about the two is alike except the
        /// concept, which is the point.</summary>
        private const string BlessingId = "Ability_Ares_Blessing";
        private const string CoatingId = "Ability_Poison_Coating";

        /// <summary>Two abilities that both declare <see cref="AbilityParameter.Attacks"/>.</summary>
        private const string LungeId = "Ability_Head_Butt";
        private const string PressureId = "Ability_Increasing_Pressure";

        /// <summary>A series whose count is a RANGE, so it declares no common count at all: the ability
        /// an augment of attacks has to leave alone.</summary>
        private const string SeriesId = "Ability_Series_Of_Attacks";

        /// <summary>A cast that lays nothing on its caster, so it declares no buff duration: the ability
        /// an augment of buff duration has to leave alone — including the poison duration it does carry,
        /// which is a different concept under a different key.</summary>
        private const string JarId = "Ability_Jar_Of_Poison";

        private const string ShroudId = "Ability_Dark_Shroud";

        /// <summary>The second ability that scales its buff, so the record below has both halves to
        /// deliver on it as well as on the shroud.</summary>
        private const string CalculationId = "Ability_Critical_Calculation";

        private const string LongerBuff = "Augment_Buff_Duration";
        private const string LongerBuffAgain = "Augment_Increased_Buff_Duration";
        private const string ExtraLunge = "Augment_Additional_Lunges";

        /// <summary>The record that trades stacks for effectiveness — the one that has to arrive whole
        /// or not at all: half of it is a bill (two stacks fewer) and half is what pays for it.</summary>
        private const string StrongerFewer = "Augment_Add_Effectiveness_Reduce_Stacks";

        [TestMethod]
        public void OneRecordOnASharedKeyLengthensTheBuffOfEveryAbilityThatDeclaresIt()
        {
            // The claim the whole wave is for. The record was written beside Ares' Blessing and names no
            // ability; both casts below buff their own caster for three turns and both say so under the
            // same key, so one record is worth writing instead of one per ability.
            Assert.AreEqual(4f, Wearing(BlessingId, LongerBuff)[AbilityParameter.Duration],
                "the buff record stopped working on the ability it was written beside");
            Assert.AreEqual(4f, Wearing(CoatingId, LongerBuff)[AbilityParameter.Duration],
                "the buff record does nothing on the second ability that declares the concept");
        }

        [TestMethod]
        public void OneRecordOnASharedKeyAddsALungeToEveryAbilityThatCountsItsAttacks()
        {
            Assert.AreEqual(2f, Wearing(LungeId, ExtraLunge)[AbilityParameter.Attacks],
                "the extra-attack record stopped working on the ability it was written beside");
            Assert.AreEqual(6f, Wearing(PressureId, ExtraLunge)[AbilityParameter.Attacks],
                "the extra-attack record does nothing on the second ability that declares the concept");
        }

        [TestMethod]
        public void ARecordOnASharedKeyLeavesAnAbilityThatNeverDeclaredTheConceptExactlyWhereItWas()
        {
            // The other half, and the one that used to go wrong: an augment reaches an ability by its
            // tags, and a key that is a bare word ("Attacks", "Duration") would be found on an ability
            // that meant something else by it. Declared rather than spelled, the concept is absent here
            // and the record is inert — it does not reach into the numbers the ability does carry.
            IAbility series = Wearing(SeriesId, ExtraLunge);

            Assert.AreEqual(2f, series["MinAttacks"], "the attack record reached into the floor of a series it knows nothing about");
            Assert.AreEqual(5f, series["MaxAttacks"], "the attack record reached into the ceiling of a series it knows nothing about");

            IAbility jar = Wearing(JarId, LongerBuff);

            Assert.AreEqual(3f, jar[AbilityParameter.PoisonDuration],
                "the buff record lengthened a poison it was never offered for");
        }

        [TestMethod]
        public void TheRecordThatTradesStacksForEffectivenessDeliversBothHalvesOnEveryAbilityItReaches()
        {
            // A record made of a bill and what pays for it. Its tags seat it on both abilities that
            // count their buff in stacks, and on either of them half of it arriving would be a straight
            // loss — two stacks gone and nothing bought. The concepts are declared in pairs for exactly
            // this reason: an ability that scales its buff names the multiplier as well as the count.
            foreach (string abilityId in new[] { ShroudId, CalculationId })
            {
                IAbility bare = Wearing(abilityId);
                IAbility traded = Wearing(abilityId, StrongerFewer);

                Assert.AreEqual(bare[AbilityParameter.Stacks] - 2f, traded[AbilityParameter.Stacks],
                    $"'{StrongerFewer}' no longer takes its two stacks off '{abilityId}'");
                Assert.AreEqual(bare[AbilityParameter.Effectiveness] + 0.35f, traded[AbilityParameter.Effectiveness],
                    $"'{StrongerFewer}' charges '{abilityId}' two stacks and hands back no effectiveness");
            }
        }

        [TestMethod]
        public void TwoRecordsLengtheningOneBuffAreOneOfferAndOnlyTheBetterIsWorn()
        {
            // Two shipped records, written for two different abilities, both offering a longer buff.
            // On the shared key they are the same offer and the ability wears the better of them — the
            // rivalry rule reading a concept instead of a coincidence of spelling.
            IAbility alone = Wearing(ShroudId, LongerBuff);
            IAbility both = Wearing(ShroudId, LongerBuff, LongerBuffAgain);

            Assert.AreEqual(4f, alone[AbilityParameter.Duration], "one record no longer lengthens the shroud at all");
            Assert.AreEqual(alone[AbilityParameter.Duration], both[AbilityParameter.Duration],
                "the two records that offer one longer buff were added up instead of the better one being worn");
        }

        /// <summary>The ability as the game builds it, wearing the named shipped records in the order
        /// given — the registry builds the upgrades, so what is walked is the road a seated augment
        /// actually travels.</summary>
        private static IAbility Wearing(string abilityId, params string[] augmentIds)
        {
            (AbilityProvider registry, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            IAbility ability = registry.CreateAbility(abilityId);

            ability.InstallUpgrades(InThisOrder([.. augmentIds.Select(id => Built(registry, catalog, id))]));

            return ability;
        }

        private static IAbilityUpgrade Built(AbilityProvider registry, AbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityUpgradeData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

            IAbilityUpgrade? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");

            return upgrade;
        }
    }
}
