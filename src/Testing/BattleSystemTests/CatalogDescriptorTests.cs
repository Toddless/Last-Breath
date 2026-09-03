namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai;
    using Core.Ai.World;
    using Core.Data.CraftingData;
    using Core.Data.EquipData;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Data.NpcData;
    using Core.Data.Schema;
    using Core.Enums;
    using Core.Localization;
    using LastBreath.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// The join between the game's data and the authoring tool: the descriptors say which catalogs the
    /// tool can be handed a schema for, and the schemas of the described ones are built here from the real
    /// DTOs against the real shipped files.
    /// <para>Nothing else joins the two. A DTO renamed, a reference left unmarked or a catalog added with
    /// no descriptor costs nothing at build time and shows up as an editor quietly offering a text box
    /// where the author expects a picker.</para>
    /// </summary>
    [TestClass]
    public class CatalogDescriptorTests
    {
        /// <summary>Separates the json names of a path walked down a record — <c>reactions.abilityId</c>
        /// is the reaction's ability, whatever lists and maps stand between the two.</summary>
        private const char PathSeparator = '.';

        /// <summary>Separates the steps of a json pointer, which is how the addresses of the keys a
        /// schema does not know are written.</summary>
        private const char PointerSeparator = '/';

        /// <summary>What the json name of a field holding an id ends with, whatever its case.</summary>
        private const string IdSuffix = "Id";

        /// <summary>The steps from a loot table down to the positions at it: a table holds tiers, a tier
        /// holds the seats. Neither is named by the descriptor — the DTOs are read for them — so the walk
        /// spells them out.</summary>
        private const string TiersField = "tiers";

        private const string ItemsField = "items";

        /// <summary>What a composite modifier line holds its parts under. No descriptor names it — it is
        /// the DTO's own field, the way a table's tiers are, so the walk spells it out.</summary>
        private const string PartsField = "parts";

        /// <summary>What a line handing out behaviour instead of a number is written under.</summary>
        private const string GrantField = "grant";

        /// <summary>What a modifier line claims its slot family under.</summary>
        private const string AffixField = "affix";

        /// <summary>What an augment group is written with.</summary>
        private const string TierField = "tier";

        private const string RarityField = "rarity";

        /// <summary>What an npc writes its starting parameter values under. No descriptor names it — it is
        /// the DTO's own field, the way a table's tiers are, so the walk spells it out.</summary>
        private const string BaseParametersField = "baseParameters";

        /// <summary>What the reflector may still have to say about the real NPC DTOs. Empty: everything
        /// the walk meets there is a plain property it can read whole. A note appearing here is either a
        /// DTO to fix or a fact to write down — never something to silence by widening the check.</summary>
        private static readonly string[] s_allowedNpcNotes = [];

        /// <summary>
        /// What the reflector has to say about the real loot table DTOs — every one of them a fact about
        /// the shipped types rather than something to be rid of: a position is a positional record and a
        /// group of augments is another, so neither states defaults; the positions of a tier are read
        /// through a converter, which is exactly why the descriptor states their shapes; and whether a
        /// position names anything at all is worked out from the fields around it.
        /// </summary>
        /// <remarks>Held as a set: which notes the DTOs earn is the fact, while the order they are met in
        /// is the walk's business. A note not on this list is a new thing the tool cannot see.</remarks>
        private static readonly string[] s_allowedLootTableNotes =
        [
            $"'{nameof(AugmentGroup)}' cannot be built without arguments, so none of its fields carry a default.",
            $"'{nameof(LootTableTierData)}.{ItemsField}' is read through '{nameof(TableRecordsConverter)}', so the shape of the file may differ from the shape of the type: state it with variants.",
            $"'{nameof(TableRecord)}' cannot be built without arguments, so none of its fields carry a default.",
            $"'{nameof(TableRecord)}.{nameof(TableRecord.NamesADrop)}' is worked out from other fields and is not written to the file; it is not in the schema."
        ];

        /// <summary>Every reference the NPC parser resolves by id, addressed the way the file writes it.</summary>
        private static readonly (string Path, string Catalog)[] s_npcReferences =
        [
            ("abilities", DataCatalog.Abilities),
            ("stages.abilities", DataCatalog.Abilities),
            ("abilityBehaviors.id", DataCatalog.Abilities),
            ("reactions.abilityId", DataCatalog.Abilities),
            ("reactions.blockedByFinalDeathOf", DataCatalog.Npc),
            ("passives.id", DataCatalog.PassiveSkills),
        ];

        /// <summary>Every field the NPC parser turns into an enum member. The tool offers the members; a
        /// field the markup missed would take any word and fail at load instead.</summary>
        private static readonly (string Path, Type Members)[] s_npcChoices =
        [
            ("fraction", typeof(Fractions)),
            ("entityType", typeof(EntityType)),
            ("aiIntellect", typeof(AiIntellect)),
            ("stances", typeof(Stance)),
            ("rarity", typeof(Rarity)),
            ("authored.stance", typeof(Stance)),
            ("authored.rarity", typeof(Rarity)),
            ("lifecycle.kind", typeof(NpcLifecycleKind)),
            ("world.activity", typeof(WorldActivityType)),
            ("world.schedule.activity", typeof(WorldActivityType)),
            ("world.routine.activity", typeof(WorldActivityType)),
            ("abilityBehaviors.role", typeof(AbilityRole)),
            ("reactions.trigger", typeof(ReactionTrigger)),
            ("stages.attackEffects.effect", typeof(StageAttackEffectKind)),
        ];

        /// <summary>What the reflector may still have to say about the real behaviour DTOs. Empty: an
        /// archetype and the weighted casts under it are plain properties it can read whole. A note
        /// appearing here is either a DTO to fix or a fact to write down — never something to silence by
        /// widening the check.</summary>
        private static readonly string[] s_allowedNpcBehaviorNotes = [];

        /// <summary>Every reference the behaviour parser resolves by id, addressed the way the file writes
        /// it. An archetype scores casts it names by id and nothing else.</summary>
        private static readonly (string Path, string Catalog)[] s_npcBehaviorReferences =
        [
            (Cast("id"), DataCatalog.Abilities),
        ];

        /// <summary>Every field the behaviour parser turns into an enum member. The stance is the one an
        /// archetype is FOUND by — the provider keys the archetypes with it — so a word naming no member
        /// loses the whole catalog at load, not just its own record.</summary>
        private static readonly (string Path, Type Members)[] s_npcBehaviorChoices =
        [
            (NpcBehaviorsCatalogDescriptor.StanceField, typeof(Stance)),
            (Cast("role"), typeof(AbilityRole)),
        ];

        /// <summary>What a note about a member the file does not hold turns on.</summary>
        private const string WorkedOut = "worked out";

        /// <summary>What a note about a type that names its parts in a constructor turns on: a positional
        /// record cannot be built empty, so nothing can state what its fields start at.</summary>
        private const string NamesItsPartsInAConstructor = "cannot be built";

        /// <summary>
        /// What the reflector has to say about a modifier line wherever one stands — on an item, in a
        /// rollable pool, on a material — as pairs of "about what" and the word the note turns on: the
        /// sentences themselves are the library's wording, and pinning them would fail on a rephrasing that
        /// changed nothing.
        /// <para>Every one of them is a fact about the shipped types: a "number or {min,max}" field is read
        /// through a converter, so the shape of the file may differ from the shape of the type; the two ends
        /// of such a range answer questions worked out from each other; and a line holds lines (composites)
        /// and grants that hold lines again, so the walk meets types it is already inside of.</para>
        /// </summary>
        private static readonly (string About, string Word)[] s_modifierLineNotes =
        [
            ($"{nameof(ItemModifier)}.value", nameof(ValueRangeDataConverter)),
            ($"{nameof(ItemModifier)}.extraUpgradeLevels", nameof(LevelRangeDataConverter)),
            ($"{nameof(ValueRangeData)}.{nameof(ValueRangeData.IsFixed)}", WorkedOut),
            ($"{nameof(ValueRangeData)}.{nameof(ValueRangeData.IsValid)}", WorkedOut),
            ($"{nameof(LevelRangeData)}.{nameof(LevelRangeData.IsFixed)}", WorkedOut),
            ($"{nameof(ItemModifier)}.{PartsField}", nameof(ItemModifier)),
            ($"{nameof(GrantData)}.modifiers", nameof(ItemModifier)),
        ];

        /// <summary>What the walk has to say about the real equipment DTOs: the lines any catalog carrying
        /// them earns, and the two of its own — a template's level is a range too, and its grants are read
        /// before its lines, so the walk meets a grant inside a line already inside a grant.</summary>
        private static readonly (string About, string Word)[] s_allowedEquipItemNotes =
        [
            .. s_modifierLineNotes,
            ($"{nameof(EquipItemData)}.updateLevel", nameof(LevelRangeDataConverter)),
            ($"{nameof(BaseStatData)}.value", nameof(ValueRangeDataConverter)),
            ($"{nameof(ItemModifier)}.grant", nameof(GrantData)),
        ];

        /// <summary>What the walk has to say about the real resource DTOs: the lines a material carries,
        /// and four records that name their parts in a constructor.</summary>
        private static readonly (string About, string Word)[] s_allowedResourceNotes =
        [
            .. s_modifierLineNotes,
            (nameof(MaterialCategoryData), NamesItsPartsInAConstructor),
            (nameof(MaterialData), NamesItsPartsInAConstructor),
            (nameof(UpgradeResourceData), NamesItsPartsInAConstructor),
            (nameof(CraftingResourceData), NamesItsPartsInAConstructor),
        ];

        /// <summary>Every reference the equipment parser resolves by id, addressed the way the file writes
        /// it. A grant names a passive or an effect by the kind it declares; a line names the predicate it
        /// is held up by, wherever a line stands.</summary>
        private static readonly (string Path, string Catalog)[] s_equipItemReferences =
        [
            ("grants.id", DataCatalog.PassiveSkills),
            ("grants.id", DataCatalog.Effects),
            ("implicits.condition", DataCatalog.Conditions),
            ("modifiers.condition", DataCatalog.Conditions),
            ("grants.modifiers.condition", DataCatalog.Conditions),
        ];

        /// <summary>Every field the equipment parser turns into an enum member.</summary>
        private static readonly (string Path, Type Members)[] s_equipItemChoices =
        [
            (EquipItemsCatalogDescriptor.SlotField, typeof(EquipmentPiece)),
            ("weaponType", typeof(WeaponType)),
            ("handedness", typeof(Handedness)),
            ("rarity", typeof(Rarity)),
            ("baseStats.parameter", typeof(EntityParameter)),
            ("implicits.scope", typeof(ModifierScope)),
            ("implicits.affix", typeof(AffixKind)),
            ("modifiers.scope", typeof(ModifierScope)),
            ("modifiers.affix", typeof(AffixKind)),
            ("grants.kind", typeof(GrantKind)),
        ];

        /// <summary>The records written as a number OR a {min,max} object. Read off the schema by the
        /// record standing under a field, so a second field of the same shape is found without being
        /// listed here.</summary>
        private static readonly string[] s_rangeRecords = [nameof(ValueRangeData), nameof(LevelRangeData)];

        /// <summary>The two ends every one of those records is written with.</summary>
        private static readonly string[] s_rangeBounds = ["min", "max"];

        /// <summary>The one slot of the paperdoll no item is written for: which of the two ring slots a
        /// ring lands in is the equipment component's answer, not the record's.</summary>
        private const EquipmentPiece SlotNoItemCarries = EquipmentPiece.Ring2;

        /// <summary>Keys the shipped equipment files write that no field of the schema is written under.
        /// Both are data rather than schema: the game's reader matches names without regard to case and
        /// passes over what it cannot place, so neither is heard from at load.</summary>
        private static readonly string[] s_unknownEquipItemKeys = ["CriticalDamage", "effectId"];

        /// <summary>Every id the pool parser resolves against another catalog, addressed the way the file
        /// writes it. A pool entry is a modifier line and names what any line names.</summary>
        private static readonly (string Path, string Catalog)[] s_modifierPoolReferences =
        [
            (Entry("condition"), DataCatalog.Conditions),
            (Entry("grant.id"), DataCatalog.PassiveSkills),
            (Entry("grant.id"), DataCatalog.Effects),
        ];

        /// <summary>Every field of a pool entry the parser turns into an enum member.</summary>
        private static readonly (string Path, Type Members)[] s_modifierPoolChoices =
        [
            (Entry("scope"), typeof(ModifierScope)),
            (Entry("affix"), typeof(AffixKind)),
            (Entry("grant.kind"), typeof(GrantKind)),
        ];

        /// <summary>The two shipped pool files and whether the entries in them are the ascension gift's:
        /// what tells the files apart is written on the ENTRIES, which is why no placement rule can reach
        /// it. Held here so that a third file, or a mythic entry filed among the ordinary pools, fails.</summary>
        private static readonly (string File, bool Mythic)[] s_modifierPoolFiles =
        [
            ("EquipItemModifierPools", false),
            ("MythicModifiers", true)
        ];

        /// <summary>Every id the resource parser resolves against another catalog, addressed the way the
        /// file writes it, under the section whose records write it. A material names its category in the
        /// catalog it stands in itself.</summary>
        private static readonly (string Section, string Path, string Catalog)[] s_resourceReferences =
        [
            (ResourcesCatalogDescriptor.CraftingResourcesKey, $"material.{ResourcesCatalogDescriptor.CategoryField}", DataCatalog.Resources),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, "material.modifiers.condition", DataCatalog.Conditions),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, "modifiers.condition", DataCatalog.Conditions),
        ];

        /// <summary>Every field the resource parser turns into an enum member, under the section whose
        /// records write it. The lines of an overlay are reached through the map holding them, so what a
        /// line says is checked on both sides of the category split.</summary>
        private static readonly (string Section, string Path, Type Members)[] s_resourceChoices =
        [
            (ResourcesCatalogDescriptor.UpgradeResourcesKey, RarityField, typeof(Rarity)),
            (ResourcesCatalogDescriptor.UpgradeResourcesKey, "category", typeof(EquipmentCategory)),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, RarityField, typeof(Rarity)),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, "material.modifiers.scope", typeof(ModifierScope)),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, "material.modifiers.affix", typeof(AffixKind)),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, $"material.{ResourcesCatalogDescriptor.OverlayField}.scope", typeof(ModifierScope)),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, $"material.{ResourcesCatalogDescriptor.OverlayField}.affix", typeof(AffixKind)),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, "modifiers.scope", typeof(ModifierScope)),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, "modifiers.affix", typeof(AffixKind)),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, $"{ResourcesCatalogDescriptor.OverlayField}.scope", typeof(ModifierScope)),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, $"{ResourcesCatalogDescriptor.OverlayField}.affix", typeof(AffixKind)),
        ];

        /// <summary>The two records writing the overlay: a whole category of materials, and one material
        /// of its own. Both are keyed by the equipment categories and hold the same lines.</summary>
        private static readonly (string Section, string Path)[] s_resourceOverlays =
        [
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, ResourcesCatalogDescriptor.OverlayField),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, $"material.{ResourcesCatalogDescriptor.OverlayField}")
        ];

        /// <summary>Keys the shipped files write that no field of the schema is written under, in both
        /// catalogs carrying modifier lines. All of them sit inside a composite: a line holds lines, so the
        /// walk stops at the second one and what is under it is carried through as the file had it.</summary>
        private static readonly string[] s_unknownKeysInsideComposites =
            ["parameter", "modifierType", "scope", "value", "min", "max"];

        /// <summary>Every catalog a loot position may name its drop out of; any one of them knowing the
        /// id makes the position real, which is why the field carries them all at once.</summary>
        private static readonly string[] s_dropCatalogs =
        [
            DataCatalog.EquipItems,
            DataCatalog.Items,
            DataCatalog.Recipes,
            DataCatalog.Resources
        ];

        /// <summary>A catalog name the tool can neither describe nor knowingly skip is a catalog the
        /// author is silently locked out of, so both lists are held against the constants at once.</summary>
        [TestMethod]
        public void EveryCatalogConstantIsEitherDescribedOrNamedAsNotYet()
        {
            IReadOnlyList<string> catalogs = DataCatalogNames.All();
            Assert.IsTrue(catalogs.Count > 0, "no catalog names were found — the guard is checking nothing");

            List<string> described = [.. CatalogDescriptors.All.Select(descriptor => descriptor.Catalog)];
            List<string> pending = [.. CatalogDescriptors.NotYetDescribed];

            Assert.AreEqual(described.Count, described.Distinct(StringComparer.Ordinal).Count(), "a catalog is described twice");
            Assert.AreEqual(pending.Count, pending.Distinct(StringComparer.Ordinal).Count(), "a catalog is named as undescribed twice");

            List<string> both = [.. described.Intersect(pending, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, both.Count, $"catalogs both described and named as undescribed: {string.Join(", ", both)}");

            HashSet<string> claimed = [.. described, .. pending];
            List<string> unclaimed = [.. catalogs.Where(catalog => !claimed.Contains(catalog)).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, unclaimed.Count, $"catalog names on neither list: {string.Join(", ", unclaimed)}");

            List<string> strangers = [.. claimed.Where(catalog => !catalogs.Contains(catalog)).Order(StringComparer.Ordinal)];
            Assert.AreEqual(0, strangers.Count, $"named by the descriptors and unknown to {nameof(DataCatalog)}: {string.Join(", ", strangers)}");
        }

        /// <summary>The schema of the shipped catalog, built from the shipped DTOs. Both reports gather
        /// what neither reflection nor the assembled parts could vouch for, and an editor drawn from a
        /// schema with holes in it draws those holes as fields the author may not touch.</summary>
        [TestMethod]
        public void TheNpcSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Npc));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(NpcCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(NpcCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "an npc is named in the localization by its own id and has no text of its own besides");

            CollectionAssert.AreEqual(
                s_allowedNpcNotes,
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the NPC DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Npc catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the NPC parser resolves against another catalog, and every name it parses
        /// into an enum, said so in the schema. An unmarked one reads to the tool as free text: the
        /// author types a name nothing answers, and the miss surfaces at spawn.</summary>
        [TestMethod]
        public void TheNpcSchemaNamesTheReferencesTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.Npc).Sections[0].Record;

            foreach ((string path, string catalog) in s_npcReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach ((string path, Type members) in s_npcChoices) Choice(Leaf(Locate(record, path)), members);
        }

        /// <summary>
        /// The shipped file read back through the schema: every key it holds is a key the schema ranks,
        /// and what the canonical write puts out holds everything the file held. The keys of a map are the
        /// exception, and the schema itself names the maps: the order ranks the fields of a record, and a
        /// map has none, so the tool carries its keys through as the file had them.
        /// <para>Where the shipped file wrote the keys of a record in another order the writing IS the
        /// change — the policy is canonicalizing — so the records it would move are reported and not
        /// failed.</para>
        /// </summary>
        [TestMethod]
        public void TheNpcSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.Npc);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.Npc)).Root;
            SchemaKeyOrder order = new(schema);

            (List<string> unknown, List<string> reordered) = Written(root, schema, order);

            Report("Keys of Npc.json the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            Assert.AreEqual(0, unknown.Count,
                $"keys of Npc.json no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.IsTrue(
                JToken.DeepEquals(root, JsonTreeDocument.Parse(CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default)).Root),
                "the canonical write of Npc.json says something other than the file it was given");
        }

        /// <summary>
        /// The starting values an npc is spawned with are a map, and its keys are the schema's answer
        /// rather than free words: the provider parses every one of them into a parameter strictly, so a
        /// key naming no member fails the spawn instead of being ignored. The shipped file is held against
        /// the same members.
        /// </summary>
        [TestMethod]
        public void TheNpcSchemaKeysTheBaseParametersByTheEntityParameters()
        {
            CatalogSchema schema = Schema(DataCatalog.Npc);
            FieldSchema parameters = Locate(schema.Sections[0].Record, BaseParametersField);

            Assert.AreEqual(FieldKind.Dictionary, parameters.Kind, $"'{BaseParametersField}' is not a map");
            Choice(
                parameters.Key ?? throw new AssertFailedException($"'{BaseParametersField}' lets the author write its keys freely"),
                typeof(EntityParameter));

            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.Npc)).Root;
            List<string> written = [.. Keys(root, BaseParametersField).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreNotEqual(0, written.Count, "no record of the shipped file states a starting value — the check is checking nothing");
            CollectionAssert.IsSubsetOf(
                written,
                Enum.GetNames<EntityParameter>(),
                $"the shipped file starts npcs on something other than an {nameof(EntityParameter)}: {string.Join(", ", written)}");
        }

        /// <summary>The schema of the behaviour archetypes, built from the shipped DTOs. Both reports
        /// gather what neither reflection nor the assembled parts could vouch for, and an editor drawn from
        /// a schema with holes in it draws those holes as fields the author may not touch.</summary>
        [TestMethod]
        public void TheNpcBehaviorsSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.NpcBehaviors));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(NpcBehaviorsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(NpcBehaviorsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count, "an archetype is scored against and never shown: nothing about it reaches the player");
            Assert.AreEqual(NpcBehaviorsCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEqual(
                s_allowedNpcBehaviorNotes,
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the NPC behaviour DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled NpcBehaviors catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the behaviour parser resolves against another catalog, and every name it
        /// parses into an enum, said so in the schema. An unmarked one reads to the tool as free text: the
        /// author types a name nothing answers, and the miss surfaces when an npc takes its turn.</summary>
        [TestMethod]
        public void TheNpcBehaviorsSchemaNamesTheReferencesAndChoicesTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.NpcBehaviors).Sections[0].Record;

            foreach ((string path, string catalog) in s_npcBehaviorReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach ((string path, Type members) in s_npcBehaviorChoices) Choice(Leaf(Locate(record, path)), members);
        }

        /// <summary>The shipped file read back through the schema, the way the NPC one is: every key it
        /// writes is one the schema ranks, and the canonical write loses nothing.</summary>
        [TestMethod]
        public void TheNpcBehaviorsSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.NpcBehaviors);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.NpcBehaviors)).Root;
            SchemaKeyOrder order = new(schema);

            (List<string> unknown, List<string> reordered) = Written(root, schema, order);

            Report($"Keys of {NpcBehaviorsCatalogDescriptor.FileName} the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            Assert.AreEqual(0, unknown.Count,
                $"keys of {NpcBehaviorsCatalogDescriptor.FileName} no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.IsTrue(
                JToken.DeepEquals(root, JsonTreeDocument.Parse(CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default)).Root),
                $"the canonical write of {NpcBehaviorsCatalogDescriptor.FileName} says something other than the file it was given");
        }

        /// <summary>
        /// The schema of the loot tables, built from the shipped DTOs. Unlike the plain catalogs these
        /// DTOs do have something to say for themselves — a converter and two positional records — and
        /// each note is named rather than counted: a walk that reports four things is only proof of
        /// anything while those four are the four it is expected to report.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaBuildsFromTheRealDtosWithTheNotesItsPositionsAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.LootTables));

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[]
                {
                    LootTablesCatalogDescriptor.GeneralKey,
                    LootTablesCatalogDescriptor.FractionsKey,
                    LootTablesCatalogDescriptor.TypesKey,
                    LootTablesCatalogDescriptor.IndividualKey
                },
                schema.Sections.Select(section => section.Key).ToArray());
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count, "a table carries no text of its own: what drops is named in its own catalog");
            Assert.AreEqual(LootTablesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEquivalent(
                s_allowedLootTableNotes,
                builder.Reflection.Notes.ToArray(),
                $"reflection has something else to say about the loot table DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled LootTables catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>
        /// A position takes one of two shapes, and the tool has to find both of them two arrays below
        /// every one of the four sections — that is what registering the shapes against the DTO buys.
        /// Which shape stands in a file is told by the key that is there, not by a value: a position
        /// naming a thing writes an id, a position naming a set of augments writes the filter.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaCarriesBothShapesOfAPositionIntoEverySection()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);

            foreach (SectionSchema section in schema.Sections)
            {
                RecordSchema position = Position(section.Record);
                Assert.AreEqual(nameof(TableRecord), position.TypeName);
                Assert.IsNull(position.IdField, "a seat at a table is not a record anything names");

                VariantSet shapes = position.Variants
                                    ?? throw new AssertFailedException($"the positions under '{section.Key}' take one shape only");

                Assert.IsNull(shapes.Discriminator, "the shapes are told apart by the key that is there, not by a value");
                CollectionAssert.AreEqual(
                    new[] { LootTablesCatalogDescriptor.IdField, LootTablesCatalogDescriptor.AugmentsField },
                    shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray());

                RecordSchema named = Shape(shapes, LootTablesCatalogDescriptor.IdField);
                RecordSchema grouped = Shape(shapes, LootTablesCatalogDescriptor.AugmentsField);
                Assert.AreEqual(nameof(LootPositionById), named.TypeName);
                Assert.AreEqual(nameof(LootPositionByGroup), grouped.TypeName);

                NamesADrop(Field(named, LootTablesCatalogDescriptor.IdField), $"{section.Key}: the shape naming one thing");
                NamesADrop(Field(position, LootTablesCatalogDescriptor.IdField), $"{section.Key}: the position itself");

                RecordSchema group = Nested(grouped, LootTablesCatalogDescriptor.AugmentsField);
                Assert.AreEqual(nameof(AugmentGroup), group.TypeName);
                Assert.AreEqual(FieldKind.Integer, Field(group, TierField).Kind);
                Assert.AreEqual(FieldKind.Enum, Field(group, RarityField).Kind);
                CollectionAssert.AreEqual(Enum.GetNames<Rarity>(), Field(group, RarityField).EnumValues.ToArray());
            }
        }

        /// <summary>
        /// What a table's key names is the section's answer and not the type's: one DTO stands under all
        /// four sections, so a key is a fraction here, a kind of foe there, an npc's own id in the third
        /// — and in the fourth a name the author picks, which is said out loud rather than left silent.
        /// </summary>
        [TestMethod]
        public void TheLootTablesSchemaNamesWhatEachSectionsKeyPointsAt()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);

            foreach (SectionSchema section in schema.Sections)
                Assert.AreEqual(LootTablesCatalogDescriptor.KeyField, section.Record.IdField, $"a table of '{section.Key}' is found by another field");

            FieldSchema general = SectionKey(schema, LootTablesCatalogDescriptor.GeneralKey);
            Assert.AreEqual(FieldKind.String, general.Kind);
            Assert.IsTrue(general.RefusedAsReference, "the key of a general table points at nothing, and nothing says so");

            Choice(SectionKey(schema, LootTablesCatalogDescriptor.FractionsKey), typeof(Fractions));
            Choice(SectionKey(schema, LootTablesCatalogDescriptor.TypesKey), typeof(EntityType));

            FieldSchema individual = SectionKey(schema, LootTablesCatalogDescriptor.IndividualKey);
            Assert.AreEqual(FieldKind.Reference, individual.Kind);
            CollectionAssert.AreEqual(new[] { DataCatalog.Npc }, individual.RefCatalogs.ToArray());
        }

        /// <summary>The shipped file read back through the schema, the way the NPC one is: every key it
        /// writes is one the schema ranks, and the canonical write loses nothing. No map stands anywhere
        /// in these tables, so the exception made for keys the author fills himself is checked to be
        /// empty rather than listed — a map added later would go unranked and is a failure here.</summary>
        [TestMethod]
        public void TheLootTablesSchemaRanksEveryKeyTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.LootTables);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.LootTables)).Root;
            SchemaKeyOrder order = new(schema);

            List<string> maps = [.. Records(schema).SelectMany(record => record.Fields).Where(field => Map(field) != null).Select(field => field.JsonName)];
            Assert.AreEqual(0, maps.Count, $"the loot tables hold maps, whose keys nothing ranks: {string.Join(", ", maps)}");

            (List<string> unknown, List<string> reordered) = Written(root, schema, order);

            Report("Keys of LootTables.json the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);

            Assert.AreEqual(0, unknown.Count,
                $"keys of LootTables.json no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.IsTrue(
                JToken.DeepEquals(root, JsonTreeDocument.Parse(CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default)).Root),
                "the canonical write of LootTables.json says something other than the file it was given");
        }

        /// <summary>The schema of the equipment, built from the shipped DTOs. What the walk has to say is
        /// held as pairs of subject and keyword rather than whole sentences: which types earn a note is the
        /// fact being pinned, while the wording belongs to the library.</summary>
        [TestMethod]
        public void TheEquipItemsSchemaBuildsFromTheRealDtosWithTheNotesItsRangesAndCompositesAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.EquipItems));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(EquipItemsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(EquipItemsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "an equipment template is named and described in the localization by its own id");

            Unexpected(builder.Reflection.Notes, s_allowedEquipItemNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled EquipItems catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the equipment parser resolves against another catalog, and every name it
        /// parses into an enum, said so in the schema. An unmarked one reads to the tool as free text: the
        /// author types a name nothing answers, and the miss surfaces when the item is minted.</summary>
        [TestMethod]
        public void TheEquipItemsSchemaNamesTheReferencesAndChoicesTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.EquipItems).Sections[0].Record;

            foreach ((string path, string catalog) in s_equipItemReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach ((string path, Type members) in s_equipItemChoices) Choice(Leaf(Locate(record, path)), members);
        }

        /// <summary>
        /// The catalog is split into a file per slot, and the rule that says so is the record's own field:
        /// every record of every shipped file answers with the slot it carries, and the file it sits in is
        /// named after it. The slots are all the paperdoll has bar the second ring, which no item is
        /// written for.
        /// <para>The placement rule hands the writer the slot, so a file named otherwise than its records'
        /// slot is one the tool would write a second copy of beside.</para>
        /// </summary>
        [TestMethod]
        public void TheEquipItemsCatalogIsSplitIntoOneFilePerSlotByTheRecordsOwnField()
        {
            CatalogSchema schema = Schema(DataCatalog.EquipItems);
            List<string> files = ShippedFiles(DataCatalog.EquipItems);
            HashSet<string> slots = new(StringComparer.Ordinal);

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                List<JObject> records = SectionRecords(JsonTreeDocument.Load(file).Root, EquipItemsCatalogDescriptor.RecordsKey);
                Assert.AreNotEqual(0, records.Count, $"{name} holds no records — the check would pass on an empty file");

                foreach (JObject item in records)
                {
                    string slot = schema.Placement.FileFor(key => item[key]?.Value<string>())
                                  ?? throw new AssertFailedException($"{name}: the placement names no file for '{Id(item, EquipItemsCatalogDescriptor.IdField)}'");

                    Assert.AreEqual(
                        item[EquipItemsCatalogDescriptor.SlotField]?.Value<string>(),
                        slot,
                        $"{name}: '{Id(item, EquipItemsCatalogDescriptor.IdField)}' is placed by something other than the slot it is worn in");

                    Assert.AreEqual(name, slot, $"'{Id(item, EquipItemsCatalogDescriptor.IdField)}' sits in a file named other than the slot it carries");
                    slots.Add(slot);
                }
            }

            string[] worn = [.. Enum.GetNames<EquipmentPiece>().Where(piece => piece != SlotNoItemCarries.ToString())];
            CollectionAssert.AreEquivalent(worn, slots.ToArray(), "the shipped records cover other slots than the paperdoll has");
            Assert.AreEqual(worn.Length, files.Count, "the catalog ships another number of files than there are slots to split it by");
        }

        /// <summary>
        /// A "number or {min,max}" field reaches the tool as the object alone: the contract states shapes as
        /// whole records and a bare number is not one, so the scalar form has no variant to be drawn as.
        /// The run counts how many records are written the scalar way, which is what a decision to widen the
        /// contract would be taken on.
        /// </summary>
        [TestMethod]
        public void TheEquipItemsSchemaDrawsARangeAsItsObjectFormAndCountsWhatIsWrittenAsANumber()
        {
            CatalogSchema schema = Schema(DataCatalog.EquipItems);
            HashSet<string> ranges = RangeFields(schema);
            Assert.AreNotEqual(0, ranges.Count, "no field of the catalog is a range — the count is counting nothing");

            foreach (RecordSchema range in Records(schema).Where(record => s_rangeRecords.Contains(record.TypeName)))
                foreach (string bound in s_rangeBounds)
                {
                    FieldSchema end = Field(range, bound);
                    Assert.IsTrue(end.Kind is FieldKind.Integer or FieldKind.Number, $"'{range.TypeName}.{bound}' is not a number");
                }

            List<string> scalars = [];

            foreach (string file in ShippedFiles(DataCatalog.EquipItems))
            {
                int written = Scalars(JsonTreeDocument.Load(file).Root, ranges);
                if (written > 0) scalars.Add($"{Path.GetFileName(file)}: {written}");
            }

            Report($"Ranges written as a plain number, which the tool draws as raw json ({string.Join(", ", ranges.Order(StringComparer.Ordinal))})", scalars);
        }

        /// <summary>The shipped files read back through the schema, all nine of them the way the game reads
        /// them: every key they write is one the schema ranks, and the canonical write neither loses
        /// anything nor moves on a second pass. The keys it does not know are named one by one — a key the
        /// schema cannot place is a field the tool would not let the author touch.</summary>
        [TestMethod]
        public void TheEquipItemsSchemaRanksEveryKeyTheShippedFilesWrite()
        {
            List<string> unknown = UnknownKeys(Schema(DataCatalog.EquipItems), DataCatalog.EquipItems);

            CollectionAssert.AreEquivalent(
                s_unknownEquipItemKeys,
                unknown.Select(LastKey).Distinct(StringComparer.Ordinal).ToArray(),
                $"the EquipItems files write other keys than the known ones no field of the schema is written under:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", unknown)}");
        }

        /// <summary>The schema of the rollable pools, built from the shipped DTOs. A pool is a bag of
        /// modifier lines and nothing else, so what the walk has to say about it is what a line is known
        /// for wherever one stands.</summary>
        [TestMethod]
        public void TheModifierPoolsSchemaBuildsFromTheRealDtosWithTheNotesItsLinesAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.ModifierPools));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(ModifierPoolsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(ModifierPoolsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count, "a pool is drawn from and never shown: the line it hands out is worded from its own parameter");

            Unexpected(builder.Reflection.Notes, s_modifierLineNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled ModifierPools catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id a pool entry resolves against another catalog, and every name it parses into
        /// an enum, said so in the schema — the same markup the equipment lines are read by, reached down
        /// the field a pool holds its entries in.</summary>
        [TestMethod]
        public void TheModifierPoolsSchemaNamesTheReferencesAndChoicesTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.ModifierPools).Sections[0].Record;

            foreach ((string path, string catalog) in s_modifierPoolReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach ((string path, Type members) in s_modifierPoolChoices) Choice(Leaf(Locate(record, path)), members);
        }

        /// <summary>
        /// The catalog ships two files with two meanings — the pools an item rolls from and the pools the
        /// ascension gift draws from — and no field of a POOL says which it belongs to: the mythic mark is
        /// written on its entries, one level below anything a placement rule is handed. So the file a new
        /// pool goes to is the tool's to choose, and the two facts are held together here: what the files
        /// are apart, and that the record cannot say it.
        /// </summary>
        [TestMethod]
        public void TheModifierPoolsCatalogLeavesTheFileToTheToolBecauseNoPoolNamesOne()
        {
            CatalogSchema schema = Schema(DataCatalog.ModifierPools);

            Assert.IsInstanceOfType<FreeFilePlacement>(schema.Placement, "the pools name a file of their own");
            CollectionAssert.AreEqual(
                new[] { ModifierPoolsCatalogDescriptor.IdField, ModifierPoolsCatalogDescriptor.EntriesField },
                schema.Sections[0].Record.Fields.Select(field => field.JsonName).ToArray(),
                "a pool writes a field besides its id and its entries — one of them may well name the file");

            List<string> files = ShippedFiles(DataCatalog.ModifierPools);
            CollectionAssert.AreEquivalent(
                s_modifierPoolFiles.Select(known => known.File).ToArray(),
                files.Select(Path.GetFileNameWithoutExtension).ToArray(),
                "the catalog ships other files than the two known ones");

            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                bool mythic = s_modifierPoolFiles.First(known => known.File == name).Mythic;
                List<JObject> pools = SectionRecords(JsonTreeDocument.Load(file).Root, ModifierPoolsCatalogDescriptor.RecordsKey);

                Assert.AreNotEqual(0, pools.Count, $"{name} holds no pools — the check would pass on an empty file");

                foreach (JObject pool in pools)
                    foreach (JObject entry in Entries(pool))
                        Assert.AreEqual(
                            mythic,
                            entry[AffixField]?.Value<string>() == nameof(AffixKind.Mythic),
                            $"{name}: an entry of '{Id(pool, ModifierPoolsCatalogDescriptor.IdField)}' claims another slot family than the file it is written in stands for");
            }
        }

        /// <summary>The shipped pool files read back through the schema. Everything they write is ranked
        /// bar the insides of a composite: a line holding lines is where the walk stops.</summary>
        [TestMethod]
        public void TheModifierPoolsSchemaRanksEveryKeyTheShippedFilesWriteOutsideAComposite()
        {
            InsideCompositesOnly(
                UnknownKeys(Schema(DataCatalog.ModifierPools), DataCatalog.ModifierPools),
                DataCatalog.ModifierPools);
        }

        /// <summary>The schema of the resources: three sections of one file, each record found by its own
        /// id and named in the localization by it. Beside what a modifier line is known for, the walk has
        /// one thing to say per record — every one of them names its parts in a constructor, so nothing can
        /// state what its fields start at.</summary>
        [TestMethod]
        public void TheResourcesSchemaBuildsFromTheRealDtosWithTheNotesItsRecordsAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Resources));

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[]
                {
                    ResourcesCatalogDescriptor.MaterialCategoriesKey,
                    ResourcesCatalogDescriptor.UpgradeResourcesKey,
                    ResourcesCatalogDescriptor.CraftingResourcesKey
                },
                schema.Sections.Select(section => section.Key).ToArray());

            foreach (SectionSchema section in schema.Sections)
                Assert.AreEqual(ResourcesCatalogDescriptor.IdField, section.Record.IdField, $"a record of '{section.Key}' is found by another field");

            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "a resource is named in the localization by its own id and has no text of its own besides");
            Assert.AreEqual(ResourcesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            Unexpected(builder.Reflection.Notes, s_allowedResourceNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Resources catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the resource parser resolves and every name it parses into an enum, said so in
        /// the schema. The material's category is a reference like any other, and points into the catalog it
        /// is written in itself: a resource naming a category nothing answers loses its lines at load.</summary>
        [TestMethod]
        public void TheResourcesSchemaNamesTheReferencesAndChoicesTheParserResolves()
        {
            CatalogSchema schema = Schema(DataCatalog.Resources);

            foreach ((string section, string path, string catalog) in s_resourceReferences)
                Points(Leaf(Locate(Section(schema, section), path)), $"{section}.{path}", catalog);

            foreach ((string section, string path, Type members) in s_resourceChoices)
                Choice(Leaf(Locate(Section(schema, section), path)), members);
        }

        /// <summary>
        /// The overlay a category and a material carry is a map keyed by the equipment categories, and the
        /// keys are the schema's answer rather than free words: an author writing a fourth section loses it
        /// at load with nothing but a report. The shipped file is held against the same members.
        /// </summary>
        [TestMethod]
        public void TheResourcesSchemaKeysTheCategoryOverlayByTheEquipmentCategories()
        {
            CatalogSchema schema = Schema(DataCatalog.Resources);

            foreach ((string section, string path) in s_resourceOverlays)
            {
                FieldSchema overlay = Locate(Section(schema, section), path);

                Assert.AreEqual(FieldKind.Dictionary, overlay.Kind, $"'{section}.{path}' is not a map");
                Choice(
                    overlay.Key ?? throw new AssertFailedException($"'{section}.{path}' lets the author write its keys freely"),
                    typeof(EquipmentCategory));
            }

            JToken root = JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.Resources)).Root;
            List<string> written = [.. Keys(root, ResourcesCatalogDescriptor.OverlayField).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreNotEqual(0, written.Count, "no record of the shipped file splits its lines by category — the check is checking nothing");
            CollectionAssert.IsSubsetOf(
                written,
                Enum.GetNames<EquipmentCategory>(),
                $"the shipped file splits its lines by something other than an {nameof(EquipmentCategory)}: {string.Join(", ", written)}");
        }

        /// <summary>The shipped resource file read back through the schema. Everything it writes is ranked
        /// bar the insides of a composite, the keys of the overlay included: those are the members of an
        /// enum rather than a record's fields, and they keep the place the file gave them.</summary>
        [TestMethod]
        public void TheResourcesSchemaRanksEveryKeyTheShippedFileWritesOutsideAComposite()
        {
            InsideCompositesOnly(
                UnknownKeys(Schema(DataCatalog.Resources), DataCatalog.Resources),
                DataCatalog.Resources);
        }

        /// <summary>
        /// Across every described catalog: a field written under a name ending in "id" either points into
        /// a catalog or says out loud that it does not. The one exception is the field a record IS found
        /// by, which names its own record rather than another.
        /// <para>Keys of a map are not covered — the contract has no way of refusing them — so a map keyed
        /// by ids is still a silence this cannot see.</para>
        /// </summary>
        [TestMethod]
        public void EveryFieldWrittenUnderAnIdNameIsAReferenceOrRefusesToBeOne()
        {
            List<string> silent = [];

            foreach (ICatalogDescriptor descriptor in CatalogDescriptors.All)
                foreach (RecordSchema record in Records(new CatalogSchemaBuilder(new SchemaReflector()).Build(descriptor)))
                    foreach (FieldSchema field in record.Fields)
                    {
                        if (!field.JsonName.EndsWith(IdSuffix, StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(field.JsonName, record.IdField, StringComparison.Ordinal)) continue;

                        FieldSchema leaf = Leaf(field);
                        if (leaf.Kind == FieldKind.Reference || leaf.RefusedAsReference) continue;

                        silent.Add($"{descriptor.Catalog}: {record.TypeName}.{field.JsonName}");
                    }

            Assert.AreEqual(0, silent.Count,
                $"fields named like an id that neither point anywhere nor say they do not: {string.Join(", ", silent.Distinct().Order(StringComparer.Ordinal))}");
        }

        /// <summary>The one descriptor of a catalog, taken from the registry the tool reads: a schema
        /// built from a descriptor the registry does not hold would pin nothing the tool uses.</summary>
        private static ICatalogDescriptor Descriptor(string catalog) =>
            CatalogDescriptors.All.FirstOrDefault(descriptor => descriptor.Catalog == catalog)
            ?? throw new AssertFailedException($"{nameof(CatalogDescriptors)} holds no descriptor of the {catalog} catalog.");

        private static CatalogSchema Schema(string catalog) => new CatalogSchemaBuilder(new SchemaReflector()).Build(Descriptor(catalog));

        /// <summary>Every note the walk had, held against what the DTOs are known for. A note matching no
        /// pair is something new the tool cannot see; a pair matching no note is a fact that has moved on
        /// and would go on excusing whatever grows into its place.</summary>
        private static void Unexpected(IReadOnlyList<string> notes, (string About, string Word)[] allowed, SchemaReflectionReport report)
        {
            bool Matches((string About, string Word) pair, string note) =>
                note.Contains(pair.About, StringComparison.Ordinal) && note.Contains(pair.Word, StringComparison.Ordinal);

            List<string> strangers = [.. notes.Where(note => !allowed.Any(pair => Matches(pair, note))).Distinct(StringComparer.Ordinal)];
            List<string> stale = [.. allowed.Where(pair => !notes.Any(note => Matches(pair, note))).Select(pair => $"{pair.About} ({pair.Word})")];

            Assert.AreEqual(0, strangers.Count,
                $"reflection has something else to say about the DTOs:{Environment.NewLine}{string.Join(Environment.NewLine, strangers)}{Environment.NewLine}all of it:{Environment.NewLine}{report}");
            Assert.AreEqual(0, stale.Count, $"nothing was said about: {string.Join(", ", stale)}");
        }

        /// <summary>Every shipped file of a catalog folder, in the order the game's own data source reads
        /// them — a catalog is the union of its files, and one of them is not the catalog.</summary>
        private static List<string> ShippedFiles(string catalog) =>
            [.. CatalogWorkspace.FilePaths(SharedData.Catalog(catalog))];

        /// <summary>The records one section of a file holds.</summary>
        private static List<JObject> SectionRecords(JToken root, string section) =>
            [.. (root[section] as JArray ?? []).OfType<JObject>()];

        /// <summary>What a record calls itself, for a message about it.</summary>
        private static string Id(JObject record, string idField) =>
            record[idField]?.Value<string>() ?? string.Empty;

        /// <summary>The entries of one shipped pool.</summary>
        private static List<JObject> Entries(JObject pool) =>
            [.. (pool[ModifierPoolsCatalogDescriptor.EntriesField] as JArray ?? []).OfType<JObject>()];

        /// <summary>The address of a field of a pool entry: an entry is written under the field a pool
        /// holds its entries in, and everything about it is addressed from there.</summary>
        private static string Entry(string path) =>
            $"{ModifierPoolsCatalogDescriptor.EntriesField}{PathSeparator}{path}";

        /// <summary>The address of a field of one weighted cast: a cast is written under the field an
        /// archetype holds its casts in, and everything about it is addressed from there.</summary>
        private static string Cast(string path) =>
            $"{NpcBehaviorsCatalogDescriptor.AbilitiesField}{PathSeparator}{path}";

        /// <summary>The records of one section, which is where a path down a catalog of several starts.</summary>
        private static RecordSchema Section(CatalogSchema schema, string section) =>
            schema.Sections.FirstOrDefault(candidate => candidate.Key == section)?.Record
            ?? throw new AssertFailedException($"the catalog holds no '{section}' section.");

        /// <summary>That a field names a record of a catalog: an unmarked one reads to the tool as free
        /// text, and the author types a name nothing answers.</summary>
        private static void Points(FieldSchema field, string path, string catalog)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{path}' is not a reference");
            CollectionAssert.Contains(field.RefCatalogs.ToArray(), catalog, $"'{path}' does not point into {catalog}");
        }

        /// <summary>The keys one map is written with, wherever in a document it stands.</summary>
        private static IEnumerable<string> Keys(JToken token, string map)
        {
            switch (token)
            {
                case JObject holder:
                    foreach (JProperty property in holder.Properties())
                    {
                        if (property.Name == map && property.Value is JObject keyed)
                            foreach (JProperty key in keyed.Properties())
                                yield return key.Name;

                        foreach (string nested in Keys(property.Value, map)) yield return nested;
                    }

                    break;

                case JArray array:
                    foreach (JToken item in array)
                        foreach (string nested in Keys(item, map))
                            yield return nested;

                    break;
            }
        }

        /// <summary>How many times a key is written anywhere in a document — how many composites a catalog
        /// ships, how many of its lines hand out behaviour instead of a number.</summary>
        private static int Occurrences(JToken token, string key) => token switch
        {
            JObject holder => holder.Properties().Sum(property => (property.Name == key ? 1 : 0) + Occurrences(property.Value, key)),
            JArray array => array.Sum(item => Occurrences(item, key)),
            _ => 0
        };

        /// <summary>
        /// Every shipped file of a catalog read back through its schema: the addresses of the keys no field
        /// is written under, gathered for the caller to hold against what it expects, while the canonical
        /// write is checked here — it must lose nothing and say the same thing on a second pass, whichever
        /// catalog it is given.
        /// </summary>
        private static List<string> UnknownKeys(CatalogSchema schema, string catalog)
        {
            SchemaKeyOrder order = new(schema);
            List<string> unknown = [];
            List<string> reordered = [];
            int composites = 0;
            int grants = 0;

            foreach (string file in ShippedFiles(catalog))
            {
                string name = Path.GetFileName(file);
                JToken root = JsonTreeDocument.Load(file).Root;
                (List<string> unplaced, List<string> moved) = Written(root, schema, order);

                unknown.AddRange(unplaced.Select(key => $"{name}{key}"));
                reordered.AddRange(moved.Select(record => $"{name} {record}"));
                composites += Occurrences(root, PartsField);
                grants += Occurrences(root, GrantField);

                string once = CanonicalJsonWriter.Write(root, order, CanonicalJsonOptions.Default);
                JToken written = JsonTreeDocument.Parse(once).Root;

                Assert.AreEqual(once, CanonicalJsonWriter.Write(written, order, CanonicalJsonOptions.Default),
                    $"the canonical write of {name} is not settled: writing what it produced says something else again");
                Assert.IsTrue(JToken.DeepEquals(root, written),
                    $"the canonical write of {name} says something other than the file it was given");
            }

            Report($"Keys of the {catalog} files the schema does not know", unknown);
            Report("Records the canonical write would reorder", reordered);
            Report($"Lines of the {catalog} files the walk cannot rank the insides of",
                [$"{PartsField}: {composites}", $"{GrantField}: {grants}"]);

            return unknown;
        }

        /// <summary>That the keys the schema could not place all sit inside a composite, and that a
        /// composite writes the keys a line is known for. A line holds lines, so the walk stops at the
        /// second one and everything under it is carried through as the file had it — a key ANYWHERE else
        /// is a field the tool would not let the author touch.</summary>
        private static void InsideCompositesOnly(List<string> unknown, string catalog)
        {
            string inside = $"{PointerSeparator}{PartsField}{PointerSeparator}";
            List<string> elsewhere = [.. unknown.Where(address => !address.Contains(inside, StringComparison.Ordinal))];

            Assert.AreEqual(0, elsewhere.Count,
                $"keys of the {catalog} files outside a composite that no field of the schema is written under: {string.Join(", ", elsewhere)}");
            CollectionAssert.AreEquivalent(
                s_unknownKeysInsideComposites,
                unknown.Select(LastKey).Distinct(StringComparer.Ordinal).ToArray(),
                $"a composite of the {catalog} files writes other keys than the known ones:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", unknown)}");
        }

        /// <summary>Json names of the fields holding a range record — a number or a pair of bounds. Read
        /// off the schema rather than listed, so a field of the same shape added later is counted too.</summary>
        private static HashSet<string> RangeFields(CatalogSchema schema) =>
        [
            .. Records(schema)
                .SelectMany(record => record.Fields)
                .Where(field => Leaf(field).Record is { } held && s_rangeRecords.Contains(held.TypeName))
                .Select(field => field.JsonName)
        ];

        /// <summary>How many of those fields the document writes as a plain number instead of the object
        /// the schema states.</summary>
        private static int Scalars(JToken token, HashSet<string> ranges) => token switch
        {
            JObject holder => holder.Properties().Sum(property =>
                (ranges.Contains(property.Name) && property.Value is not JObject ? 1 : 0) + Scalars(property.Value, ranges)),
            JArray array => array.Sum(item => Scalars(item, ranges)),
            _ => 0
        };

        /// <summary>The key an address ends in, which is the name the schema does not know.</summary>
        private static string LastKey(string address) => address.Split(PointerSeparator)[^1];

        /// <summary>The shipped file of a catalog, named by the schema's own placement rule.</summary>
        private static string CatalogFile(CatalogSchema schema, string catalog)
        {
            string name = schema.Placement.FileFor(_ => null)
                          ?? throw new AssertFailedException($"the {catalog} catalog names no file of its own.");

            return Path.Combine(SharedData.Catalog(catalog), name + CatalogWorkspace.FileExtension);
        }

        /// <summary>What a shipped file writes against what the schema knows: the keys no field is
        /// written under, and the records the canonical write would put in another order.</summary>
        private static (List<string> Unknown, List<string> Reordered) Written(JToken root, CatalogSchema schema, SchemaKeyOrder order)
        {
            HashSet<string> maps = Maps(schema);
            List<string> unknown = [];
            List<string> reordered = [];

            foreach ((JsonPointer at, JObject holder) in Objects(root, JsonPointer.Root))
            {
                List<string> written = [.. holder.Properties().Select(property => property.Name)];
                List<string> ranked = [.. written.OrderBy(key => order.Rank(at, key))];

                if (!written.SequenceEqual(ranked, StringComparer.Ordinal))
                    reordered.Add($"{at}: {string.Join(", ", written)} → {string.Join(", ", ranked)}");

                if (maps.Contains(at.Last ?? string.Empty)) continue;

                unknown.AddRange(written
                    .Where(key => order.Rank(at, key) == IKeyOrder.Unknown)
                    .Select(key => $"{at}/{key}"));
            }

            return (unknown, reordered);
        }

        /// <summary>The field one path addresses, stepping through whatever lists and maps hold the
        /// records on the way.</summary>
        private static FieldSchema Locate(RecordSchema record, string path)
        {
            RecordSchema level = record;
            string[] segments = path.Split(PathSeparator);

            for (int segment = 0; segment < segments.Length; segment++)
            {
                FieldSchema field = level.Fields.FirstOrDefault(candidate => candidate.JsonName == segments[segment])
                                    ?? throw new AssertFailedException($"'{level.TypeName}' writes no '{segments[segment]}' (looking for '{path}').");

                if (segment == segments.Length - 1) return field;

                level = Leaf(field).Record
                        ?? throw new AssertFailedException($"'{path}' walks through '{segments[segment]}', which holds no records.");
            }

            throw new AssertFailedException($"'{path}' addresses nothing.");
        }

        private static FieldSchema Field(RecordSchema record, string jsonName) =>
            record.Fields.FirstOrDefault(field => field.JsonName == jsonName)
            ?? throw new AssertFailedException($"'{record.TypeName}' writes no '{jsonName}'.");

        /// <summary>The record a field holds one of.</summary>
        private static RecordSchema Nested(RecordSchema record, string jsonName) =>
            Leaf(Field(record, jsonName)).Record
            ?? throw new AssertFailedException($"'{record.TypeName}.{jsonName}' holds no records.");

        /// <summary>The seats of a table: two arrays down, whichever section the table stands in.</summary>
        private static RecordSchema Position(RecordSchema table) => Nested(Nested(table, TiersField), ItemsField);

        private static RecordSchema Shape(VariantSet shapes, string key) =>
            shapes.Variants.FirstOrDefault(variant => variant.DiscriminatorValue == key)?.Record
            ?? throw new AssertFailedException($"no shape is picked by '{key}'.");

        private static FieldSchema SectionKey(CatalogSchema schema, string section) =>
            Field(Section(schema, section), LootTablesCatalogDescriptor.KeyField);

        /// <summary>That a field names something to drop, out of every catalog droppable things live in.</summary>
        private static void NamesADrop(FieldSchema field, string what)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"{what} does not name anything");
            Assert.IsFalse(field.AllowEmpty, $"{what} may be left empty, which prices a seat that drops nothing");
            CollectionAssert.AreEquivalent(s_dropCatalogs, field.RefCatalogs.ToArray(), $"{what} points into other catalogs");
        }

        private static void Choice(FieldSchema field, Type members)
        {
            Assert.AreEqual(FieldKind.Enum, field.Kind, $"'{field.JsonName}' is not a choice of names");
            CollectionAssert.AreEqual(Enum.GetNames(members), field.EnumValues.ToArray(), $"'{field.JsonName}' offers other members than {members.Name}");
        }

        /// <summary>What a field ultimately holds: the element of a list, the value of a map, or the
        /// field itself. Markup travels to the value, so that is where a reference or a choice is said.</summary>
        private static FieldSchema Leaf(FieldSchema field)
        {
            FieldSchema leaf = field;

            while (leaf.Kind is FieldKind.Array or FieldKind.Dictionary && leaf.Item is { } item) leaf = item;

            return leaf;
        }

        /// <summary>The map a field holds, if it holds one: the field itself, or the element of the list
        /// it is. Null when nothing under the field is a map.</summary>
        private static FieldSchema? Map(FieldSchema field)
        {
            for (FieldSchema? node = field; node != null; node = node.Item)
                if (node.Kind == FieldKind.Dictionary) return node;

            return null;
        }

        /// <summary>Json names of every map the catalog holds, read off the schema rather than listed here.
        /// The order ranks the keys of a RECORD, and a map has none — whether its keys are free words or the
        /// members of an enum, they keep the place the file gave them, and what they are allowed to be is
        /// checked where the map is described.</summary>
        private static HashSet<string> Maps(CatalogSchema schema) =>
        [
            .. Records(schema)
                .SelectMany(record => record.Fields)
                .Where(field => Map(field) != null)
                .Select(field => field.JsonName)
        ];

        /// <summary>Every record the catalog reaches: those of its sections, the shapes they take, and
        /// everything nested in their fields. A field leading back to a record already being read carries
        /// no record of its own, so the walk always ends.</summary>
        private static IEnumerable<RecordSchema> Records(CatalogSchema schema)
        {
            HashSet<RecordSchema> seen = new(ReferenceEqualityComparer.Instance);
            Stack<RecordSchema> pending = new(schema.Sections.Select(section => section.Record));

            while (pending.Count > 0)
            {
                RecordSchema record = pending.Pop();

                if (!seen.Add(record)) continue;

                yield return record;

                if (record.Variants is { } variants)
                    foreach (VariantSchema variant in variants.Variants)
                        pending.Push(variant.Record);

                foreach (FieldSchema field in record.Fields)
                    foreach (RecordSchema nested in Held(field))
                        pending.Push(nested);
            }
        }

        /// <summary>The records standing under one field, whatever lists and maps hold them.</summary>
        private static IEnumerable<RecordSchema> Held(FieldSchema field)
        {
            if (field.Record is { } record) yield return record;

            if (field.Item is { } item)
                foreach (RecordSchema nested in Held(item))
                    yield return nested;

            if (field.Key is { } key)
                foreach (RecordSchema nested in Held(key))
                    yield return nested;
        }

        /// <summary>Every object of a document with the address it sits at — the address the key order
        /// is asked about.</summary>
        private static IEnumerable<(JsonPointer At, JObject Holder)> Objects(JToken token, JsonPointer at)
        {
            switch (token)
            {
                case JObject holder:
                    yield return (at, holder);

                    foreach (JProperty property in holder.Properties())
                        foreach ((JsonPointer, JObject) nested in Objects(property.Value, at.Append(property.Name)))
                            yield return nested;

                    break;

                case JArray array:
                    for (int index = 0; index < array.Count; index++)
                        foreach ((JsonPointer, JObject) nested in Objects(array[index], at.Append(index)))
                            yield return nested;

                    break;
            }
        }

        /// <summary>What the run has to say about the shipped file — a finding for whoever owns the data,
        /// never a failure: neither list is something the schema can put right.</summary>
        private static void Report(string what, List<string> lines) =>
            Console.WriteLine(lines.Count == 0
                ? $"{what}: none."
                : $"{what} ({lines.Count}):{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", lines)}");
    }
}
