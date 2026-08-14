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
        private const string ShroudId = "Ability_Dark_Shroud";

        /// <summary>The second ability that scales its buff, so the record below has both halves to
        /// deliver on it as well as on the shroud.</summary>
        private const string CalculationId = "Ability_Critical_Calculation";

        /// <summary>The record that trades stacks for effectiveness — the one that has to arrive whole
        /// or not at all: half of it is a bill (two stacks fewer) and half is what pays for it.</summary>
        private const string StrongerFewer = "Augment_Add_Effectiveness_Reduce_Stacks";

        /// <summary>The augment that puts Life-Giving Shade on the shroud's caster, and the record that
        /// raises how strongly what the shroud lays lands.</summary>
        private const string Immortality = "Augment_Immortality";
        private const string StrongerRecovery = "Augment_Recovery_Effectiveness";

        /// <summary>The family's other record — the weaker of the two where they meet.</summary>
        private const string StrongerBuff = "Augment_Buff_Effectiveness";

        /// <summary>The family's third record and the ability that carries the tags of two of them: the
        /// aegis lays a barrier on its caster and clumsiness on whoever hits it, so both deals are
        /// offered to it and both stand on the one key.</summary>
        private const string StrongerDebuff = "Augment_Debuff_Effectiveness";
        private const string AegisId = "Ability_Ice_Aegis";

        /// <summary>Share of health the shade restores per evade before effectiveness, and what the
        /// record above is worth — both written out, because a walk that read either off the shipped
        /// files would agree with whatever those files became.</summary>
        private const float ShadeRestore = 0.35f;
        private const float RecoveryBonus = 0.35f;

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
        public void TheThirdRecordOfTheFamilyIsTheSameOfferAsTheOtherTwoWhereItMeetsThem()
        {
            // The family grew to three and its price did not change: one key, one direction, so any two
            // of them that reach the same ability are one deal and the ability wears the better. The
            // aegis is where the buff record and the debuff record meet — both are worth a quarter, so
            // what has to be true is that the pair comes to a quarter and not to a half.
            IAbility bare = Wearing(AegisId);
            IAbility one = Wearing(AegisId, StrongerDebuff);
            IAbility both = Wearing(AegisId, StrongerDebuff, StrongerBuff);

            Assert.AreNotEqual(bare[AbilityParameter.Effectiveness], one[AbilityParameter.Effectiveness],
                "the debuff record does nothing on the aegis, so the pair below proves nothing");
            Assert.AreEqual(one[AbilityParameter.Effectiveness], both[AbilityParameter.Effectiveness],
                "two records of one family were added up on the aegis instead of the better one being worn");
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

        private static IAbilityAugment Built(AbilityProvider registry, AbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");

            IAbilityAugment? upgrade = registry.CreateUpgrade(record);
            Assert.IsNotNull(upgrade, $"the registry builds nothing for '{augmentId}'");

            return upgrade;
        }
    }
}
