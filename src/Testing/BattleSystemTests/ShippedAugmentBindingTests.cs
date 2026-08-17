namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;
    using static AugmentCopies;

    /// <summary>
    /// What the shipped records say about where they belong, put to the rule that reads them. The
    /// declarations were derived from the registry itself: an augment reaching into the members of one
    /// ability names that ability, an augment working through the contract every ability honours
    /// claims the whole book, and everything else says neither yet. Those three readings are only
    /// worth what the seating does with them, so the walks below take the shipped file down the loader
    /// the game uses and ask the real board â€” every binding seats where it was written and nowhere
    /// else, every claim on the book seats where no tag would have carried it, and a record that has
    /// said nothing so far is still refused, which is what keeps the filling from having quietly made
    /// the whole registry fit everywhere.
    /// </summary>
    [TestClass]
    public class ShippedAugmentBindingTests
    {
        /// <summary>How many records the section declares. Held because the records no longer sit
        /// inside the abilities: a block that used to go missing took its ability's augments with it
        /// and left the rest readable, and a section loses them one bulk edit at a time.
        /// Recounted at the owner's catalog cleanup (54 records removed).</summary>
        private const int ShippedRecordCount = 84;

        /// <summary>How many records name an ability — held because a lost binding is an augment
        /// silently offered to the whole family. Recounted at the catalog cleanup; includes the
        /// naming-rule binding and Augment_Lucky_Crit, bound at the tag-vocabulary pass by the G-1
        /// precedent (typed factory, straying tags). Three left at CL-4: the health-regen record's key
        /// was generalised (the binding was standing in for that), and the two fury-burn records now
        /// travel by the 'fury' tag the owner's markup gave the ability.</summary>
        /// <remarks>Seven more left at CL-7: the card list gives them fitting tags, and each stands on a
        /// shared key or carries its own behaviour, so the binding was the only thing holding them in.</remarks>
        private const int BoundRecords = 48;

        /// <summary>How many records claim every ability there is â€” cost, cooldown and the other
        /// levers of the base contract. Held for the same reason as <see cref="BoundRecords"/>, and
        /// with more at stake: universality is the widest reach in the system. Four of them came out
        /// of the collapse of the base-contract families â€” the plain cost cut, the plain cooldown cut,
        /// the cooldown bought with a higher price and the price paid in health â€” one record each for
        /// the whole book.</summary>
        private const int UniversalRecords = 11;

        /// <summary>How many records name neither an ability nor the whole book, and are judged by
        /// their tags alone. Most of them carry tags now; the number is held because a record losing
        /// its last tag belongs nowhere and says so nowhere.</summary>
        private const int SilentRecords = 25;

        [TestMethod]
        public void TheSectionDeclaresTheRecordsTheTriageCounted()
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            var records = ShippedRecords(catalog).ToList();

            Assert.AreEqual(ShippedRecordCount, records.Count, "the section no longer declares the records the registry holds factories for");
            Assert.AreEqual(BoundRecords, records.Count(Bound), "the shipped data no longer binds the records the registry says are written for one ability");
            Assert.AreEqual(UniversalRecords, records.Count(Universal), "the shipped data no longer claims the whole book for the records that work through the base contract");
            Assert.AreEqual(SilentRecords, records.Count(entry => !Bound(entry) && !Universal(entry)), "the records that have declared nothing yet are no longer the ones counted");
        }

        [TestMethod]
        public void EveryBoundRecordNamesAnAbilityTheBookDeclares()
        {
            // A binding used to be checked against the ability whose block held the record; the
            // records stand on their own now, so the name is all there is â€” and a name the book does
            // not carry fails loudly nowhere: the augment simply never fits any slot in the game.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            var abilities = book.KnownAbilityIds.ToHashSet(StringComparer.Ordinal);
            int bound = 0;

            foreach ((string id, AbilityAugmentData record) in ShippedRecords(catalog).Where(Bound))
            {
                Assert.IsTrue(abilities.Contains(record.AbilityId), $"'{id}' binds itself to '{record.AbilityId}', which the book does not declare");
                bound++;
            }

            Assert.AreEqual(BoundRecords, bound, "the shipped data no longer binds the records the registry says are written for one ability");
        }

        [TestMethod]
        public void ABoundRecordGoesIntoItsOwnAbilitysSocketAndIntoNoOtherAbilitysSocket()
        {
            // Both verdicts on every binding, over the whole book: the augment the record was written
            // for takes it, and the twenty-four abilities it was not written for refuse it. The slots
            // are opened at the record's own tier, so a refusal is the binding talking and not the
            // socket being too small.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds];
            int bound = 0;

            foreach ((string id, AbilityAugmentData record) in ShippedRecords(catalog).Where(Bound))
            {
                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);

                foreach (string ability in abilities)
                {
                    bool seated = board.Install(board.At(SocketOf(ability)), Copy(id));

                    if (string.Equals(ability, record.AbilityId, StringComparison.Ordinal))
                        Assert.IsTrue(seated, $"'{id}' stayed out of a tier {record.Tier} slot of '{ability}', the ability its own record names");
                    else
                        Assert.IsFalse(seated, $"'{id}' went onto '{ability}' though its record names '{record.AbilityId}'");
                }

                bound++;
            }

            Assert.AreEqual(BoundRecords, bound, "the shipped data no longer binds the records the registry says are written for one ability");
        }

        [TestMethod]
        public void AUniversalRecordGoesOntoAbilitiesItSharesNoTagWith()
        {
            // The claim is honoured where no tag could have carried the augment: the abilities under
            // test share nothing with the record, so a seating here is the claim being read and not
            // the tags quietly agreeing.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds];
            int universal = 0;

            foreach ((string id, AbilityAugmentData record) in ShippedRecords(catalog).Where(Universal))
            {
                Assert.AreEqual(string.Empty, record.AbilityId,
                    $"'{id}' claims every ability and names one, and the rule refuses a record answering the same question twice");
                string[] strangers = [.. abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability)))];
                Assert.IsTrue(strangers.Length > 0, $"'{id}' shares a tag with every ability, so seating it says nothing about its claim");

                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);
                foreach (string stranger in strangers)
                    Assert.IsTrue(board.Install(board.At(SocketOf(stranger)), Copy(id)),
                        $"'{id}' claims every ability and stayed out of a tier {record.Tier} slot of '{stranger}'");

                universal++;
            }

            Assert.AreEqual(UniversalRecords, universal, "the shipped data no longer claims the whole book for the records that work through the base contract");
        }

        [TestMethod]
        public void ARecordDeclaringNeitherBindingNorUniversalityIsStillRefused()
        {
            // The control the two walks above need. Most of the registry has said nothing yet, and if
            // the filling had made those records fit as well, the walks would be passing on a rule
            // that seats everything. A record that names no ability and claims no book is judged by
            // its tags alone, so every ability sharing none of them refuses it.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds];
            int silent = 0;

            foreach ((string id, AbilityAugmentData record) in ShippedRecords(catalog).Where(entry => !Bound(entry) && !Universal(entry)))
            {
                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);

                foreach (string stranger in abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability))))
                    Assert.IsFalse(board.Install(board.At(SocketOf(stranger)), Copy(id)),
                        $"'{id}' names no ability, claims no book and shares no tag with '{stranger}', and went in anyway");

                silent++;
            }

            Assert.IsTrue(silent > 0, "every shipped record declares a binding or a claim, so the refusal has nothing left to be proved on");
        }

        [TestMethod]
        public void EveryRecordSeatsOnAnAbilityItsOwnUpgradeWasWrittenFor()
        {
            // The hole the three walks above leave open, and the one the catalogue actually fell
            // through. They read what a record DECLARES; none of them asks what the code behind it
            // demands. An upgrade written for one ability class says so in its own type
            // (AbilityUpgrade&lt;T&gt;), and a record whose tags carry it anywhere else fails in the
            // quietest way the system has: the seating succeeds, the install reports a cast that could
            // not be made, and the player wears an augment that does nothing. A record no tag and no
            // binding carries anywhere at all is the same silence one step earlier.
            //
            // So both directions are walked here, off the demand rather than off the declaration:
            // every record reaches at least one ability its upgrade can be applied to, and every
            // ability it reaches is one of them.
            (AbilityProvider book, AbilityAugmentCatalog catalog) = ShippedAbilityData.Load();
            string[] abilities = [.. book.KnownAbilityIds];
            var made = abilities.ToDictionary(id => id, id => book.CreateAbility(id).GetType(), StringComparer.Ordinal);
            List<string> nowhere = [];
            List<string> strangers = [];

            foreach ((string id, AbilityAugmentData record) in ShippedRecords(catalog))
            {
                IAbilityAugment? upgrade = book.CreateUpgrade(record);
                Assert.IsNotNull(upgrade, $"the registry builds nothing for '{id}'");
                Type demanded = DemandedAbility(upgrade.GetType());

                string[] seats = [.. abilities.Where(ability => AugmentFit.Check(
                    new AbilitySocketPlacement(SocketOf(ability), ability, record.Tier),
                    catalog.TagsOf(ability), record, []) == AugmentFitResult.Fits)];

                if (seats.Length == 0)
                    nowhere.Add($"'{id}' (its upgrade is written for {demanded.Name})");

                string[] refused = [.. seats.Where(ability => !demanded.IsAssignableFrom(made[ability]))];
                if (refused.Length > 0)
                    strangers.Add($"'{id}' is written for {demanded.Name} and seats on {string.Join(", ", refused)}");
            }

            Assert.AreEqual(0, nowhere.Count,
                $"declared, minted, offered and seatable nowhere:\n  {string.Join("\n  ", nowhere)}");
            Assert.AreEqual(0, strangers.Count,
                $"seated where its own upgrade cannot be applied, which is an augment worn and doing nothing:\n  {string.Join("\n  ", strangers)}");
        }

        /// <summary>The ability class the code behind a record was written for — the argument of the
        /// <c>AbilityUpgrade&lt;T&gt;</c> it descends from. Upgrades working through the contract every
        /// ability honours name the base class there and so demand nothing in particular.
        ///
        /// Which is also the shape of what this walk CANNOT see, and both blind spots are the same
        /// blind spot: an upgrade whose demand is written somewhere other than its own type says
        /// <c>Ability</c> here and is judged to fit everywhere.
        /// <list type="number">
        /// <item>The hard cast inside a factory's lambda — <c>AbilityUpgradeCastEffect</c> is built for
        /// any ability and handed <c>ability =&gt; new …(((Porcupine)ability).Duration)</c>, so the
        /// demand lives in a closure and blows up at cast time rather than at install time.</item>
        /// <item>The private parameter key — every row of the parameter table is an
        /// <c>AbilityUpgradeParameterSet</c>, and a record standing on a key only one ability
        /// publishes is inert on every other one without a word: 21 of the 24 records still judged by
        /// their tags are of this kind.</item>
        /// </list>
        /// Both close the same way and not here: an ability declaring the key (wave B) and a behaviour
        /// declaring what it needs (wave C) of <c>Docs/PLAN-Augments.md</c>.</summary>
        private static Type DemandedAbility(Type upgrade)
        {
            for (Type? type = upgrade; type != null; type = type.BaseType)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AbilityAugment<>))
                    return type.GetGenericArguments()[0];

            return typeof(IAbility);
        }

        private static bool Bound((string Id, AbilityAugmentData Record) entry) =>
            !string.IsNullOrWhiteSpace(entry.Record.AbilityId);

        private static bool Universal((string Id, AbilityAugmentData Record) entry) =>
            entry.Record.FitsAnyAbility;

        /// <summary>One slot per ability in the book, all of the same tier, judged by the shipped
        /// catalog.</summary>
        private static IAbilitySocketBoard BoardOver(IAbilityAugmentCatalog catalog, IEnumerable<string> abilities, int tier)
        {
            var board = new AbilitySocketBoard(catalog);
            board.Sync([.. abilities.Select(ability => new AbilitySocketPlacement(SocketOf(ability), ability, tier))]);
            return board;
        }

        private static string SocketOf(string abilityId) => $"socket_{abilityId}";

        /// <summary>Every augment the shipped section writes, paired with the record the loader made
        /// of it. The ids are read off the files rather than taken from the catalog, so a record the
        /// loader dropped on the way shows up here as a missing one instead of never being asked
        /// about.</summary>
        private static IEnumerable<(string Id, AbilityAugmentData Record)> ShippedRecords(IAbilityAugmentCatalog catalog)
        {
            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject augment in Children(JObject.Parse(File.ReadAllText(path)), "augments"))
                {
                    string id = augment.Value<string>("id") ?? string.Empty;
                    AbilityAugmentData? record = catalog.Find(id);
                    Assert.IsNotNull(record, $"the catalog does not hold '{id}', which the section declares");

                    yield return (id, record);
                }
        }

        private static IEnumerable<JObject> Children(JObject owner, string property) =>
            owner[property] is { } list ? list.OfType<JObject>() : [];
    }
}
