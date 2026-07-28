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
        [TestMethod]
        public void EveryAbility_CarriesKnownTags()
        {
            string json = File.ReadAllText(Path.Combine(FindSharedData(), "Abilities", "BaseAbilityData.json"));
            var root = JsonConvert.DeserializeObject<AbilityDataRoot>(json)!;

            Assert.IsTrue(root.Abilities.Count > 0, "BaseAbilityData.json produced no entries");
            foreach (var ability in root.Abilities)
            {
                Assert.IsTrue(ability.Tags.Length > 0, $"'{ability.Id}' has no tags — no tag-bound augment can ever fit it");
                foreach (string tag in ability.Tags)
                    Assert.IsTrue(AbilityTags.All.Contains(tag), $"'{ability.Id}' carries unknown tag '{tag}'");
            }
        }

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
