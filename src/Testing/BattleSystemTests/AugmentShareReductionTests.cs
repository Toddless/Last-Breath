namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.HeadButt;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Enums;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The two augments that took the place of thirty-eight. Both work through the base contract every
    /// ability honours, so both are written once and state what they take as a share of the number
    /// they cut rather than as a number of their own: they go on every ability there is — prices
    /// running from nothing at all to five hundred, waits from no turns to nine — and one flat figure
    /// would be a near-free cast at the cheap end and nothing worth choosing at the expensive one.
    ///
    /// Two things a share has to answer for, and both are walked below. A share of a small base is a
    /// fraction of a turn, and turns are whole: a cut that rounds down to zero would leave an augment
    /// chosen, worn, paid for and doing nothing at all — the silent refusal this system has already
    /// been bitten by once on effect durations. And a share is a share OF something: measured against
    /// whatever the parameter happens to carry when the augment goes in, the same build would be worth
    /// one price assembled in one order and another price in the other.
    /// </summary>
    [TestClass]
    public class AugmentShareReductionTests
    {
        private const string CostAugment = "Ability_Upgrade_Reduce_Cost";
        private const string CooldownAugment = "Ability_Upgrade_Reduce_Cooldown";

        /// <summary>The augment of another tier the cost share has to share a parameter with: Head
        /// Butt's longer stun, bought with fifty more mana. The shipped record and the shipped
        /// class.</summary>
        private const string CostSurcharge = "Ability_Hb_Upgrade_Extend_Stun_Add_Cost";

        /// <summary>The same on the other parameter — Armageddon reaching every target and waiting three
        /// turns longer for it. Stood in for by an upgrade of the same shape (a flat addition to the
        /// wait), because Armageddon itself has nothing to do with the question.</summary>
        private const string CooldownSurcharge = "Ability_Arm_Upgrade_All_Targets";

        /// <summary>How many augments the game holds after the collapse — the same number in the data
        /// and in the registry, because one half without the other is either an offer nothing builds
        /// or code nothing can reach.</summary>
        private const int ShippedAugmentCount = 153;

        /// <summary>Every cooldown the shipped abilities are written with, and what a quarter of it
        /// comes to once it is rounded to whole turns. Held as a table rather than recomputed, so a
        /// change of the rounding rule shows up as the numbers it moves.</summary>
        private static readonly (int Base, int Cut)[] s_cooldownTable =
        [
            (3, 1), (4, 1), (5, 1), (6, 2), (7, 2), (9, 2)
        ];

        [TestMethod]
        public void TheCostShareIsTakenOffThePriceOfTheAbilityItGoesOn()
        {
            // The whole reason the value is a share: the same record on the cheapest ability the game
            // ships and on the dearest, taking a proportional bite out of each.
            var cheap = AbilityWith(cost: 100);
            var dear = AbilityWith(cost: 500);

            new AbilityUpgradeReduceCost(CostAugment, [], 1, 0.3f).Apply(cheap);
            new AbilityUpgradeReduceCost(CostAugment, [], 1, 0.3f).Apply(dear);

            Assert.AreEqual(70, cheap.CostValue, "the share was not measured against the cheap ability's own price");
            Assert.AreEqual(350, dear.CostValue, "the share was not measured against the dear ability's own price");
        }

        [TestMethod]
        public void TheCooldownShareIsTakenOffTheWaitOfTheAbilityItGoesOn()
        {
            var quick = AbilityWith(cooldown: 4);
            var slow = AbilityWith(cooldown: 9);

            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f).Apply(quick);
            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f).Apply(slow);

            Assert.AreEqual(3f, quick.Cooldown, "the share was not measured against the quick ability's own wait");
            Assert.AreEqual(7f, slow.Cooldown, "the share was not measured against the slow ability's own wait");
        }

        [TestMethod]
        public void TheCostShareIsTheSameWhicheverAugmentTheBuildWasStartedFrom()
        {
            // The share is a share of the ability's price, and an augment of another tier charging fifty
            // more mana for a longer stun does not change what the ability costs to begin with. Read off
            // the number the parameter happens to carry instead, the same two augments would be worth
            // one price picked in one order and another price picked in the other — and the build would
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
        public void TakingTheAugmentOffAndPuttingItBackOnLeavesTheBuildWhereItWas()
        {
            // The tier slot holds one augment, and swapping it is a click away in the augment window.
            // A cut fixed from the number found at the moment of wearing deepens every time the slot is
            // refilled over another augment — and says nothing while it does.
            var ability = BuildWithSurcharge(surchargeFirst: false);
            int assembled = ability.CostValue;

            ability.ClearUpgrade(1);
            ability.SelectUpgrade(1, CostAugment);

            Assert.AreEqual(assembled, ability.CostValue, "the same augment took more off the second time it was worn");
        }

        [TestMethod]
        public void EveryShippedCooldownLosesAWholeTurnAndTheOneTheTableNames()
        {
            foreach ((int wait, int cut) in s_cooldownTable)
            {
                var ability = AbilityWith(cooldown: wait);

                new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f).Apply(ability);

                Assert.IsTrue(ability.Cooldown < wait, $"a quarter of {wait} turns rounded down to nothing, and the augment does nothing at all");
                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"a quarter of {wait} turns no longer comes to {cut}");
            }
        }

        [TestMethod]
        public void TheTableCoversEveryCooldownTheShippedAbilitiesAreWrittenWith()
        {
            // The control the walk above needs: its table is only worth something while it is the
            // waits the game actually ships. A new ability with a base of its own has to be answered
            // for here rather than quietly left out.
            var written = ShippedCooldowns();

            CollectionAssert.AreEquivalent(
                s_cooldownTable.Select(entry => entry.Base).ToList(),
                written,
                "the shipped abilities no longer wait exactly the numbers of turns the rounding table answers for");
        }

        [TestMethod]
        public void AWaitTooShortForTheShareToReachATurnStillLosesOne()
        {
            // The floor, on the only base small enough to need it. Nothing the game ships waits a
            // single turn today, so this is the rule stated for the data that will: a quarter of one
            // turn is a quarter of a turn, and rounding it honestly leaves the augment inert.
            var ability = AbilityWith(cooldown: 1);

            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f).Apply(ability);

            Assert.AreEqual(0f, ability.Cooldown, "a share too small to reach a whole turn took nothing at all");
        }

        [TestMethod]
        public void AnAbilityThatCostsNothingAndWaitsForNothingIsLeftAlone()
        {
            // Both augments now go onto every ability there is, and the book holds casts that are free
            // and instant. A floor of one unit applied blindly would hand the caster a negative price.
            var free = AbilityWith(cost: 0, cooldown: 0);

            new AbilityUpgradeReduceCost(CostAugment, [], 1, 0.3f).Apply(free);
            new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f).Apply(free);

            Assert.AreEqual(0, free.CostValue, "an ability that costs nothing was given a negative price");
            Assert.AreEqual(0f, free.Cooldown, "an ability that waits for nothing was given a negative wait");
        }

        [TestMethod]
        public void BothRecordsGoOntoAbilitiesTheyShareNoTagWith()
        {
            // The claim on the whole book, honoured where no tag could have carried the augment. Both
            // records name no tag at all, so every ability in the game is a stranger to them.
            AbilityProvider catalog = ShippedCatalog();
            string[] abilities = [.. catalog.KnownAbilityIds];

            foreach (string id in new[] { CostAugment, CooldownAugment })
            {
                AbilityUpgradeData? record = catalog.Find(id);
                Assert.IsNotNull(record, $"the shipped data declares no '{id}'");
                Assert.AreEqual(1, record.Tier, $"'{id}' is no longer the plainest augment there is");

                var board = new AbilitySocketBoard(catalog);
                board.Sync([.. abilities.Select(ability => new AbilitySocketPlacement(Socket(ability), ability, record.Tier))]);

                string[] strangers = [.. abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability)))];
                Assert.IsTrue(strangers.Length > 0, $"'{id}' shares a tag with every shipped ability, so the claim on strangers has nothing to be proved on");

                foreach (string stranger in strangers)
                    Assert.IsTrue(board.Install(Socket(stranger), id), $"'{id}' claims every ability and stayed out of a slot of '{stranger}'");
            }
        }

        [TestMethod]
        public void TheDataAndTheRegistryDeclareTheSameNumberOfAugments()
        {
            AbilityProvider catalog = ShippedCatalog();

            Assert.AreEqual(ShippedAugmentCount, catalog.KnownAugmentIds.Count, "the section no longer declares the augments the collapse left it with");
            Assert.AreEqual(ShippedAugmentCount, catalog.BuildableAugmentIds.Count, "the registry no longer holds factories for the augments the collapse left it with");
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
            ability.SetAbilityUpgrades(new()
            {
                [1] = [new AbilityUpgradeReduceCooldown(CooldownAugment, [], 1, 0.25f)],
                [3] = [new AbilityUpgradeParameterSet(CooldownSurcharge, [], 3, [(AbilityParameter.Cooldown, 3f)])]
            });

            int[] order = surchargeFirst ? [3, 1] : [1, 3];
            foreach (int tier in order)
                ability.SelectUpgrade(tier, tier == 1 ? CooldownAugment : CooldownSurcharge);

            return ability.Cooldown;
        }

        /// <summary>Head Butt wearing the cost share in its tier-one slot and the stun surcharge in its
        /// tier-two one, chosen in the order asked for — the two are seated through the ability's own
        /// selection, because that is where a slot is emptied and refilled.</summary>
        private static HeadButt BuildWithSurcharge(bool surchargeFirst)
        {
            var ability = AbilityWith(cost: 150);
            ability.SetAbilityUpgrades(new()
            {
                [1] = [new AbilityUpgradeReduceCost(CostAugment, [], 1, 0.3f)],
                [2] = [new HbUpgradeExtendStunAddCost(CostSurcharge, [], 2, 1f, 50f)]
            });

            int[] order = surchargeFirst ? [2, 1] : [1, 2];
            foreach (int tier in order)
                ability.SelectUpgrade(tier, tier == 1 ? CostAugment : CostSurcharge);

            return ability;
        }

        /// <summary>A stand-in ability carrying the base numbers under test. Any ability would do —
        /// both augments decorate keys every ability registers as part of the base contract.</summary>
        private static HeadButt AbilityWith(int cost = 100, int cooldown = 5) => new(new AbilityBaseData
        {
            Id = "Ability_Head_Butt",
            Cooldown = cooldown,
            CostValue = cost,
            CostsType = Costs.Mana,
            AbilityProperties = new() { ["stunDuration"] = 1, ["attacks"] = 2 }
        });

        /// <summary>The distinct cooldowns the shipped abilities declare, read off the files. Casts
        /// that wait for nothing are left out — they are answered by their own walk.</summary>
        private static List<int> ShippedCooldowns()
        {
            List<int> waits = [];

            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject ability in JObject.Parse(File.ReadAllText(path))["abilities"] as JArray ?? [])
                    waits.Add(ability.Value<int>("cooldown"));

            return [.. waits.Where(wait => wait > 0).Distinct().OrderBy(wait => wait)];
        }

        /// <summary>The shipped ability data as the game reads it: the real source, the real loader,
        /// the real parser.</summary>
        private static AbilityProvider ShippedCatalog()
        {
            var provider = new AbilityProvider();
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return provider;
        }
    }
}
