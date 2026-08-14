namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Every number an ability's record carries has to be a number the ability actually reads. The base
    /// registers each <c>abilityProperties</c> key under its PascalCase name whether anybody asks for it
    /// or not, and an ability's own <c>RegisterDefault</c> yields to data — so a key renamed on one side
    /// only leaves the record still parsing, still registering, and quietly steering nothing. What is
    /// worse than the silence is the direction it fails in: the hard-coded fallback becomes the source of
    /// truth and the data file stops mattering, which is the project's data-first rule inverted.
    ///
    /// The orphan is found by building the same ability twice — once as shipped, once with its properties
    /// emptied. What the second one declares is what the ability asks for itself; anything the record adds
    /// beyond that is a key nobody reads.
    /// </summary>
    [TestClass]
    public class AbilityDataKeyTests
    {
        /// <summary>Record keys that are deliberately not read by the ability that carries them. Empty,
        /// and meant to stay so: a legitimate orphan needs a reason written next to it.</summary>
        private static readonly (string AbilityId, string Key)[] s_knowinglyUnread =
        [
            // Found by this walk when it was written, and older than it. The Crit Calculation buff
            // lengthens itself by ONE turn per critical hit, and the one is written into the effect
            // rather than read from here. Left standing rather than deleted: the figure looks like the
            // author's word for that step, and wiring it up would change what the ability does — the
            // owner's call, not a guess made while adding a guard.
            ("Ability_Critical_Calculation", "additionalDurationAmount"),
        ];

        [TestMethod]
        public void EveryNumberARecordCarriesIsANumberItsAbilityReads()
        {
            AbilityProvider bare = ShippedAbilityData.AbilitiesOver(WithoutProperties());
            List<string> orphans = [];

            foreach ((string abilityId, List<string> keys) in ShippedProperties())
            {
                IAbility stripped = bare.CreateAbility(abilityId);

                foreach (string key in keys)
                {
                    string parameter = ToParameterKey(key);
                    if (stripped.Declares(parameter)) continue;
                    if (s_knowinglyUnread.Contains((abilityId, key))) continue;

                    orphans.Add($"{abilityId}.{key} (registers '{parameter}', which the ability never asks for)");
                }
            }

            Assert.AreEqual(0, orphans.Count,
                "record numbers nothing reads — the data says one thing and the ability follows its own default:\n  "
                + string.Join("\n  ", orphans));
        }

        [TestMethod]
        public void TheWalkWouldSeeAnOrphanIfThereWereOne()
        {
            // The walk above is only worth its green if it can go red. A key nothing declares is added to
            // one shipped record and the same reading is asked for it.
            AbilityProvider bare = ShippedAbilityData.AbilitiesOver(WithoutProperties());
            IAbility stripped = bare.CreateAbility("Ability_Dark_Shroud");

            Assert.IsFalse(stripped.Declares(ToParameterKey("reviewProbeOrphan")),
                "the probe ability declares a key nobody wrote, so the walk cannot tell an orphan from a reader");
            Assert.IsTrue(stripped.Declares(ToParameterKey("healthRegeneration")),
                "the Dark Shroud stopped asking for its own regeneration key, so the walk above is vacuous");
        }

        /// <summary>json camelCase to the PascalCase parameter name, the way the base does it.</summary>
        private static string ToParameterKey(string key) => char.ToUpperInvariant(key[0]) + key[1..];

        /// <summary>Every shipped ability and the property keys its record carries.</summary>
        private static IEnumerable<(string AbilityId, List<string> Keys)> ShippedProperties()
        {
            foreach (JObject entry in Abilities())
            {
                List<string> keys = entry["abilityProperties"] is JObject properties
                    ? [.. properties.Properties().Select(property => property.Name)]
                    : [];
                if (keys.Count > 0) yield return ((string?)entry["id"] ?? string.Empty, keys);
            }
        }

        /// <summary>The shipped catalog with every ability's properties emptied — what each ability
        /// declares on its own, with nothing the data added.</summary>
        private static string WithoutProperties()
        {
            JObject root = Root();
            foreach (JObject entry in (root["abilities"] as JArray ?? []).OfType<JObject>())
                entry["abilityProperties"] = new JObject();

            return root.ToString();
        }

        private static IEnumerable<JObject> Abilities() => (Root()["abilities"] as JArray ?? []).OfType<JObject>();

        private static JObject Root()
        {
            string path = SharedData.Catalog(DataCatalog.Abilities);
            string[] files = [.. Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)];
            Assert.AreEqual(1, files.Length, "the ability catalog is no longer one file — this walk rewrites it whole");
            return JObject.Parse(File.ReadAllText(files[0]));
        }
    }
}
