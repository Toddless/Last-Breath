namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// What an augment's card says. A record that lays an effect states no figures of its own — they are
    /// balanced once, in the effect canon — so every number on its line has to come from there. Where it
    /// did not, the card printed the template: "-{reduceBy:%} incoming healing", raw braces and all, on a
    /// legendary the player had just paid for.
    /// <para>The walks below are about the SHIPPED wording against the SHIPPED canon, because that pairing
    /// is what a player reads and neither half can be checked alone: a line naming a figure nobody supplies
    /// and a canon nobody prints both look healthy from their own side.</para>
    /// </summary>
    [TestClass]
    public class AugmentDescriptionTests
    {
        private const string ArmorRecord = "Augment_Attacks_Reduce_Armor";
        private const string ArmorEffect = "Effect_Armor_Reduction";
        private const string HealRecord = "Augment_Attacks_Reduce_Incoming_Heal";

        /// <summary>A record moving a number of its own and laying no effect — the half of the catalog
        /// that always printed correctly and must go on doing so.</summary>
        private const string PlainRecord = "Augment_Additional_Attacks";

        [TestMethod]
        public void EveryBehaviourRecordPrintsNumbersWhereItsLineNamesThem()
        {
            // Every copy a behaviour record can produce, against the wording the game ships. A pool record
            // is walked member by member: which effect a copy lays is a draw, so a line that only works for
            // the member the seed happened to pick is a line broken for everybody else.
            (_, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();
            var minter = new AugmentItemMinter(catalog, new AugmentMinter(catalog, new DefaultRandomNumberGenerator(7)), () => effects);

            List<string> unfilled = [];
            foreach (AbilityAugmentData record in Behaviours(catalog))
                foreach (string text in Cards(minter, catalog, record))
                    if (text.Contains('{')) unfilled.Add($"{record.Id}: {text}");

            Assert.IsTrue(Behaviours(catalog).Count > 0, "no record declares a behaviour, so this walk proves nothing");
            Assert.AreEqual(0, unfilled.Count,
                $"augment cards printing a placeholder instead of a number:\n  {string.Join("\n  ", unfilled)}");
        }

        [TestMethod]
        public void TheNumbersOnACardAreTheCanonsAndMoveWithIt()
        {
            // The claim the walk above cannot make: that the figures shown are the CANON's rather than
            // something a record still carries. The catalog is rebalanced in memory — the file on disk is
            // read, never written — and the card has to follow it.
            (_, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();

            string shipped = Card(catalog, effects, ArmorRecord);
            StringAssert.Contains(shipped, "15%", $"'{ArmorRecord}' does not print the share the canon balances '{ArmorEffect}' at");

            string rebalanced = Card(catalog, Rebalanced(ArmorEffect, "reduceBy", 0.55f), ArmorRecord);
            StringAssert.Contains(rebalanced, "55%", "the canon moved and the card did not follow it");

            // The rollback: the untouched registry still answers what the file says, so the move above was
            // the doctored canon and not a figure cached somewhere between the two.
            Assert.AreEqual(shipped, Card(catalog, effects, ArmorRecord), "the shipped canon stopped answering what it did before");
        }

        [TestMethod]
        public void ACopysOwnNumberBeatsTheCanonicalOne()
        {
            // The order the two sources are merged in. A record laying an effect is forbidden to restate
            // its figures, so today only a copy's own levers land here — but the day one carries a number
            // beside the canon, the number the player was sold is the one he must read.
            var effects = Canon("Effect_Probe", new Dictionary<string, float> { ["duration"] = 3f, ["value"] = 0.15f });
            var record = new AbilityAugmentData
            {
                Id = "Augment_Probe",
                EffectId = "Effect_Probe",
                UpgradeProperties = new Dictionary<string, float> { ["value"] = 0.42f },
            };

            Dictionary<string, object?> values = AugmentDescription.Values(record, effects);

            Assert.AreEqual(0.42f, values["value"], "the canon overwrote the figure the record states itself");
            Assert.AreEqual(3f, values["duration"], "the key nobody spoke about did not come from the canon");

            // And through a copy, which is what the bag and the socket actually print.
            var copy = new AugmentInstance("Augment_Probe", new Dictionary<string, float> { ["value"] = 0.9f }, Rarity.Rare);
            Dictionary<string, object?> printed = AugmentDescription.Values(copy.Applied(record), effects);

            Assert.AreEqual(0.9f, printed["value"], "the copy's own roll lost to the record and the canon behind it");
        }

        [TestMethod]
        public void ARecordLayingNoEffectIsPrintedFromItselfAlone()
        {
            // The simple half of the catalog, unchanged: its number is its own, there is no effect to name,
            // and nothing about an effect may appear in what it prints.
            (_, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();

            AbilityAugmentData record = Record(catalog, PlainRecord);
            Dictionary<string, object?> values = AugmentDescription.Values(record, effects);

            CollectionAssert.AreEquivalent(
                record.UpgradeProperties.Keys.ToList(),
                values.Keys.ToList(),
                $"'{PlainRecord}' is printed with values it does not declare");
            Assert.IsFalse(Card(catalog, effects, PlainRecord).Contains('{'), "a plain record stopped printing its own number");
        }

        [TestMethod]
        public void WithoutAnEffectRegistryTheLineKeepsItsPlaceholdersInsteadOfFailing()
        {
            // A composition with no canon — a sandbox, a tool — must still mint, carry and show augments.
            // The line then says what it said before the canon existed, which is a visible placeholder.
            (_, AbilityAugmentCatalog catalog, _) = ShippedAbilityData.Composed();
            UseShippedWording();
            var minter = new AugmentItemMinter(catalog, new AugmentMinter(catalog, new DefaultRandomNumberGenerator(7)));

            IAugmentItem? item = minter.Mint(HealRecord);

            Assert.IsNotNull(item, "a composition without an effect registry could not mint an augment at all");
            StringAssert.Contains(item.Description, "{", "a card with no canon behind it invented numbers instead of showing the template");
        }

        /// <summary>Every card one record can produce: one per effect it may lay, or a single one for a
        /// record that lays none.</summary>
        private static IEnumerable<string> Cards(IAugmentItemMinter minter, IAbilityAugmentCatalog catalog, AbilityAugmentData record)
        {
            IAugmentItem? drawn = minter.Mint(record.Id);
            Assert.IsNotNull(drawn, $"the minter builds no copy of '{record.Id}'");

            if (record.PoolEffects.Count == 0)
            {
                yield return drawn.Description;
                yield break;
            }

            foreach (string effectId in record.PoolEffects)
            {
                IAugmentItem? member = minter.Restore(new AugmentInstance(record.Id, drawn.Augment.Values, drawn.Rarity, effectId));
                Assert.IsNotNull(member, $"the minter could not carry back a copy of '{record.Id}' laying '{effectId}'");
                yield return member.Description;
            }
        }

        private static List<AbilityAugmentData> Behaviours(IAbilityAugmentCatalog catalog) =>
            [.. catalog.All.Where(record => !string.IsNullOrWhiteSpace(record.Behaviour))];

        private static AbilityAugmentData Record(IAbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }

        private static string Card(IAbilityAugmentCatalog catalog, IEffectProvider effects, string augmentId)
        {
            IAugmentItem? item = new AugmentItemMinter(
                catalog, new AugmentMinter(catalog, new DefaultRandomNumberGenerator(7)), () => effects).Mint(augmentId);
            Assert.IsNotNull(item, $"the minter builds no copy of '{augmentId}'");
            return item.Description;
        }

        /// <summary>The shipped canon with one figure moved — read off the file and patched in memory, so
        /// nothing on disk is touched and the untouched registry beside it still says what it said.</summary>
        private static EffectProvider Rebalanced(string effectId, string key, float figure)
        {
            var provider = new EffectProvider();
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json", SearchOption.AllDirectories))
            {
                JObject root = JObject.Parse(File.ReadAllText(file));
                foreach (JObject entry in (root["effects"] as JArray ?? []).OfType<JObject>())
                    if ((string?)entry["id"] == effectId && entry["properties"] is JObject properties)
                        properties[key] = figure;

                provider.Apply(DataCatalog.Effects, new GameDataFile(Path.GetFileName(file), root.ToString()));
            }

            return provider;
        }

        /// <summary>A registry that knows one effect and its figures — for the merge rule, which is about
        /// order and not about any shipped record.</summary>
        private static IEffectProvider Canon(string effectId, Dictionary<string, float> figures)
        {
            var effects = new Mock<IEffectProvider>();
            effects.Setup(provider => provider.CanonOf(effectId)).Returns(figures);
            return effects.Object;
        }

        /// <summary>The wording the game ships, put behind the static facade the cards render through.</summary>
        private static void UseShippedWording()
        {
            var provider = new FakeLocalizationProvider();
            foreach ((string key, string text) in ShippedEntries()) provider.Strings[key] = text;

            Localization.Override(new LocalizationService(
                provider, new ModifierFormatter(provider, new ParameterFormatProvider()), new ContextModifierFormatter(provider), []));
        }

        /// <summary>en.po as key → wording. Single-line entries, which is how the catalog is written; a
        /// continued one simply comes back with its first line, which still tells a worded key from an
        /// empty one.</summary>
        private static Dictionary<string, string> ShippedEntries()
        {
            string[] lines = File.ReadAllLines(Path.Combine(SharedData.Root(), "Localization", "en.po"));
            Dictionary<string, string> entries = new(StringComparer.Ordinal);

            for (int line = 0; line + 1 < lines.Length; line++)
            {
                if (!Quoted(lines[line], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[line + 1], "msgstr ", out string text)) entries[id] = text;
            }

            Assert.IsTrue(entries.Count > 0, "en.po was not read at all, so these walks prove nothing");
            return entries;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            if (!line.StartsWith(prefix + '"', StringComparison.Ordinal) || !line.EndsWith('"')) return false;

            value = line[(prefix.Length + 1)..^1];
            return true;
        }
    }
}
