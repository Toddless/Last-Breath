namespace LastBreathTest.Augment
{
    using Ability;
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Effects;
    using Localization;
    using Microsoft.Extensions.DependencyInjection;
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
    /// <para>An augment names the effect it lays in its own record OR in the factory that builds it. Both
    /// answers reach the card through one rule, so which of the two a record uses is not a thing the
    /// player can tell — and the walks here do not sort the catalog by it.</para>
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

        /// <summary>A record whose effect is named in the CODE that builds it: a typed factory, so its own
        /// declaration says nothing and the catalog alone cannot tell what its figures are balanced as.</summary>
        private const string CodeNamedRecord = "Augment_Berserk_Fury_Healing";

        private const string CodeNamedEffect = "Effect_Healing_Fury";

        [TestMethod]
        public void EveryRecordInTheCatalogPrintsNumbersWhereItsLineNamesThem()
        {
            // Every copy every record can produce, against the wording the game ships. A pool record is
            // walked member by member: which effect a copy lays is a draw, so a line that only works for
            // the member the seed happened to pick is a line broken for everybody else.
            // The whole catalog and not the behaviour half alone: a record laying an effect its FACTORY
            // names looks, from the data, exactly like a record laying nothing — which is how four
            // legendaries went on printing "{healAmount:%}" while the behaviour walk stayed green.
            (AbilityProvider abilities, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();
            IAugmentItemMinter minter = Minter(catalog, effects, abilities);

            List<string> unfilled = [];
            foreach (AbilityAugmentData record in catalog.All)
                foreach (string text in Cards(minter, catalog, record))
                    if (text.Contains('{')) unfilled.Add($"{record.Id}: {text}");

            Assert.IsTrue(catalog.All.Count > 0, "the catalog declares no augment, so this walk proves nothing");
            Assert.AreEqual(0, unfilled.Count,
                $"augment cards printing a placeholder instead of a number:\n  {string.Join("\n  ", unfilled)}");
        }

        [TestMethod]
        public void ACopyReadsTheSameInTheBagAsItDoesSeatedOnTheAbility()
        {
            // Two assemblies of one card: the copy lying in the bag builds its own, and the upgrade the
            // copy installs carries one built by the ability registry. The tray, the picker and the socket
            // cell read the first; the seated augment reads the second. They are one card, so a road that
            // knows something the other does not — which effect a factory lays, say — is a card that
            // changes wording the moment the player seats it.
            (AbilityProvider abilities, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();
            IAugmentItemMinter minter = Minter(catalog, effects, abilities);

            List<string> disagreed = [];
            foreach (AbilityAugmentData record in catalog.All)
            {
                IAugmentItem? carried = minter.Mint(record.Id);
                Assert.IsNotNull(carried, $"the minter builds no copy of '{record.Id}'");

                IAugment? seated = abilities.CreateUpgrade(carried.Augment);
                Assert.IsNotNull(seated, $"the registry builds no upgrade out of a copy of '{record.Id}'");

                string inSocket = Localization.RenderDescription(record.Id, seated.DescriptionValues);
                if (!string.Equals(carried.Description, inSocket, StringComparison.Ordinal))
                    disagreed.Add($"{record.Id}\n     bag:    {carried.Description}\n     socket: {inSocket}");
            }

            Assert.AreEqual(0, disagreed.Count,
                $"augments reading differently in the bag and in the slot:\n  {string.Join("\n  ", disagreed)}");
        }

        [TestMethod]
        public void TheCanonMovesACardWhoseEffectIsNamedInCode()
        {
            // The claim the walks above cannot make about the code-named half: that its figures are the
            // CANON's rather than something the factory carries. Same doctoring as the behaviour half —
            // the catalog is rebalanced in memory, the file on disk is only read — and the card follows.
            // It is also what a wrongly declared effect trips over: another effect's canon does not carry
            // this line's keys, so the card comes back with its braces instead of a different number.
            (AbilityProvider abilities, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();

            string shipped = Card(catalog, effects, CodeNamedRecord, abilities);
            StringAssert.Contains(shipped, "15%", $"'{CodeNamedRecord}' does not print the share the canon balances '{CodeNamedEffect}' at");

            string rebalanced = Card(catalog, Rebalanced(CodeNamedEffect, "healAmount", 0.55f), CodeNamedRecord, abilities);
            StringAssert.Contains(rebalanced, "55%", "the canon moved and the card did not follow it");

            Assert.AreEqual(shipped, Card(catalog, effects, CodeNamedRecord, abilities),
                "the shipped canon stopped answering what it did before");
        }

        [TestMethod]
        public void TheComposedMintReachesTheRegistryThatNamesACodeNamedEffect()
        {
            // The registrations rather than the classes. What hands the bag's mint the registry naming an
            // augment's effect is one line in the container, and its absence fails no build and throws
            // nothing — it prints a template on a legendary. Composed as a project composes it, plus the
            // one service outside these two extensions that a draw needs.
            ServiceProvider container = new ServiceCollection()
                .AddSharedGameDataParticipants()
                .AddBattleSystemModuleDependencies()
                .AddSingleton<IRandomNumberGenerator>(new DefaultRandomNumberGenerator(7))
                .BuildServiceProvider();
            LoadShippedInto(container);
            UseShippedWording();

            IAugmentItem? carried = container.GetRequiredService<IAugmentItemMinter>().Mint(CodeNamedRecord);

            Assert.IsNotNull(carried, "the composed mint builds no copy of a record whose effect is named in code");
            Assert.IsFalse(carried.Description.Contains('{'),
                $"the composed mint printed a template instead of the canon: {carried.Description}");
        }

        [TestMethod]
        public void WithoutTheRegistryThatNamesItACodeNamedCardKeepsItsPlaceholders()
        {
            // A composition holding the records and the canon but not the module whose factories name the
            // effects — the bag alone. Nothing there can say what such a record lays, so the line says what
            // it said before the declarations existed: a visible placeholder, and not a failed mint.
            (_, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();

            StringAssert.Contains(Card(catalog, effects, CodeNamedRecord), "{",
                "a card with nothing to name its effect invented numbers instead of showing the template");
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
        public void TheCritProtectionCardsPrintTheShareTheirOwnEffectIsBalancedAt()
        {
            // #199, from the side the player saw it: both crit-protection augments laid the shared
            // defence buff, so cards promising to cut critical damage printed that buff's 15%. The walk
            // above only asks that a number appears; this one asks that it is the RIGHT number, and that
            // each card reads its own effect's canon rather than a neighbour's.
            (_, AbilityAugmentCatalog catalog, EffectProvider effects) = ShippedAbilityData.Composed();
            UseShippedWording();

            string porcupine = Card(catalog, effects, "Augment_Porcupine_Crit_Mitigation");
            string iceAegis = Card(catalog, effects, "Augment_Ice_Aegis_Crit_Mitigation_Under_Shield");

            StringAssert.Contains(porcupine, "80%", "the Porcupine's crit protection does not print the 80% the design list gives it");
            StringAssert.Contains(iceAegis, "50%", "the Ice Aegis' crit protection does not print the 50% its canon carries");
            Assert.IsFalse(porcupine.Contains('{'), $"the Porcupine's card printed a template: {porcupine}");
            Assert.IsFalse(iceAegis.Contains('{'), $"the Ice Aegis' card printed a template: {iceAegis}");

            // The control. One class now builds all three crit-mitigation rows, so the general defence
            // buff a pool draws has to go on printing its own 15% and not a crit protection's figure.
            StringAssert.Contains(Card(catalog, effects, "Augment_Apply_Enhanced_Defence"), "15%",
                "the shared defence buff's card moved when the crit protections were given figures of their own");

            // Two effects and not one: rebalancing the Porcupine's must leave the Ice Aegis' where it was.
            string moved = Card(catalog, Rebalanced("Effect_Crit_Mitigation", "value", 0.35f), "Augment_Porcupine_Crit_Mitigation");
            StringAssert.Contains(moved, "35%", "the canon moved and the Porcupine's card did not follow it");
            Assert.AreEqual(iceAegis, Card(catalog, effects, "Augment_Ice_Aegis_Crit_Mitigation_Under_Shield"),
                "the two crit protections share a figure, so neither can be balanced without the other");
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
        public void TheEffectARecordNamesItselfBeatsWhatAFactoryWouldNameForIt()
        {
            // The order of the two words about what an augment lays. It matters for the pool records above
            // all: a COPY carries its drawn effect in exactly the field a record states its own in, so a
            // declaration winning here would print one member of the pool on every copy of the record.
            var effects = new Mock<IEffectProvider>();
            effects.Setup(provider => provider.CanonOf("Effect_Stated")).Returns(new Dictionary<string, float> { ["value"] = 0.15f });
            effects.Setup(provider => provider.CanonOf("Effect_Declared")).Returns(new Dictionary<string, float> { ["value"] = 0.9f });
            var declared = new Mock<IAugmentLaidEffects>();
            declared.Setup(source => source.LaidEffectOf("Augment_Probe")).Returns("Effect_Declared");
            var record = new AbilityAugmentData { Id = "Augment_Probe", EffectId = "Effect_Stated" };

            Dictionary<string, object?> values = AugmentDescription.Values(record, effects.Object, declared.Object);

            Assert.AreEqual(0.15f, values["value"], "a declaration overruled the effect the record names itself");
            Assert.AreEqual(new LocalizedId("Effect_Stated"), values[AbilityAugmentData.EffectPlaceholder],
                "the card names an effect the record does not lay");
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

        /// <summary>The shipped data into a composed container, through the loader the game runs. The
        /// participants are named one by one rather than taken whole: the container holds several a
        /// project fills in, and a card's mint needs neither of them.</summary>
        private static void LoadShippedInto(ServiceProvider container)
        {
            var service = new GameDataService(
                new FileSystemDataSource(SharedData.Root()),
                [container.GetRequiredService<AbilityAugmentCatalog>(), container.GetRequiredService<EffectCanonCatalog>()]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        /// <summary>The mint the game composes: the records, the canon behind them, and whoever answers
        /// for the records whose effect is named in code. The last is optional there and here.</summary>
        private static IAugmentItemMinter Minter(
            IAbilityAugmentCatalog catalog, IEffectProvider effects, IAugmentLaidEffects? laid = null) =>
            new AugmentItemMinter(
                catalog, new AugmentMinter(catalog, new DefaultRandomNumberGenerator(7)), () => effects, () => laid);

        private static AbilityAugmentData Record(IAbilityAugmentCatalog catalog, string augmentId)
        {
            AbilityAugmentData? record = catalog.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }

        private static string Card(
            IAbilityAugmentCatalog catalog, IEffectProvider effects, string augmentId, IAugmentLaidEffects? laid = null)
        {
            IAugmentItem? item = Minter(catalog, effects, laid).Mint(augmentId);
            Assert.IsNotNull(item, $"the minter builds no copy of '{augmentId}'");
            return item.Description;
        }

        /// <summary>The shipped canon with one figure moved — read off the file and patched in memory, so
        /// nothing on disk is touched and the untouched registry beside it still says what it said.</summary>
        private static EffectProvider Rebalanced(string effectId, string key, float figure)
        {
            List<string> files = [];
            foreach (string file in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Effects), "*.json", SearchOption.AllDirectories))
            {
                JObject root = JObject.Parse(File.ReadAllText(file));
                foreach (JObject entry in (root["effects"] as JArray ?? []).OfType<JObject>())
                    if ((string?)entry["id"] == effectId && entry["properties"] is JObject properties)
                        properties[key] = figure;

                files.Add(root.ToString());
            }

            return EffectProviders.FromJson([.. files]);
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
