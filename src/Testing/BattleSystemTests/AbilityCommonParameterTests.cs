namespace LastBreathTest.BattleSystemTests
{
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Moq;
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

        /// <summary>The berserker, whose fury is a buff on its own caster like any other — and the
        /// ability's own record, which SHORTENS that same buff.</summary>
        private const string FuryId = "Ability_Berserk_Fury";
        private const string ShorterFury = "Augment_Fury_Duration";

        /// <summary>The augment that puts Life-Giving Shade on the shroud's caster, and the record that
        /// raises how strongly what the shroud lays lands.</summary>
        private const string Immortality = "Augment_Immortality";
        private const string StrongerRecovery = "Augment_Recovery_Effectiveness";

        /// <summary>The family's other record — the weaker of the two where they meet.</summary>
        private const string StrongerBuff = "Augment_Buff_Effectiveness";

        /// <summary>Share of health the shade restores per evade before effectiveness, and what the
        /// record above is worth — both written out, because a walk that read either off the shipped
        /// files would agree with whatever those files became.</summary>
        private const float ShadeRestore = 0.35f;
        private const float RecoveryBonus = 0.35f;

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
        public void ARecordThatShortensABuffAndOneThatLengthensItAreBothWorn()
        {
            // The berserker's fury is a buff on his own caster, so the book's "your buff lasts longer"
            // reaches it — and lengthens the health it burns along with the series it grants. His own
            // record pulls the same number the other way. Reaching for one number is not doing the same
            // thing: told apart by the direction they leave it in, the two are two effects and the
            // ability wears both. Read as one, the pair would silently drop whichever rolled lower.
            IAbility bare = Wearing(FuryId);
            IAbility longer = Wearing(FuryId, LongerBuff);
            IAbility shorter = Wearing(FuryId, ShorterFury);
            IAbility both = Wearing(FuryId, LongerBuff, ShorterFury);

            Assert.AreEqual(bare[AbilityParameter.Duration] + 1f, longer[AbilityParameter.Duration],
                "the buff record does not reach the fury, so the pair below proves nothing");
            Assert.AreEqual(bare[AbilityParameter.Duration] - 1f, shorter[AbilityParameter.Duration],
                "the ability's own record stopped shortening the fury");
            Assert.AreEqual(bare[AbilityParameter.Duration], both[AbilityParameter.Duration],
                "one of the two records was read as a weaker copy of the other and dropped");
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

        [TestMethod]
        public void TwoRecordsOfTheEffectivenessFamilyAreOneOfferWhereTheyMeet()
        {
            // The family is segmented by tags and not by keys, so on an ability carrying both tags the
            // two records are one offer at two strengths and the better one works. That is the price of
            // the segmentation and it is deliberate — read as two, the shroud would wear both raises.
            IAbility bare = Wearing(ShroudId);
            IAbility both = Wearing(ShroudId, StrongerBuff, StrongerRecovery);

            Assert.AreEqual(bare[AbilityParameter.Effectiveness] + RecoveryBonus, both[AbilityParameter.Effectiveness],
                0.0001f, "the two records of one family were added up instead of the better one being worn");
        }

        [TestMethod]
        public async Task EffectivenessReachesTheContentACastLaysAndNotOnlyItsOwnNumbers()
        {
            // What effectiveness IS: the multiplier the values of what a cast lays are read through —
            // including the content an augment added, which is where it would be easiest to lose. The
            // shade is put on the caster by one augment and its restore is raised by another, and
            // neither was written for the other: the applier reads the key at the moment of the cast,
            // so whatever is seated on it by then is already in the number.
            float alone = await ShadeRestoredBy(Immortality);
            float raised = await ShadeRestoredBy(Immortality, StrongerRecovery);

            Assert.AreEqual(ShadeRestore, alone, 0.0001f, "the shade no longer restores what its own record declares");
            Assert.AreEqual(ShadeRestore * (1f + RecoveryBonus), raised, 0.0001f,
                "the effectiveness record never reached the shade the shroud lays");
        }

        /// <summary>What one evade under the shade gives back, with the named records seated: the shroud
        /// is cast on a real fighter and the effect is read off the one it landed on.</summary>
        private static async Task<float> ShadeRestoredBy(params string[] augmentIds)
        {
            var owner = new ConditionOwner();
            IAbility shroud = Wearing(ShroudId, augmentIds);
            shroud.SetOwner(owner);

            await shroud.Execute([owner], Mock.Of<IBattleField>());

            IEffect? shade = owner.Effects.Effects.FirstOrDefault(effect => effect.Id == "Effect_Life_Giving_Shade");
            Assert.IsNotNull(shade, "the shroud never laid the shade at all, so the figure below proves nothing");

            return ((LifeGivingShadeEffect)shade).LifeToRecover;
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
