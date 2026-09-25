namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A fallback written in code must name the number its record names. Data wins on both roads —
    /// <c>RegisterDefault</c> yields to the key the base already registered from <c>abilityProperties</c>,
    /// and <c>GetValueOrDefault</c> never looks at its second argument while the record carries the key —
    /// so a fallback that drifted away from its record steers nothing at all and is still read by
    /// everybody: the reviewer weighing what an ability is worth, the author copying the figure into a
    /// new record, and the next hand to move a <c>RegisterDefault</c> above its <c>base</c> call, which
    /// turns the dead lie into the live number. Balance is decided in the files, and the code is not
    /// allowed to contradict them even where the contradiction costs nothing today.
    /// </summary>
    [TestClass]
    public class AbilityDefaultDriftTests
    {
        private const float Tolerance = 0.0001f;

        /// <summary>
        /// Record keys whose code side is a NEUTRAL BASE and not a copy of the shipped figure: zero as
        /// the honest entry point an augment moves off, where an ability built without the key must grant
        /// nothing rather than a figure nobody wrote. A legitimate entry needs its reason written here.
        /// <para>Critical Calculation's crit chance is the one: what a stack of the buff is worth is the
        /// record's to name, and an ability built with the key missing hands out no chance at all instead
        /// of a made-up one.</para>
        /// </summary>
        private static readonly (string AbilityId, string Key)[] s_neutralBases =
        [
            ("Ability_Critical_Calculation", "criticalChance")
        ];

        /// <summary>One entry of an augment registry — the record every fallback below the line belongs
        /// to, until the next entry opens. Both registries are keyed the same way.</summary>
        private static readonly Regex s_registryEntry = new(@"^\s*\[""(?<id>Augment_\w+)""\]", RegexOptions.Compiled);

        /// <summary>
        /// Where an augment says what it is worth when its record is silent on the property. There are
        /// two such places and they write it differently — the factory registry hands the figure to
        /// <c>GetValueOrDefault</c>, the numeric table writes it as the fourth field of the move
        /// (<see cref="Battle.Source.Abilities.AugmentParameterMove.WithoutTheProperty"/>) — but they
        /// answer the same question, so a walk that reads one of them leaves the larger half unwatched.
        /// </summary>
        private static readonly Registry[] s_registries =
        [
            new("AbilityProvider.Upgrades.cs",
                new Regex(@"GetValueOrDefault\(""(?<property>\w+)"",\s*(?<value>-?\d+(?:\.\d+)?)f?\)", RegexOptions.Compiled),
                new Regex(@"GetValueOrDefault\(", RegexOptions.Compiled)),
            new("AbilityProvider.ParameterAugments.cs",
                new Regex(@"OperationType\.\w+,\s*""(?<property>\w+)"",\s*(?<value>-?\d+(?:\.\d+)?)f?\)", RegexOptions.Compiled),
                new Regex(@"OperationType\.", RegexOptions.Compiled))
        ];

        [TestMethod]
        public void EveryAbilityFallbackIsTheNumberItsRecordShips()
        {
            List<string> drifts = DriftsBetween(Bare(), ShippedAbilityData.Abilities());

            Assert.AreEqual(0, drifts.Count,
                "an ability falls back to a number its record disagrees with — the data steers and the code lies:\n  "
                + string.Join("\n  ", drifts));
        }

        [TestMethod]
        public void TheWalkWouldSeeADriftedFallbackIfThereWereOne()
        {
            // The walk above is only worth its green if it can go red. A catalog saying nothing agrees
            // with every fallback by construction; one shipped figure is then moved in a copy of it,
            // which is exactly the shape a drifted fallback has.
            const string AbilityId = "Ability_Armageddon";
            const string Key = "stage3HpCost";
            const float Probe = 0.99f;

            Assert.AreEqual(0, DriftsBetween(Bare(), Bare()).Count,
                "a catalog carrying no properties disagreed with the fallbacks, which are the only voice left in it");

            List<string> drifts = DriftsBetween(Bare(), ShippedAbilityData.AbilitiesOver(WithProperty(AbilityId, Key, Probe)));

            Assert.IsTrue(drifts.Any(drift => drift.StartsWith($"{AbilityId}.{Key}", StringComparison.Ordinal)),
                $"the walk read a record shipping {Probe} for {AbilityId}.{Key} beside a fallback that says otherwise "
                + "and reported nothing — it cannot tell a drifted fallback from an aligned one");
        }

        [TestMethod]
        public void EveryAugmentFallbackIsTheBaseRungItsRecordShips()
        {
            IAbilityAugmentCatalog records = ShippedAbilityData.Augments();
            List<string> drifts = [];

            foreach (Registry registry in s_registries)
            {
                (List<(string AugmentId, string Property, float Fallback)> read, int written) = FallbacksIn(registry);

                // The reading is a regex over source, so it can shrink without failing: a call the next
                // hand splits across two lines simply stops matching and the walk goes green over
                // numbers it never saw. Counting what the file WRITES beside what was read closes that.
                Assert.IsTrue(written > 0, $"{registry.File}: nothing in it writes a fallback any more — this walk reads a file it no longer understands");
                Assert.AreEqual(written, read.Count,
                    $"{registry.File}: {written} places write a fallback and only {read.Count} were read — "
                    + "the rest are skipped in silence, and this walk is green about numbers it never looked at");

                foreach ((string augmentId, string property, float fallback) in read)
                {
                    AbilityAugmentData? record = records.Find(augmentId);
                    Assert.IsNotNull(record, $"{registry.File} declares '{augmentId}', which no shipped record does");

                    // A key the record never names is a figure living only in code: there is nothing for
                    // it to be equal to, which is a different finding and not this walk's business.
                    if (!record.UpgradeProperties.TryGetValue(property, out float shipped)) continue;
                    if (Math.Abs(fallback - shipped) <= Tolerance) continue;

                    // A copy is minted at a rung of the record's own ladder and carries a number for every
                    // property the record declares, so the fallback only ever stands in for a record SILENT
                    // on the key — and what a silent record would be worth is its base rung.
                    drifts.Add($"{augmentId}.{property}: the code falls back to {fallback}, the record's base rung is {shipped}");
                }
            }

            Assert.AreEqual(0, drifts.Count,
                "an augment registry falls back to a number its record disagrees with:\n  " + string.Join("\n  ", drifts));
        }

        /// <summary>The abilities over a catalog with every property emptied — what each one falls back to
        /// with nothing the data added.</summary>
        private static AbilityProvider Bare() => ShippedAbilityData.AbilitiesOver(ShippedAbilityData.WithoutProperties());

        /// <summary>Every key both roads speak about, and the two numbers they speak.</summary>
        private static List<string> DriftsBetween(AbilityProvider bare, AbilityProvider shipped)
        {
            List<string> drifts = [];

            foreach ((string abilityId, List<string> keys) in ShippedAbilityData.ShippedProperties())
            {
                IAbility fallbacks = bare.CreateAbility(abilityId);
                IAbility asShipped = shipped.CreateAbility(abilityId);

                foreach (string key in keys)
                {
                    string parameter = ShippedAbilityData.ParameterKey(key);

                    // Spoken on one road only: the record is the sole voice on the key and has nothing to
                    // disagree with (the reverse — a record key nobody reads — is AbilityDataKeyTests).
                    if (!fallbacks.Declares(parameter) || !asShipped.Declares(parameter)) continue;
                    if (s_neutralBases.Contains((abilityId, key))) continue;
                    if (Math.Abs(fallbacks[parameter] - asShipped[parameter]) <= Tolerance) continue;

                    drifts.Add($"{abilityId}.{key}: the code falls back to {fallbacks[parameter]}, "
                               + $"the record ships {asShipped[parameter]}");
                }
            }

            return drifts;
        }

        /// <summary>The shipped catalog with one ability carrying one figure — a record for the walk to
        /// disagree with. Lives in memory; nothing on disk moves.</summary>
        private static string WithProperty(string abilityId, string key, float value)
        {
            JObject root = JObject.Parse(ShippedAbilityData.WithoutProperties());
            JObject? entry = (root["abilities"] as JArray ?? []).OfType<JObject>()
                .FirstOrDefault(candidate => (string?)candidate["id"] == abilityId);

            Assert.IsNotNull(entry, $"the shipped data declares no '{abilityId}'");
            entry["abilityProperties"] = new JObject { [key] = value };
            return root.ToString();
        }

        /// <summary>Every fallback one registry carries, by the record it belongs to, and how many places
        /// in that file write one at all. Read out of SOURCE because the number is an argument of a
        /// registration and nothing of it survives compiling — the same reason the effect value shapes
        /// are swept this way.</summary>
        private static (List<(string AugmentId, string Property, float Fallback)> Read, int Written) FallbacksIn(Registry registry)
        {
            List<(string AugmentId, string Property, float Fallback)> read = [];
            string augmentId = string.Empty;
            int written = 0;

            foreach (string line in File.ReadAllLines(RegistrySource(registry.File)))
            {
                Match entry = s_registryEntry.Match(line);
                if (entry.Success) augmentId = entry.Groups["id"].Value;
                written += registry.Written.Matches(line).Count;
                if (augmentId.Length == 0) continue;

                foreach (Match fallback in registry.Fallback.Matches(line))
                    read.Add((
                        augmentId,
                        fallback.Groups["property"].Value,
                        float.Parse(fallback.Groups["value"].Value, CultureInfo.InvariantCulture)));
            }

            return (read, written);
        }

        /// <summary>One augment registry in the working tree.</summary>
        private static string RegistrySource(string file)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Battle", "Source", "Abilities", file);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"'{file}' was not found above {AppContext.BaseDirectory}");
        }

        /// <summary>One place augment fallbacks are written: the file, the shape a fallback takes in it,
        /// and the shape of the registration it lives in — the second one so a fallback the first stops
        /// matching is still counted, and the silence is caught.</summary>
        private sealed record Registry(string File, Regex Fallback, Regex Written);
    }
}
