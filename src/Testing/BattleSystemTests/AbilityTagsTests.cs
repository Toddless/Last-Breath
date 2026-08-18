namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Newtonsoft.Json;

    /// <summary>
    /// Audits the REAL BaseAbilityData.json against the combat-tag vocabulary: tags are the augment
    /// compatibility axis, so an untagged ability can never receive a tag-bound augment and a typo
    /// is a silently incompatible one.
    /// </summary>
    [TestClass]
    public class AbilityTagsTests
    {
        /// <summary>
        /// The three axes that were tags until the generalisation of 2026-08-18. Duration, stacks and
        /// effectiveness are properties of what a cast LAYS, so on the cast itself they carried no
        /// meaning of their own and only widened fitting: "poison lasts longer" reached every ability
        /// that happened to say the word "duration". The genus does the seating now and the record's
        /// own properties decide what moves. Held as literal strings rather than as constants, because
        /// the claim is about the DATA and the constants are exactly what was deleted.
        /// </summary>
        private static readonly string[] s_retiredAxes = ["duration", "stacks", "effectiveness"];

        /// <summary>
        /// Abilities that wear a genus and lay nothing of that genus themselves — the sanctioned
        /// exceptions to the umbrella rule, each one the owner's canon rather than an oversight
        /// (decision 17 of 2026-08-18, and the tag lines of the stance docs).
        /// <para>The explosion DETONATES poison somebody else laid; the sacrifice and the overload arm
        /// the NEXT cast, which the owner counts as a bill rather than as content laid by this one.
        /// An entry here costs reach: an "applied" record does not reach the ability at all.</para>
        /// </summary>
        private static readonly string[] s_wearsAGenusAndLaysNothing =
            ["Ability_Poison_Explosion", "Ability_Sacrifice", "Ability_Overload"];

        [TestMethod]
        public void EveryAbility_CarriesKnownTags()
        {
            AbilityDataRoot root = Markup();

            Assert.IsTrue(root.Abilities.Count > 0, "BaseAbilityData.json produced no entries");
            foreach (var ability in root.Abilities)
            {
                Assert.IsTrue(ability.Tags.Length > 0, $"'{ability.Id}' has no tags — no tag-bound augment can ever fit it");
                foreach (string tag in ability.Tags)
                    Assert.IsTrue(AbilityTags.All.Contains(tag), $"'{ability.Id}' carries unknown tag '{tag}'");
            }
        }

        [TestMethod]
        public void AnAbilityWearingAGenusCarriesTheEffectUmbrella()
        {
            // What 'effect' means after the generalisation: the umbrella every applier wears, so the
            // generalised records ("duration of what this lays", "effectiveness of what this lays")
            // reach exactly the appliers and no one else. A genus without the umbrella is the silent
            // half of that — the ability lays a poison and the general poison records pass it by.
            AbilityDataRoot root = Markup();
            List<string> bare = [];

            foreach (var ability in root.Abilities)
            {
                if (s_wearsAGenusAndLaysNothing.Contains(ability.Id, StringComparer.Ordinal)) continue;

                string[] genera = [.. ability.Tags.Where(AbilityTags.EffectGenera.Contains)];
                if (genera.Length > 0 && !ability.Tags.Contains(AbilityTags.Effect, StringComparer.OrdinalIgnoreCase))
                    bare.Add($"'{ability.Id}' wears [{string.Join(", ", genera)}] and not '{AbilityTags.Effect}'");
            }

            Assert.AreEqual(0, bare.Count,
                "an ability names the genus it lays and not the umbrella, so every generalised record passes it by:\n  "
                + string.Join("\n  ", bare));
        }

        [TestMethod]
        public void EveryAbilityWrittenOffAsLayingNothingStillWearsAGenus()
        {
            // The other direction of the exception list. An ability that stopped wearing a genus needs
            // no exemption, and one left on the list is an exemption nobody would notice going stale.
            AbilityDataRoot root = Markup();

            foreach (string abilityId in s_wearsAGenusAndLaysNothing)
            {
                var ability = root.Abilities.Find(entry => string.Equals(entry.Id, abilityId, StringComparison.Ordinal));
                Assert.IsNotNull(ability, $"'{abilityId}' is written off as a genus-without-umbrella and the markup declares no such ability");
                Assert.IsTrue(ability.Tags.Any(AbilityTags.EffectGenera.Contains),
                    $"'{abilityId}' wears no genus any more — take it off the exception list and let the guard speak for it");
            }
        }

        [TestMethod]
        public void EveryRecordGrantingAGenusGrantsTheEffectUmbrella()
        {
            // The donor's half of the same rule. A record that teaches an ability poison teaches it
            // "lays something" as well — otherwise the generalised records would seat on the abilities
            // that lay natively and never on the ones an augment TAUGHT to lay, which is the whole road
            // the grant exists to open.
            AbilityDataRoot root = Markup();
            List<string> bare = [];

            foreach (var record in root.Augments)
            {
                string[] genera = [.. record.GrantableTags.Where(AbilityTags.EffectGenera.Contains)];
                if (genera.Length > 0 && !record.GrantableTags.Contains(AbilityTags.Effect, StringComparer.OrdinalIgnoreCase))
                    bare.Add($"'{record.Id}' grants [{string.Join(", ", genera)}] and not '{AbilityTags.Effect}'");
            }

            Assert.AreEqual(0, bare.Count,
                "an applier grants the genus it lays and not the umbrella, so the ability it taught stays out of the generalised records:\n  "
                + string.Join("\n  ", bare));
        }

        [TestMethod]
        public void TheRetiredAxesAreGoneFromTheVocabularyAndFromTheMarkupForGood()
        {
            AbilityDataRoot root = Markup();

            foreach (string axis in s_retiredAxes)
                Assert.IsFalse(AbilityTags.All.Contains(axis), $"'{axis}' is back in the vocabulary — it is a property of what is laid, not a tag");

            List<string> found =
            [
                .. root.Abilities.SelectMany(ability => Retired(ability.Id, "tags", ability.Tags)),
                .. root.Augments.SelectMany(record => Retired(record.Id, "tags", record.Tags)),
                .. root.Augments.SelectMany(record => Retired(record.Id, "grantsTags", record.GrantsTags)),
                .. root.Augments.SelectMany(record => record.EffectPool
                    .SelectMany(member => Retired($"{record.Id}/{member.Key}", "effectPool", member.Value)))
            ];

            Assert.AreEqual(0, found.Count,
                "a retired axis is back in the markup — it seats records by a word instead of by a genus:\n  " + string.Join("\n  ", found));
        }

        private static IEnumerable<string> Retired(string owner, string field, IEnumerable<string> tags) =>
            tags.Where(tag => s_retiredAxes.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .Select(tag => $"'{owner}' carries '{tag}' in {field}");

        private static AbilityDataRoot Markup() =>
            JsonConvert.DeserializeObject<AbilityDataRoot>(
                File.ReadAllText(Path.Combine(FindSharedData(), "Abilities", "BaseAbilityData.json")))!;

        [TestMethod]
        public void SharesAny_MatchesOnOneCommonTag_CaseInsensitive()
        {
            Assert.IsTrue(AbilityTags.SharesAny(["poison", "buff"], ["Poison"]));
            Assert.IsFalse(AbilityTags.SharesAny(["poison"], ["cold", "spell"]));
            Assert.IsFalse(AbilityTags.SharesAny([], ["poison"]));
        }

        private static string FindSharedData()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "SharedData");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("SharedData was not found above the test host directory");
        }
    }
}
