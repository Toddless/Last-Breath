namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.HeadButt;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json.Linq;
    using static AugmentBench;
    using static AugmentCopies;

    /// <summary>
    /// The four augments that took the place of forty-three. All of them work through the base contract
    /// every ability honours, so all of them are written once â€” and the three that move a number state
    /// what they move as a share of it rather than as a number of their own: they go on every ability
    /// there is â€” prices running from nothing at all to five hundred, waits from no turns to nine â€” and
    /// one flat figure would be a near-free cast at the cheap end and nothing worth choosing at the
    /// expensive one.
    ///
    /// Two things a share has to answer for, and both are walked below. A share of a small base is a
    /// fraction of a turn, and turns are whole: a cut that rounds down to zero would leave an augment
    /// chosen, worn, paid for and doing nothing at all â€” the silent refusal this system has already
    /// been bitten by once on effect durations. And a share is a share OF something: measured against
    /// whatever the parameter happens to carry when the augment goes in, the same build would be worth
    /// one price assembled in one order and another price in the other.
    /// </summary>
    [TestClass]
    public class AugmentShareReductionTests
    {
        private const string CostAugment = "Augment_Reduce_Cost";
        private const string CooldownAugment = "Augment_Reduce_Cooldown";

        /// <summary>The record that took the place of three: a shorter wait bought with a higher
        /// price, both stated as shares of the ability's own numbers.</summary>
        private const string SurchargeAugment = "Augment_Reduce_Cooldown_Add_Cost";

        /// <summary>The record that took the place of two: the price paid in health. It moves no
        /// number at all â€” the cost type is categorical â€” so it appears here only where the collapse
        /// is what is being walked.</summary>
        private const string HealthCostAugment = "Augment_Cost_Type_Health";

        private const float CostShare = 0.3f;
        private const float CooldownShare = 0.25f;
        private const float SurchargeShare = 0.4f;

        /// <summary>The augment of another tier the cost share has to share a parameter with: Head
        /// Butt's longer stun, bought with fifty more mana. The shipped record, built the way the
        /// registry builds it â€” two flat moves on two parameters.</summary>
        private const string CostSurcharge = "Augment_Extend_Stun_Add_Cost";

        /// <summary>The same on the other parameter â€” Armageddon reaching every target and waiting three
        /// turns longer for it. Stood in for by an upgrade of the same shape (a flat addition to the
        /// wait), because Armageddon itself has nothing to do with the question.</summary>
        private const string CooldownSurcharge = "Ability_Arm_Augment_All_Targets";

        /// <summary>How many augments the game holds after the collapse â€” the same number in the data
        /// and in the registry, because one half without the other is either an offer nothing builds
        /// or code nothing can reach.</summary>
        private const int ShippedAugmentCount = 133;

        /// <summary>Every record the collapse of the base-contract families left behind, with the tier
        /// it was written at. All four claim the whole book, which is the widest reach in the system
        /// and the thing a lost marker would take away in silence.</summary>
        private static readonly (string Id, int Tier)[] s_collapsedRecords =
        [
            (CostAugment, 1), (CooldownAugment, 1), (SurchargeAugment, 1), (HealthCostAugment, 2)
        ];

        /// <summary>Every cooldown the shipped abilities are written with, and what each share comes to
        /// on it once it is rounded to whole turns. Held as a table rather than recomputed, so a change
        /// of the rounding rule shows up as the numbers it moves.</summary>
        private static readonly (int Base, int QuarterCut, int SurchargeCut)[] s_cooldownTable =
        [
            (3, 1, 1), (4, 1, 2), (5, 1, 2), (6, 2, 2), (7, 2, 3), (9, 2, 4)
        ];

        /// <summary>The three abilities whose own cooldown augments the surcharge record absorbed, and
        /// what the shares come to on the numbers those abilities are written with. The bases are read
        /// off the files, so an ability repriced after the collapse fails here instead of quietly
        /// getting another augment than the one that was agreed.</summary>
        private static readonly (string AbilityId, int CooldownCut, int CostSurchargeValue)[] s_absorbedAbilities =
        [
            ("Ability_Porcupine", 2, 40), ("Ability_Ice_Shards", 2, 80), ("Ability_Poison_Explosion", 3, 40)
        ];

        [TestMethod]
        public void TheCostShareIsTakenOffThePriceOfTheAbilityItGoesOn()
        {
            // The whole reason the value is a share: the same record on the cheapest ability the game
            // ships and on the dearest, taking a proportional bite out of each.
            var cheap = AbilityWith(cost: 100);
            var dear = AbilityWith(cost: 500);

            new AbilityUpgradeReduceCost(CostAugment, [], 1, CostShare).Apply(cheap);
            new AbilityUpgradeReduceCost(CostAugment, [], 1, CostShare).Apply(dear);

            Assert.AreEqual(70, cheap.CostValue, "the share was not measured against the cheap ability's own price");
            Assert.AreEqual(350, dear.CostValue, "the share was not measured against the dear ability's own price");
        }

        [TestMethod]
        public void TheCooldownShareIsTakenOffTheWaitOfTheAbilityItGoesOn()
        {
            var quick = AbilityWith(cooldown: 4);
            var slow = AbilityWith(cooldown: 9);

            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(quick);
            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(slow);

            Assert.AreEqual(3f, quick.Cooldown, "the share was not measured against the quick ability's own wait");
            Assert.AreEqual(7f, slow.Cooldown, "the share was not measured against the slow ability's own wait");
        }

        [TestMethod]
        public void TheSurchargeAugmentCutsTheWaitAndRaisesThePriceOfTheAbilityItGoesOn()
        {
            // The three records the surcharge augment replaced, each on the ability it used to belong
            // to: one record now, and what it does to each of them is the ability's own numbers read
            // twice. Both shares are measured on the same base, so a mistake in either shows up as one
            // of these two figures and not as a build that merely feels off.
            foreach ((string abilityId, int cut, int surcharge) in s_absorbedAbilities)
            {
                (int wait, int price) = ShippedBaseOf(abilityId);
                var ability = AbilityWith(cost: price, cooldown: wait);

                new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare).Apply(ability);

                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"the share no longer takes {cut} turns off the {wait} '{abilityId}' waits");
                Assert.AreEqual(price + surcharge, ability.CostValue, $"the share no longer adds {surcharge} to the {price} '{abilityId}' charges");
            }
        }

        [TestMethod]
        public void TheCostShareIsTheSameWhicheverAugmentTheBuildWasStartedFrom()
        {
            // The share is a share of the ability's price, and an augment of another tier charging fifty
            // more mana for a longer stun does not change what the ability costs to begin with. Read off
            // the number the parameter happens to carry instead, the same two augments would be worth
            // one price picked in one order and another price picked in the other â€” and the build would
            // change again the next time the tier-one slot was emptied and refilled.
            Assert.AreEqual(155, PriceOf(surchargeFirst: true), "the share was measured against a price the surcharge had already raised");
            Assert.AreEqual(155, PriceOf(surchargeFirst: false), "the same two augments came to another price in the other order");
        }

        [TestMethod]
        public void TheCooldownShareIsTheSameWhicheverAugmentTheBuildWasStartedFrom()
        {
            // The same on the other parameter, where the drift is paid in whole turns. Nine turns less
            // a quarter is the two the table names, and the three the other augment adds are waited on
            // top of that; measured against the twelve it made of the wait instead, the quarter comes
            // to three turns and the build is a turn quicker for having been assembled backwards.
            Assert.AreEqual(10f, WaitOf(surchargeFirst: true), "the share was measured against a wait the other augment had already lengthened");
            Assert.AreEqual(10f, WaitOf(surchargeFirst: false), "the same two augments came to another wait in the other order");
        }

        [TestMethod]
        public void TheSurchargeAugmentMovesBothNumbersTheSameWhicheverWayTheBuildWasAssembled()
        {
            // One record moving two parameters at once, with an augment of another tier standing on
            // each of them: fifty more mana for a longer stun, three more turns of waiting. Both
            // shares are the ability's own, so what the surcharge augment does is settled before
            // either of the others is read â€” and stays settled when the slots are filled backwards.
            Assert.AreEqual((260, 8f), BothOf(surchargesFirst: true), "a share was measured against a number another augment had already moved");
            Assert.AreEqual((260, 8f), BothOf(surchargesFirst: false), "the same three augments came to another build in the other order");
        }

        [TestMethod]
        public void TakingTheAugmentOffAndPuttingItBackOnLeavesTheBuildWhereItWas()
        {
            // A slot is emptied and refilled whenever an augment is swapped, and the whole arrangement
            // is taken down and put back up on every pass of the binder besides. A cut fixed from the
            // number found at the moment of wearing deepens every time that happens â€” and says nothing
            // while it does.
            var ability = AbilityWith(cost: 150);
            ability.InstallUpgrades(InThisOrder(CostShareUpgrade(), StunSurcharge()));
            int assembled = ability.CostValue;

            ability.InstallUpgrades(InThisOrder(StunSurcharge()));                    // the tier-one slot is emptied
            ability.InstallUpgrades(InThisOrder(CostShareUpgrade(), StunSurcharge())); // and filled again

            Assert.AreEqual(assembled, ability.CostValue, "the same augment took more off the second time it was worn");
        }

        [TestMethod]
        public void EveryShippedCooldownLosesAWholeTurnAndTheOneTheTableNames()
        {
            foreach ((int wait, int cut, _) in s_cooldownTable)
            {
                var ability = AbilityWith(cooldown: wait);

                new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(ability);

                Assert.IsTrue(ability.Cooldown < wait, $"a quarter of {wait} turns rounded down to nothing, and the augment does nothing at all");
                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"a quarter of {wait} turns no longer comes to {cut}");
            }
        }

        [TestMethod]
        public void EveryShippedCooldownLosesAWholeTurnToTheSurchargeAndTheOneTheTableNames()
        {
            // The same walk for the second share on the same parameter. The surcharge augment is paid
            // for in mana whatever it gives back, so a wait it rounds down to nothing is worse than
            // inert â€” the player is charged more for a cast that comes round no sooner.
            foreach ((int wait, _, int cut) in s_cooldownTable)
            {
                var ability = AbilityWith(cooldown: wait);

                new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare).Apply(ability);

                Assert.IsTrue(ability.Cooldown < wait, $"the share of {wait} turns rounded down to nothing, and the augment is paid for and does nothing");
                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"the share of {wait} turns no longer comes to {cut}");
            }
        }

        [TestMethod]
        public void TheTableCoversEveryCooldownTheShippedAbilitiesAreWrittenWith()
        {
            // The control the walks above need: their table is only worth something while it is the
            // waits the game actually ships. A new ability with a base of its own has to be answered
            // for here rather than quietly left out.
            var written = ShippedCooldowns();

            CollectionAssert.AreEquivalent(
                s_cooldownTable.Select(entry => entry.Base).ToList(),
                written,
                "the shipped abilities no longer wait exactly the numbers of turns the rounding table answers for");
        }

        [TestMethod]
        public void APriceTooSmallForTheShareToReachAWholePointStillLosesOne()
        {
            // The rounding away from nothing, pinned on the parameter that has no floor under it. A
            // share of a small base is a fraction of a point, points are whole, and a cut that rounds
            // down to zero leaves the augment chosen, worn, paid for and doing nothing at all — the
            // silent refusal this system has already been bitten by once on effect durations. The
            // cooldown records can no longer stand as witness to it: their floor now catches exactly
            // the bases the rounding used to, so the price is the only place the two rules can still
            // be told apart.
            var ability = AbilityWith(cost: 1);

            new AbilityUpgradeReduceCost(CostAugment, [], 1, CostShare).Apply(ability);

            Assert.AreEqual(0, ability.CostValue, "a share too small to reach a whole point took nothing at all");
        }

        [TestMethod]
        public void ACutDeepEnoughToTakeTheWholeWaitLeavesTheAbilityWaitingATurn()
        {
            // The floor, on the only base small enough to reach it, for both augments that cut a wait.
            // What these records offer is a shorter cooldown and never the removal of one: rounding the
            // share up to a whole turn — which a share is worthless without — would otherwise take the
            // whole of a one-turn wait and hand the caster an ability that comes round every turn.
            // Nothing the game ships waits a single turn today, so this is the rule stated for the data
            // that will.
            var cut = AbilityWith(cooldown: 1);
            var surcharged = AbilityWith(cooldown: 1);

            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(cut);
            new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare).Apply(surcharged);

            Assert.AreEqual(1f, cut.Cooldown, "the cut took the ability's whole wait instead of stopping at the floor");
            Assert.AreEqual(1f, surcharged.Cooldown, "the surcharge record took the ability's whole wait instead of stopping at the floor");
        }

        [TestMethod]
        public void TheFloorHoldsBackOnlyTheCutThatWouldBreakThroughIt()
        {
            // The other half of the rule. A floor that is read before the share is measured would be a
            // second, quieter nerf to every wait in the book — the augments the players actually wear go
            // on the numbers the game ships, and those must lose exactly the turns they lost before.
            var cut = AbilityWith(cooldown: 3);
            var surcharged = AbilityWith(cooldown: 9);

            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(cut);
            new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare).Apply(surcharged);

            Assert.AreEqual(2f, cut.Cooldown, "a quarter of three turns no longer comes to one");
            Assert.AreEqual(5f, surcharged.Cooldown, "two fifths of nine turns no longer comes to four");
        }

        [TestMethod]
        public void AnAbilityThatCostsNothingAndWaitsForNothingIsLeftAlone()
        {
            // The augments now go onto every ability there is, and the book holds casts that are free
            // and instant. A floor of one unit applied blindly would hand the caster a negative price
            // on one side and a bill for a free cast on the other — and the floor the cooldown cut now
            // stops at would be a WAIT given to an ability written without one, which is the augment
            // charging the player to make their cast slower.
            var discounted = AbilityWith(cost: 0, cooldown: 0);
            var surcharged = AbilityWith(cost: 0, cooldown: 0);

            new AbilityUpgradeReduceCost(CostAugment, [], 1, CostShare).Apply(discounted);
            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare).Apply(discounted);
            new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare).Apply(surcharged);

            Assert.AreEqual(0, discounted.CostValue, "an ability that costs nothing was given a negative price");
            Assert.AreEqual(0f, discounted.Cooldown, "an ability that waits for nothing was given a negative wait");
            Assert.AreEqual(0, surcharged.CostValue, "a free cast was charged for a wait it does not have");
            Assert.AreEqual(0f, surcharged.Cooldown, "an ability that waits for nothing was given a negative wait");
        }

        [TestMethod]
        public void EveryCollapsedRecordGoesOntoAbilitiesItSharesNoTagWith()
        {
            // The claim on the whole book, honoured where no tag could have carried the augment. All
            // four records name no tag at all, so every ability in the game is a stranger to them.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds];

            foreach ((string id, int tier) in s_collapsedRecords)
            {
                AbilityUpgradeData? record = catalog.Find(id);
                Assert.IsNotNull(record, $"the shipped data declares no '{id}'");
                Assert.AreEqual(tier, record.Tier, $"'{id}' is no longer written at the tier the collapse gave it");

                var board = new AbilitySocketBoard(catalog);
                board.Sync([.. abilities.Select(ability => new AbilitySocketPlacement(Socket(ability), ability, record.Tier))]);

                string[] strangers = [.. abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability)))];
                Assert.IsTrue(strangers.Length > 0, $"'{id}' shares a tag with every shipped ability, so the claim on strangers has nothing to be proved on");

                foreach (string stranger in strangers)
                    Assert.IsTrue(board.Install(board.At(Socket(stranger)), Copy(id)), $"'{id}' claims every ability and stayed out of a slot of '{stranger}'");
            }
        }

        [TestMethod]
        public void TheDataAndTheRegistryDeclareTheSameNumberOfAugments()
        {
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();

            Assert.AreEqual(ShippedAugmentCount, catalog.All.Count, "the section no longer declares the augments the collapse left it with");
            Assert.AreEqual(ShippedAugmentCount, book.BuildableAugmentIds.Count, "the registry no longer holds factories for the augments the collapse left it with");
        }

        private static string Socket(string abilityId) => $"socket_{abilityId}";

        /// <summary>What Head Butt charges once the share and the surcharge are both worn, assembled in
        /// the order asked for.</summary>
        private static int PriceOf(bool surchargeFirst) => BuildWithSurcharge(surchargeFirst).CostValue;

        /// <summary>The same for the wait, over an augment shaped like Armageddon's: three more turns,
        /// added on top of whatever the ability already waits.</summary>
        private static float WaitOf(bool surchargeFirst)
        {
            var ability = AbilityWith(cooldown: 9);
            IAbilityUpgrade share = new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, CooldownShare);
            IAbilityUpgrade surcharge = CooldownSurchargeUpgrade();

            ability.InstallUpgrades(surchargeFirst ? InThisOrder(surcharge, share) : InThisOrder(share, surcharge));

            return ability.Cooldown;
        }

        /// <summary>What the ability charges and how long it waits with the surcharge augment in its
        /// tier-one slot and a flat addition to each of its two parameters above it.</summary>
        private static (int Cost, float Cooldown) BothOf(bool surchargesFirst)
        {
            var ability = AbilityWith(cost: 150, cooldown: 9);
            IAbilityUpgrade both = new AbilityUpgradeReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeShare, SurchargeShare);
            IAbilityUpgrade stun = StunSurcharge();
            IAbilityUpgrade wait = CooldownSurchargeUpgrade();

            ability.InstallUpgrades(surchargesFirst ? InThisOrder(wait, stun, both) : InThisOrder(both, stun, wait));

            return (ability.CostValue, ability.Cooldown);
        }

        /// <summary>Head Butt wearing the cost share in one slot and the stun surcharge in another,
        /// seated in the order asked for.</summary>
        private static HeadButt BuildWithSurcharge(bool surchargeFirst)
        {
            var ability = AbilityWith(cost: 150);
            IAbilityUpgrade share = CostShareUpgrade();
            IAbilityUpgrade surcharge = StunSurcharge();

            ability.InstallUpgrades(surchargeFirst ? InThisOrder(surcharge, share) : InThisOrder(share, surcharge));

            return ability;
        }

        /// <summary>Armageddon's shape: three more turns of waiting, flat.</summary>
        private static IAbilityUpgrade CooldownSurchargeUpgrade() =>
            new AbilityUpgradeParameterSet(CooldownSurcharge, [], 3, [(AbilityParameter.Cooldown, OperationType.Add, 3f)]);

        /// <summary>Head Butt's own tier-two augment: a longer stun bought with fifty more mana.</summary>
        private static IAbilityUpgrade StunSurcharge() =>
            new AbilityUpgradeParameterSet(CostSurcharge, [], 2,
                [(HeadButt.Parameters.StunDuration, OperationType.Add, 1f), (AbilityParameter.CostValue, OperationType.Add, 50f)]);

        private static IAbilityUpgrade CostShareUpgrade() => new AbilityUpgradeReduceCost(CostAugment, [], 1, CostShare);

        /// <summary>The distinct cooldowns the shipped abilities declare, read off the files. Casts
        /// that wait for nothing are left out â€” they are answered by their own walk.</summary>
        private static List<int> ShippedCooldowns()
        {
            List<int> waits = [];

            foreach (JObject ability in ShippedAbilities())
                waits.Add(ability.Value<int>("cooldown"));

            return [.. waits.Where(wait => wait > 0).Distinct().OrderBy(wait => wait)];
        }

        /// <summary>The wait and the price one shipped ability is written with.</summary>
        private static (int Cooldown, int CostValue) ShippedBaseOf(string abilityId)
        {
            foreach (JObject ability in ShippedAbilities())
                if (string.Equals(ability.Value<string>("id"), abilityId, StringComparison.Ordinal))
                    return (ability.Value<int>("cooldown"), ability.Value<int>("costValue"));

            Assert.Fail($"the shipped data declares no '{abilityId}'");
            return default;
        }

        /// <summary>Every ability record the shipped catalog writes, read straight from the files.</summary>
        private static IEnumerable<JObject> ShippedAbilities()
        {
            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject ability in JObject.Parse(File.ReadAllText(path))["abilities"] as JArray ?? [])
                    yield return ability;
        }
    }
}
