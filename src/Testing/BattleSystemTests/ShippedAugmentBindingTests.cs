namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// What the shipped records now say about where they belong, put to the rule that reads them. The
    /// declarations were derived from the registry itself: an augment reaching into the members of one
    /// ability names that ability, an augment working through the contract every ability honours
    /// claims the whole book, and everything else says neither yet. Those three readings are only
    /// worth what the seating does with them, so the walks below take the shipped file down the loader
    /// the game uses and ask the real board — every binding seats where it was written and nowhere
    /// else, every claim on the book seats where no tag would have carried it, and a record that has
    /// said nothing so far is still refused, which is what keeps the filling from having quietly made
    /// the whole registry fit everywhere.
    /// </summary>
    [TestClass]
    public class ShippedAugmentBindingTests
    {
        /// <summary>How many records name an ability. The registry triage
        /// (<c>Docs/UpgradeRegistryTriage.md</c>) counted them: 58 reaching into the members of one
        /// ability class, and 25 more that look general and are inert or throw anywhere else. The
        /// number is held so that bindings cannot go missing in a bulk edit the way they were missing
        /// before — the failure of a lost binding is an augment silently offered to the whole family.</summary>
        private const int BoundRecords = 83;

        /// <summary>How many records claim every ability there is — cost, cooldown and the other
        /// levers of the base contract. Held for the same reason as <see cref="BoundRecords"/>, and
        /// with more at stake: universality is the widest reach in the system.</summary>
        private const int UniversalRecords = 12;

        [TestMethod]
        public void EveryBoundRecordNamesTheAbilityItIsDeclaredUnder()
        {
            // A binding is written next to the ability it belongs to, so the two must be the same
            // ability. Naming another one would not fail loudly anywhere: the augment would simply
            // never fit the slots of the ability whose block declares it, and the ability it does
            // name would be offered an augment written against members it does not have.
            AbilityProvider catalog = ShippedCatalog();
            int bound = 0;

            foreach ((string id, string home, AbilityUpgradeData record) in ShippedRecords(catalog).Where(Bound))
            {
                Assert.AreEqual(home, record.AbilityId, $"'{id}' is declared under '{home}' and binds itself to another ability");
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
            AbilityProvider catalog = ShippedCatalog();
            string[] abilities = [.. catalog.KnownAbilityIds];
            int bound = 0;

            foreach ((string id, _, AbilityUpgradeData record) in ShippedRecords(catalog).Where(Bound))
            {
                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);

                foreach (string ability in abilities)
                {
                    bool seated = board.Install(SocketOf(ability), id);

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
            AbilityProvider catalog = ShippedCatalog();
            string[] abilities = [.. catalog.KnownAbilityIds];
            int universal = 0;

            foreach ((string id, _, AbilityUpgradeData record) in ShippedRecords(catalog).Where(Universal))
            {
                Assert.AreEqual(string.Empty, record.AbilityId,
                    $"'{id}' claims every ability and names one, and the rule refuses a record answering the same question twice");
                string[] strangers = [.. abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability)))];
                Assert.IsTrue(strangers.Length > 0, $"'{id}' shares a tag with every ability, so seating it says nothing about its claim");

                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);
                foreach (string stranger in strangers)
                    Assert.IsTrue(board.Install(SocketOf(stranger), id),
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
            AbilityProvider catalog = ShippedCatalog();
            string[] abilities = [.. catalog.KnownAbilityIds];
            int silent = 0;

            foreach ((string id, _, AbilityUpgradeData record) in ShippedRecords(catalog).Where(entry => !Bound(entry) && !Universal(entry)))
            {
                IAbilitySocketBoard board = BoardOver(catalog, abilities, record.Tier);

                foreach (string stranger in abilities.Where(ability => !AbilityTags.SharesAny(record.Tags, catalog.TagsOf(ability))))
                    Assert.IsFalse(board.Install(SocketOf(stranger), id),
                        $"'{id}' names no ability, claims no book and shares no tag with '{stranger}', and went in anyway");

                silent++;
            }

            Assert.IsTrue(silent > 0, "every shipped record declares a binding or a claim, so the refusal has nothing left to be proved on");
        }

        private static bool Bound((string Id, string Home, AbilityUpgradeData Record) entry) =>
            !string.IsNullOrWhiteSpace(entry.Record.AbilityId);

        private static bool Universal((string Id, string Home, AbilityUpgradeData Record) entry) =>
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

        /// <summary>The shipped ability data as the game reads it: the real source, the real loader,
        /// the real parser. A record the loader did not produce is a record the rule never sees.</summary>
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

        /// <summary>Every shipped augment paired with the ability it is declared under and with the
        /// record the loader made of it. Where a record is written is read off the files themselves —
        /// the catalog answers by id alone and has no opinion about it.</summary>
        private static IEnumerable<(string Id, string Home, AbilityUpgradeData Record)> ShippedRecords(IAbilityAugmentCatalog catalog)
        {
            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject ability in Children(JObject.Parse(File.ReadAllText(path)), "abilities"))
                {
                    string home = ability.Value<string>("id") ?? string.Empty;

                    foreach (JObject augment in Children(ability, "upgrades"))
                    {
                        string id = augment.Value<string>("id") ?? string.Empty;
                        AbilityUpgradeData? record = catalog.Find(id);
                        Assert.IsNotNull(record, $"the catalog does not hold '{id}', declared by '{home}'");

                        yield return (id, home, record);
                    }
                }
        }

        private static IEnumerable<JObject> Children(JObject owner, string property) =>
            owner[property] is { } list ? list.OfType<JObject>() : [];
    }
}
