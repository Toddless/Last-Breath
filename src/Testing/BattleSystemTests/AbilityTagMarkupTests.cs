namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;

    /// <summary>
    /// The tags an ability carries are not decoration: an augment that names no ability and claims no
    /// book reaches that ability through one shared tag and through nothing else. So a tag dropped in
    /// a data pass takes augments off an ability without a word — the file still parses, the vocabulary
    /// audit still passes, the registry still builds everything, and the only thing that changed is
    /// that a socket the player could fill is now offered less.
    ///
    /// What is written out below is every pair the markup leans on: an ability and a tag of its own
    /// that is the ONLY thing carrying at least one augment onto it. Pairs where the augment would
    /// arrive by another tag anyway are not here — losing such a tag costs nothing and holding it
    /// would make this a copy of the file rather than a claim about it.
    /// </summary>
    [TestClass]
    public class AbilityTagMarkupTests
    {
        /// <summary>The tier every fit below is judged at — the deepest a socket goes, so a refusal is
        /// the tags talking and never the slot being too small for the record.</summary>
        private const int DeepestSocket = 3;

        private static readonly (string AbilityId, string Tag)[] s_loadBearing =
        [
            ("Ability_Series_Of_Attacks", AbilityTags.Attack),
            ("Ability_Series_Of_Attacks", AbilityTags.Damage),

            ("Ability_Increasing_Pressure", AbilityTags.Attack),
            ("Ability_Increasing_Pressure", AbilityTags.Damage),

            ("Ability_Jar_Of_Poison", AbilityTags.Poison),
            ("Ability_Jar_Of_Poison", AbilityTags.Stacks),
            ("Ability_Jar_Of_Poison", AbilityTags.Duration),

            ("Ability_Dark_Shroud", AbilityTags.Buff),
            ("Ability_Dark_Shroud", AbilityTags.Evasion),
            ("Ability_Dark_Shroud", AbilityTags.Health),
            ("Ability_Dark_Shroud", AbilityTags.Stacks),
            ("Ability_Dark_Shroud", AbilityTags.Duration),

            ("Ability_Critical_Calculation", AbilityTags.Buff),
            ("Ability_Critical_Calculation", AbilityTags.Crit),
            ("Ability_Critical_Calculation", AbilityTags.Stacks),
            ("Ability_Critical_Calculation", AbilityTags.Duration),

            ("Ability_Poison_Explosion", AbilityTags.Poison),
            ("Ability_Poison_Explosion", AbilityTags.Execute),

            ("Ability_Poison_Coating", AbilityTags.Poison),
            ("Ability_Poison_Coating", AbilityTags.Buff),
            ("Ability_Poison_Coating", AbilityTags.Stacks),
            ("Ability_Poison_Coating", AbilityTags.Duration),

            ("Ability_Double_Strike", AbilityTags.Attack),
            ("Ability_Double_Strike", AbilityTags.Damage),
            ("Ability_Double_Strike", AbilityTags.Stacks),
            ("Ability_Double_Strike", AbilityTags.Duration),

            ("Ability_Ares_Blessing", AbilityTags.Buff),
            ("Ability_Ares_Blessing", AbilityTags.Health),
            ("Ability_Ares_Blessing", AbilityTags.Stacks),
            ("Ability_Ares_Blessing", AbilityTags.Duration),

            ("Ability_Porcupine", AbilityTags.Buff),
            ("Ability_Porcupine", AbilityTags.Armor),
            ("Ability_Porcupine", AbilityTags.Stacks),
            ("Ability_Porcupine", AbilityTags.Duration),

            ("Ability_Berserk_Fury", AbilityTags.Attack),
            ("Ability_Berserk_Fury", AbilityTags.Health),
            ("Ability_Berserk_Fury", AbilityTags.Damage),
            ("Ability_Berserk_Fury", AbilityTags.Stacks),
            ("Ability_Berserk_Fury", AbilityTags.Duration),

            ("Ability_Head_Butt", AbilityTags.Attack),
            ("Ability_Head_Butt", AbilityTags.Control),
            ("Ability_Head_Butt", AbilityTags.Damage),

            ("Ability_Sacrifice", AbilityTags.Health),
            ("Ability_Sacrifice", AbilityTags.Buff),
            ("Ability_Sacrifice", AbilityTags.Duration),

            ("Ability_Armageddon", AbilityTags.Health),
            ("Ability_Armageddon", AbilityTags.Damage),

            ("Ability_Ice_Block", AbilityTags.Control),
            ("Ability_Ice_Block", AbilityTags.Damage),
            ("Ability_Ice_Block", AbilityTags.Hit),

            ("Ability_Ice_Aegis", AbilityTags.Buff),
            ("Ability_Ice_Aegis", AbilityTags.Stacks),
            ("Ability_Ice_Aegis", AbilityTags.Duration),

            ("Ability_Ice_Shards", AbilityTags.Damage),
            ("Ability_Ice_Shards", AbilityTags.Hit),

            ("Ability_Deep_Freeze", AbilityTags.Stacks),
            ("Ability_Deep_Freeze", AbilityTags.Duration),

            ("Ability_Discharge", AbilityTags.Damage),
            ("Ability_Discharge", AbilityTags.Hit),

            ("Ability_Static_Armor", AbilityTags.Buff),
            ("Ability_Static_Armor", AbilityTags.Stacks),
            ("Ability_Static_Armor", AbilityTags.Duration),
            ("Ability_Static_Armor", AbilityTags.Damage),
            ("Ability_Static_Armor", AbilityTags.Hit),

            ("Ability_Twin_Assist_Attack", AbilityTags.Attack)
        ];

        [TestMethod]
        public void EveryTagTheMarkupLeansOnIsStillDeclaredAndStillCarriesItsAugments()
        {
            AbilityProvider catalog = ShippedCatalog();

            foreach ((string abilityId, string tag) in s_loadBearing)
            {
                List<string> tags = [.. catalog.TagsOf(abilityId)];
                Assert.IsTrue(tags.Contains(tag, StringComparer.OrdinalIgnoreCase),
                    $"'{abilityId}' no longer declares '{tag}', and the augments that reached it through that tag reach nothing now");

                List<string> lost = Lost(catalog, abilityId, tags, tag);
                Assert.IsTrue(lost.Count > 0,
                    $"'{tag}' carries no augment onto '{abilityId}' any more, so this pair says nothing and the markup it stood for is gone");
            }
        }

        [TestMethod]
        public void TheTableNamesEveryTagTheMarkupLeansOn()
        {
            // The other direction. A pair the table does not name is a tag holding augments nobody
            // wrote down, which is the same silence one edit later — the walk above would keep passing
            // while the markup it was written for is no longer the markup that ships.
            AbilityProvider catalog = ShippedCatalog();
            var written = s_loadBearing.Select(pair => $"{pair.AbilityId} / {pair.Tag}").ToHashSet(StringComparer.Ordinal);
            var carrying = new HashSet<string>(StringComparer.Ordinal);

            foreach (string abilityId in catalog.KnownAbilityIds)
            {
                List<string> tags = [.. catalog.TagsOf(abilityId)];
                foreach (string tag in tags)
                    if (Lost(catalog, abilityId, tags, tag).Count > 0)
                        carrying.Add($"{abilityId} / {tag}");
            }

            var unwritten = carrying.Except(written).OrderBy(pair => pair, StringComparer.Ordinal).ToList();
            var stale = written.Except(carrying).OrderBy(pair => pair, StringComparer.Ordinal).ToList();

            Assert.AreEqual(0, unwritten.Count, $"tags carrying augments that the table does not name:\n  {string.Join("\n  ", unwritten)}");
            Assert.AreEqual(0, stale.Count, $"the table names tags that carry nothing:\n  {string.Join("\n  ", stale)}");
        }

        /// <summary>The augments an ability would stop being offered if it dropped that one tag — the
        /// whole meaning of a tag being load-bearing, asked of the rule that actually seats them.</summary>
        private static List<string> Lost(AbilityProvider catalog, string abilityId, IReadOnlyCollection<string> tags, string dropped)
        {
            string[] without = [.. tags.Where(tag => !string.Equals(tag, dropped, StringComparison.OrdinalIgnoreCase))];
            return [.. Offered(catalog, abilityId, tags).Except(Offered(catalog, abilityId, without), StringComparer.Ordinal)];
        }

        /// <summary>Every tag-judged augment the fitting rule lets onto an ability carrying those tags.
        /// Records naming an ability or claiming the whole book are left out: neither of them ever asks
        /// about a tag, so neither can be lost by dropping one.</summary>
        private static IEnumerable<string> Offered(AbilityProvider catalog, string abilityId, IReadOnlyCollection<string> tags) =>
            catalog.KnownAugmentIds
                .Select(id => (Id: id, Record: catalog.Find(id)!))
                .Where(entry => TagJudged(entry.Record))
                .Where(entry => AugmentFit.Check(
                    new AbilitySocketPlacement(string.Empty, abilityId, DeepestSocket), tags, entry.Record, []) == AugmentFitResult.Fits)
                .Select(entry => entry.Id);

        private static bool TagJudged(AbilityUpgradeData record) =>
            !record.FitsAnyAbility && string.IsNullOrWhiteSpace(record.AbilityId);

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
