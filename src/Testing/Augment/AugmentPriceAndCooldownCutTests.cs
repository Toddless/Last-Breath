namespace LastBreathTest.Augment
{
    using Ability;
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
    /// every ability honours, so all of them are written once — and the two numbers they move are stated
    /// in two different ways, which is the whole of what these walks are about.
    ///
    /// A PRICE is a share of the ability's own. The records go on every ability there is, prices running
    /// from nothing at all to five hundred, and one flat figure would be a near-free cast at the cheap
    /// end and nothing worth choosing at the expensive one. A WAIT is whole turns, because the design
    /// line counts turns: a player reads "two turns sooner" and gets two turns sooner wherever the
    /// record is worn.
    ///
    /// Each form has one thing to answer for, and both are walked below. A share of a small base is a
    /// fraction of a point, and points are whole: a cut that rounds down to zero would leave an augment
    /// chosen, worn, paid for and doing nothing at all — the silent refusal this system has already been
    /// bitten by once on effect durations. A flat cut has no rounding to be saved by and no base to be
    /// measured against, so what it needs instead is a FLOOR: turns taken off a short wait run past zero
    /// into a negative one, which never counts back down and leaves the ability uncastable for good.
    ///
    /// And the shares are shares OF something: measured against whatever the parameter happens to carry
    /// when the augment goes in, the same build would be worth one price assembled in one order and
    /// another price in the other.
    /// </summary>
    [TestClass]
    public class AugmentPriceAndCooldownCutTests
    {
        private const string CostAugment = "Augment_Reduce_Cost";
        private const string CooldownAugment = "Augment_Reduce_Cooldown";

        /// <summary>The record that took the place of three: a shorter wait bought with a higher price —
        /// the turns flat, the price a share of the ability's own.</summary>
        private const string SurchargeAugment = "Augment_Reduce_Cooldown_Add_Cost";

        /// <summary>The record that cuts the wait AND the price together, and the deepest flat cut of the
        /// three — deep enough to reach the floor on a wait the book does not ship yet, which is why the
        /// floor is walked on this one.</summary>
        private const string FlatCutAugment = "Augment_Reduce_Cooldown_And_Cost";

        /// <summary>The record that took the place of two: the price paid in health. It moves no
        /// number at all — the cost type is categorical — so it appears here only where the collapse
        /// is what is being walked.</summary>
        private const string HealthCostAugment = "Augment_Cost_Type_Health";

        private const float CostShare = 0.3f;
        private const float CooldownTurns = 1f;
        private const float SurchargeTurns = 2f;
        private const float SurchargeCostShare = 0.15f;

        /// <summary>The augment of another tier the cost share has to share a parameter with: Head
        /// Butt's longer stun, bought with fifty more mana. The shipped record, built the way the
        /// registry builds it — two flat moves on two parameters.</summary>
        private const string CostSurcharge = "Augment_Extend_Stun_Add_Cost";

        /// <summary>The same on the other parameter — Armageddon reaching every target and waiting three
        /// turns longer for it. Stood in for by an upgrade of the same shape (a flat addition to the
        /// wait), because Armageddon itself has nothing to do with the question.</summary>
        private const string CooldownSurcharge = "Augment_Armageddon_All_Targets";

        /// <summary>How many augments the game holds after the collapse — the same number in the data
        /// and in the registry, because one half without the other is either an offer nothing builds
        /// or code nothing can reach.</summary>
        private const int ShippedAugmentCount = 97;

        /// <summary>Every record the collapse of the base-contract families left behind, with the tier
        /// it was written at. All four claim the whole book, which is the widest reach in the system
        /// and the thing a lost marker would take away in silence.</summary>
        private static readonly (string Id, int Tier)[] s_collapsedRecords =
        [
            (CostAugment, 1), (CooldownAugment, 1), (SurchargeAugment, 1), (HealthCostAugment, 2)
        ];

        /// <summary>Every cooldown the shipped abilities are written with, and the turns each record
        /// takes off it. Held as a table rather than recomputed: the cut is the same figure everywhere,
        /// so the column that matters is the wait it LEAVES, and a base that stops being shipped or a
        /// cut that stops clearing the floor shows up here as the numbers it moves.</summary>
        private static readonly (int Base, int PlainCut, int SurchargeCut)[] s_cooldownTable =
        [
            (3, 1, 2), (4, 1, 2), (5, 1, 2), (6, 1, 2), (7, 1, 2), (9, 1, 2)
        ];

        /// <summary>The three abilities whose own cooldown augments the surcharge record absorbed: the
        /// turns it takes off each, and what its price share comes to on what each of them charges. The
        /// bases are read off the files, so an ability repriced after the collapse fails here instead of
        /// quietly getting another augment than the one that was agreed.</summary>
        private static readonly (string AbilityId, int CooldownCut, int CostSurchargeValue)[] s_absorbedAbilities =
        [
            ("Ability_Porcupine", 2, 15), ("Ability_Ice_Shards", 2, 30), ("Ability_Poison_Explosion", 2, 15)
        ];

        [TestMethod]
        public void TheCostShareIsTakenOffThePriceOfTheAbilityItGoesOn()
        {
            // The whole reason the value is a share: the same record on the cheapest ability the game
            // ships and on the dearest, taking a proportional bite out of each.
            var cheap = AbilityWith(cost: 100);
            var dear = AbilityWith(cost: 500);

            new AugmentReduceCost(CostAugment, [], 1, CostShare).Apply(cheap);
            new AugmentReduceCost(CostAugment, [], 1, CostShare).Apply(dear);

            Assert.AreEqual(70, cheap.CostValue, "the share was not measured against the cheap ability's own price");
            Assert.AreEqual(350, dear.CostValue, "the share was not measured against the dear ability's own price");
        }

        [TestMethod]
        public void TheCooldownCutIsTheSameWholeTurnsWhateverTheAbilityWaits()
        {
            // The mirror of the case above, and the reason the two parameters are stated differently: a
            // wait is counted, so the record takes the turns it names off the quick ability and off the
            // slow one alike. A share here would be worth a fraction of a turn on the short waits.
            var quick = AbilityWith(cooldown: 4);
            var slow = AbilityWith(cooldown: 9);

            new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(quick);
            new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(slow);

            Assert.AreEqual(3f, quick.Cooldown, "the cut no longer takes its whole turn off the quick ability's wait");
            Assert.AreEqual(8f, slow.Cooldown, "the cut came to something other than the same turn on the slow ability's wait");
        }

        [TestMethod]
        public void TheSurchargeAugmentCutsTheWaitAndRaisesThePriceOfTheAbilityItGoesOn()
        {
            // The three records the surcharge augment replaced, each on the ability it used to belong
            // to: one record now, moving the two parameters in the two forms — flat turns off the wait,
            // a share of the price onto the bill. Both are read off the ability's own base, so a mistake
            // in either shows up as one of these two figures and not as a build that merely feels off.
            foreach ((string abilityId, int cut, int surcharge) in s_absorbedAbilities)
            {
                (int wait, int price) = ShippedBaseOf(abilityId);
                var ability = AbilityWith(cost: price, cooldown: wait);

                new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare).Apply(ability);

                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"the cut no longer takes {cut} turns off the {wait} '{abilityId}' waits");
                Assert.AreEqual(price + surcharge, ability.CostValue, $"the share no longer adds {surcharge} to the {price} '{abilityId}' charges");
            }
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
        public void TheCooldownCutIsTheSameWhicheverAugmentTheBuildWasStartedFrom()
        {
            // The same on the other parameter. A flat cut cannot drift with the base it is read against
            // — that is the point of stating it flat — but the FLOOR under it is read at the moment the
            // cut is applied, and an augment lengthening the wait beside it moves what the floor is
            // measuring. Order-independence is therefore still a claim about this pair rather than a
            // property the form hands over for free.
            Assert.AreEqual(11f, WaitOf(surchargeFirst: true), "the cut came out different beside an augment that had already lengthened the wait");
            Assert.AreEqual(11f, WaitOf(surchargeFirst: false), "the same two augments came to another wait in the other order");
        }

        [TestMethod]
        public void TheSurchargeAugmentMovesBothNumbersTheSameWhicheverWayTheBuildWasAssembled()
        {
            // One record moving two parameters at once, with an augment of another tier standing on
            // each of them: fifty more mana for a longer stun, three more turns of waiting. Its cut is
            // a figure of its own and its bill is a share of the ability's own price, so what the
            // surcharge augment does is settled before either of the others is read — and stays settled
            // when the slots are filled backwards.
            Assert.AreEqual((223, 10f), BothOf(surchargesFirst: true), "a move was measured against a number another augment had already changed");
            Assert.AreEqual((223, 10f), BothOf(surchargesFirst: false), "the same three augments came to another build in the other order");
        }

        [TestMethod]
        public void TakingTheAugmentOffAndPuttingItBackOnLeavesTheBuildWhereItWas()
        {
            // A slot is emptied and refilled whenever an augment is swapped, and the whole arrangement
            // is taken down and put back up on every pass of the binder besides. A cut fixed from the
            // number found at the moment of wearing deepens every time that happens — and says nothing
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

                new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(ability);

                Assert.IsTrue(ability.Cooldown < wait, $"the floor swallowed the whole cut on a {wait}-turn wait, and the augment does nothing at all");
                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"the cut no longer takes {cut} turns off a wait of {wait}");
            }
        }

        [TestMethod]
        public void EveryShippedCooldownLosesAWholeTurnToTheSurchargeAndTheOneTheTableNames()
        {
            // The same walk for the second record cutting the same parameter. The surcharge augment is
            // paid for in mana whatever it gives back, so a wait its cut fails to shorten is worse than
            // inert — the player is charged more for a cast that comes round no sooner.
            foreach ((int wait, _, int cut) in s_cooldownTable)
            {
                var ability = AbilityWith(cooldown: wait);

                new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare).Apply(ability);

                Assert.IsTrue(ability.Cooldown < wait, $"the floor swallowed the whole cut on a {wait}-turn wait, and the augment is paid for and does nothing");
                Assert.AreEqual((float)(wait - cut), ability.Cooldown, $"the cut no longer takes {cut} turns off a wait of {wait}");
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

            new AugmentReduceCost(CostAugment, [], 1, CostShare).Apply(ability);

            Assert.AreEqual(0, ability.CostValue, "a share too small to reach a whole point took nothing at all");
        }

        [TestMethod]
        public void ACutDeepEnoughToTakeTheWholeWaitLeavesTheAbilityWaitingATurn()
        {
            // The floor, on the only base small enough to reach it, for both augments that cut a wait.
            // What these records offer is a shorter cooldown and never the removal of one: a whole turn
            // taken off a one-turn wait would otherwise leave the caster an ability that comes round
            // every turn. Nothing the game ships waits a single turn today, so this is the rule stated
            // for the data that will.
            var cut = AbilityWith(cooldown: 1);
            var surcharged = AbilityWith(cooldown: 1);

            new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(cut);
            new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare).Apply(surcharged);

            Assert.AreEqual(1f, cut.Cooldown, "the cut took the ability's whole wait instead of stopping at the floor");
            Assert.AreEqual(1f, surcharged.Cooldown, "the surcharge record took the ability's whole wait instead of stopping at the floor");
        }

        [TestMethod]
        public void TheFlatCutStopsAtTheFloorAndLeavesAnInstantCastInstant()
        {
            // The DEEPEST of the three flat cuts, taken past what any wait in the book could absorb. A
            // figure has no rounding to be saved by: two turns off a one-turn wait is minus one, and a
            // negative wait never counts back down to nought, so the ability could never be cast again
            // for the rest of the fight. Unreachable on the data shipped today (the shortest wait in the
            // book is three turns), which is exactly why the arithmetic is pinned here instead of
            // resting on the guard alone.
            var shortest = AbilityWith(cooldown: 1);
            var instant = AbilityWith(cooldown: 0);
            const float TwoTurns = 2f;

            new AugmentReduceCooldownAndCost(FlatCutAugment, [], 1, TwoTurns, CostShare).Apply(shortest);
            new AugmentReduceCooldownAndCost(FlatCutAugment, [], 1, TwoTurns, CostShare).Apply(instant);

            Assert.AreEqual(1f, shortest.Cooldown, "a flat cut deeper than the wait drove it past the floor");
            Assert.AreEqual(0f, instant.Cooldown, "an instant cast was handed a wait it was never written with");

            // The floor is what holds it, and nothing else: the same move without one is the negative wait
            // the record would otherwise ship.
            Assert.AreEqual(-1f,
                new SimpleAbilityParameterDecorator(
                    AbilityParameter.Cooldown, Priority.Weak, OperationType.Subtract, TwoTurns, "probe", FlatCutAugment)
                    .Decorate(1f),
                "the flat decorator no longer reaches a negative wait without a floor, so the floor above proves nothing");
        }

        [TestMethod]
        public void TheFloorHoldsBackOnlyTheCutThatWouldBreakThroughIt()
        {
            // The other half of the rule. A floor that held back every cut rather than the one breaking
            // through it would be a second, quieter nerf to every wait in the book — the augments the
            // players actually wear go on the numbers the game ships, and those must lose exactly the
            // turns the records name.
            var cut = AbilityWith(cooldown: 3);
            var surcharged = AbilityWith(cooldown: 9);

            new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(cut);
            new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare).Apply(surcharged);

            Assert.AreEqual(2f, cut.Cooldown, "one turn off a wait of three no longer leaves two");
            Assert.AreEqual(7f, surcharged.Cooldown, "two turns off a wait of nine no longer leave seven");
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

            new AugmentReduceCost(CostAugment, [], 1, CostShare).Apply(discounted);
            new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns).Apply(discounted);
            new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare).Apply(surcharged);

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
                AbilityAugmentData? record = catalog.Find(id);
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
            IAugment cut = new AugmentReduceCooldown(CooldownAugment, [], 1, CooldownTurns);
            IAugment surcharge = CooldownSurchargeUpgrade();

            ability.InstallUpgrades(surchargeFirst ? InThisOrder(surcharge, cut) : InThisOrder(cut, surcharge));

            return ability.Cooldown;
        }

        /// <summary>What the ability charges and how long it waits with the surcharge augment in its
        /// tier-one slot and a flat addition to each of its two parameters above it.</summary>
        private static (int Cost, float Cooldown) BothOf(bool surchargesFirst)
        {
            var ability = AbilityWith(cost: 150, cooldown: 9);
            IAugment both = new AugmentReduceCooldownAddCost(SurchargeAugment, [], 1, SurchargeTurns, SurchargeCostShare);
            IAugment stun = StunSurcharge();
            IAugment wait = CooldownSurchargeUpgrade();

            ability.InstallUpgrades(surchargesFirst ? InThisOrder(wait, stun, both) : InThisOrder(both, stun, wait));

            return (ability.CostValue, ability.Cooldown);
        }

        /// <summary>Head Butt wearing the cost share in one slot and the stun surcharge in another,
        /// seated in the order asked for.</summary>
        private static HeadButt BuildWithSurcharge(bool surchargeFirst)
        {
            var ability = AbilityWith(cost: 150);
            IAugment share = CostShareUpgrade();
            IAugment surcharge = StunSurcharge();

            ability.InstallUpgrades(surchargeFirst ? InThisOrder(surcharge, share) : InThisOrder(share, surcharge));

            return ability;
        }

        /// <summary>Armageddon's shape: three more turns of waiting, flat.</summary>
        private static IAugment CooldownSurchargeUpgrade() =>
            new AugmentParameterSet(CooldownSurcharge, [], 3, [(AbilityParameter.Cooldown, OperationType.Add, 3f)]);

        /// <summary>Head Butt's own tier-two augment: a longer stun bought with fifty more mana.</summary>
        private static IAugment StunSurcharge() =>
            new AugmentParameterSet(CostSurcharge, [], 2,
                [(AbilityParameter.StunDuration, OperationType.Add, 1f), (AbilityParameter.CostValue, OperationType.Add, 50f)]);

        private static IAugment CostShareUpgrade() => new AugmentReduceCost(CostAugment, [], 1, CostShare);

        /// <summary>The distinct cooldowns the shipped abilities declare, read off the files. Casts
        /// that wait for nothing are left out — they are answered by their own walk.</summary>
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
