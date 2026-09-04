namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai;
    using Core.Ai.World;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.CraftingData;
    using Core.Data.EquipData;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Data.NpcData;
    using Core.Data.PlayerStatsData;
    using Core.Data.Schema;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.Modifiers.Conditions;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using LastBreath.Descriptors;
    using Moq;
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

        /// <summary>What a reference into the whole of a catalog names as its section: none. Every catalog
        /// whose records answer to one another alike is pointed into this way.</summary>
        private const string? WholeCatalog = null;

        /// <summary>Every reference the NPC parser resolves by id, addressed the way the file writes it,
        /// with the section of the catalog it is answered from. Everything an npc names out of the
        /// abilities catalog is a CAST: the augments written beside them are nobody's to cast, and a field
        /// naming the whole catalog would offer the author a hundred ids the provider drops.</summary>
        private static readonly (string Path, string Catalog, string? Section)[] s_npcReferences =
        [
            ("abilities", DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
            ("stages.abilities", DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
            ("abilityBehaviors.id", DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
            ("reactions.abilityId", DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
            ("reactions.blockedByFinalDeathOf", DataCatalog.Npc, WholeCatalog),
            ("passives.id", DataCatalog.PassiveSkills, WholeCatalog),
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
        private static readonly (string Path, string Catalog, string? Section)[] s_npcBehaviorReferences =
        [
            (Cast("id"), DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
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
        /// file writes it, under the section whose records write it, with the section it is answered from.
        /// A material names its category in the catalog it stands in itself — and only in the section of
        /// categories: six of the seventy-odd ids that catalog writes are answers the parser accepts.</summary>
        private static readonly (string Section, string Path, string Catalog, string? Into)[] s_resourceReferences =
        [
            (ResourcesCatalogDescriptor.CraftingResourcesKey, $"material.{ResourcesCatalogDescriptor.CategoryField}",
                DataCatalog.Resources, ResourcesCatalogDescriptor.MaterialCategoriesKey),
            (ResourcesCatalogDescriptor.CraftingResourcesKey, "material.modifiers.condition", DataCatalog.Conditions, WholeCatalog),
            (ResourcesCatalogDescriptor.MaterialCategoriesKey, "modifiers.condition", DataCatalog.Conditions, WholeCatalog),
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

        /// <summary>What an ability writes its stance, its cost currency and the way it picks targets
        /// under. No descriptor names them — they are the DTO's own fields, the way a table's tiers are —
        /// so the walk spells them out.</summary>
        private const string StanceField = "stance";

        private const string CostTypeField = "costType";

        private const string TargetTypeField = "targetType";

        /// <summary>The two ends of the rarity band an augment's copies are drawn from.</summary>
        private const string MinRarityField = "minRarity";

        private const string MaxRarityField = "maxRarity";

        /// <summary>What an augment writes the effect its behaviour lays under, and the pool it draws one
        /// out of instead.</summary>
        private const string EffectField = "effectId";

        private const string EffectPoolField = "effectPool";

        /// <summary>The three fields an augment names something out of a registry of CODE under — a
        /// behaviour, an attack modifier, and the host parameters its properties are read off. No catalog
        /// holds any of them, which the records say out loud rather than leaving unmarked.</summary>
        private static readonly string[] s_augmentRefusals = ["behaviour", "attackModifier", "propertyRefs"];

        /// <summary>Which touches an augment's behaviour works on.</summary>
        private const string ImpactKindField = "impactKind";

        /// <summary>
        /// What the reflector has to say about the real ability DTOs. Both are facts about the shipped
        /// types: an augment works its rarity band and the effect a bare record stands for out of the
        /// fields around them, so neither is written to the file.
        /// </summary>
        /// <remarks>The two members worked out the same way that hold COLLECTIONS say so to the serializer
        /// instead: a get-only list is what a reader pours values INTO, so the walk cannot tell one from a
        /// key the author writes and would have offered both as fields.</remarks>
        private static readonly (string About, string Word)[] s_allowedAbilityNotes =
        [
            ($"{nameof(AbilityAugmentData)}.{nameof(AbilityAugmentData.RarityBand)}", WorkedOut),
            ($"{nameof(AbilityAugmentData)}.{nameof(AbilityAugmentData.LaidEffectId)}", WorkedOut),
        ];

        /// <summary>Every id the ability parser resolves against another catalog, under the section whose
        /// records write it, with the section it is answered from. An augment names the one ability it is
        /// written for and the effect its behaviour lays; an ability names nothing outside itself. The
        /// binding is answered by the casts alone: the augments stand in the same file, and a field naming
        /// the whole catalog would take one augment as the binding of another.</summary>
        private static readonly (string Section, string Path, string Catalog, string? Into)[] s_abilityReferences =
        [
            (AbilitiesCatalogDescriptor.AugmentsKey, AbilitiesCatalogDescriptor.AbilityField,
                DataCatalog.Abilities, AbilitiesCatalogDescriptor.AbilitiesKey),
            (AbilitiesCatalogDescriptor.AugmentsKey, EffectField, DataCatalog.Effects, WholeCatalog),
        ];

        /// <summary>Every field the ability parser turns into an enum member, under the section whose
        /// records write it. An unmarked one reads to the tool as free text: the author types a name
        /// nothing answers, and the miss surfaces when the cast is built.</summary>
        private static readonly (string Section, string Path, Type Members)[] s_abilityChoices =
        [
            (AbilitiesCatalogDescriptor.AbilitiesKey, StanceField, typeof(Stance)),
            (AbilitiesCatalogDescriptor.AbilitiesKey, CostTypeField, typeof(Costs)),
            (AbilitiesCatalogDescriptor.AbilitiesKey, TargetTypeField, typeof(AbilityTargetType)),
            (AbilitiesCatalogDescriptor.AugmentsKey, RarityField, typeof(Rarity)),
            (AbilitiesCatalogDescriptor.AugmentsKey, MinRarityField, typeof(Rarity)),
            (AbilitiesCatalogDescriptor.AugmentsKey, MaxRarityField, typeof(Rarity)),
            (AbilitiesCatalogDescriptor.AugmentsKey, ImpactKindField, typeof(Core.Data.ImpactKind)),
        ];

        /// <summary>Everything an ability is written with, in the order a canonical file writes it.</summary>
        private static readonly string[] s_abilityFields =
        [
            "id", "tags", "cooldown", "costValue", CostTypeField, StanceField, TargetTypeField, "maxTargets",
            "hidden", "damage", "weaponDamageScale", "spellDamageScale", "abilityProperties"
        ];

        /// <summary>
        /// Everything an augment is written with, in the order a canonical file writes it — the whole of
        /// what the tool would offer an author. Held as a list because the walk cannot see the one way a
        /// field arrives by mistake: a member worked out from the others that happens to hold a COLLECTION
        /// reads to reflection exactly like one a file fills, and would be offered as a key nobody may
        /// write. Nothing else names it, since the shipped file does not write it either.
        /// </summary>
        /// <remarks>Three axes are written here flat, every key of them optional, because the shapes tell
        /// records apart by ONE thing and the binding is what they were spent on: how a value is written
        /// (<c>upgradeProperties</c> alone, with <c>bestRarityProperties</c>, or as an authored
        /// <c>rarityLadder</c>), what the augment lays (<c>effectId</c> or <c>effectPool</c>), and which
        /// road its behaviour works on.</remarks>
        private static readonly string[] s_augmentFields =
        [
            "id", AbilitiesCatalogDescriptor.TagsField, TierField, RarityField, MinRarityField, MaxRarityField,
            AbilitiesCatalogDescriptor.AbilityField, AbilitiesCatalogDescriptor.AnyAbilityField, "exclusionGroup",
            "grantsTags", "behaviour", EffectField, EffectPoolField, ImpactKindField, "attackModifier",
            "poolFromWholeHit", "propertyRefs", "upgradeProperties", "bestRarityProperties", "rarityLadder"
        ];

        /// <summary>How many records carry the tags key BESIDE the key their form is picked by. Every
        /// augment writes tags, so the tag-judged shape is the one nothing else claimed rather than one of
        /// three keys only one of which is ever there — which is the whole of what a one-axis contract can
        /// say here, and the number is held so the overlap cannot grow unnoticed.</summary>
        private const int AugmentsCarryingTagsBesideAStrongerKey = 45;

        /// <summary>Everywhere a loot position may name its drop; any one of them knowing the id makes the
        /// position real, which is why the field carries them all at once. The resources answer with the
        /// two sections holding things and not with the third: a material category is what a resource
        /// belongs to, and no kill has ever dropped one.</summary>
        private static readonly ReferenceTarget[] s_dropTargets =
        [
            ReferenceTarget.Whole(DataCatalog.EquipItems),
            ReferenceTarget.Whole(DataCatalog.Items),
            ReferenceTarget.Whole(DataCatalog.Recipes),
            new(DataCatalog.Resources, ResourcesCatalogDescriptor.UpgradeResourcesKey),
            new(DataCatalog.Resources, ResourcesCatalogDescriptor.CraftingResourcesKey)
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

            foreach ((string path, string catalog, string? section) in s_npcReferences)
                Points(Leaf(Locate(record, path)), path, catalog, section);

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

            foreach ((string path, string catalog, string? section) in s_npcBehaviorReferences)
                Points(Leaf(Locate(record, path)), path, catalog, section);

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
            CollectionAssert.AreEqual(new[] { ReferenceTarget.Whole(DataCatalog.Npc) }, individual.RefTargets.ToArray());
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
        /// The run counts how many records are written the scalar way, which the tool would hand back as raw
        /// json; that the shipped equipment writes none of them is <see cref="EquipItemDataFormAuditTests"/>'s
        /// to keep.
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

            foreach ((string section, string path, string catalog, string? into) in s_resourceReferences)
                Points(Leaf(Locate(Section(schema, section), path)), $"{section}.{path}", catalog, into);

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

        /// <summary>The schema of the abilities and the augments beside them: two sections of one file,
        /// each record found by its own id and both named and described in the localization by it. What the
        /// walk has to say is held as pairs of subject and keyword rather than whole sentences: which
        /// members earn a note is the fact being pinned, while the wording belongs to the library.</summary>
        [TestMethod]
        public void TheAbilitiesSchemaBuildsFromTheRealDtosWithTheNotesItsAugmentsWorkedOutMembersAreKnownFor()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Abilities));

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[] { AbilitiesCatalogDescriptor.AbilitiesKey, AbilitiesCatalogDescriptor.AugmentsKey },
                schema.Sections.Select(section => section.Key).ToArray());

            foreach (SectionSchema section in schema.Sections)
                Assert.AreEqual(AbilitiesCatalogDescriptor.IdField, section.Record.IdField, $"a record of '{section.Key}' is found by another field");

            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "a cast and an augment alike are named and described in the localization by their own id");
            Assert.AreEqual(AbilitiesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));
            CollectionAssert.AreEqual(
                new[] { AbilitiesCatalogDescriptor.FileName },
                ShippedFiles(DataCatalog.Abilities).Select(Path.GetFileNameWithoutExtension).ToArray(),
                "the catalog ships other files than the one its placement names");

            CollectionAssert.AreEqual(
                s_abilityFields,
                Section(schema, AbilitiesCatalogDescriptor.AbilitiesKey).Fields.Select(field => field.JsonName).ToArray(),
                "an ability is written with other keys than the ones the tool offers for it");
            CollectionAssert.AreEqual(
                s_augmentFields,
                Section(schema, AbilitiesCatalogDescriptor.AugmentsKey).Fields.Select(field => field.JsonName).ToArray(),
                "an augment is written with other keys than the ones the tool offers for it");

            Unexpected(builder.Reflection.Notes, s_allowedAbilityNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Abilities catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the ability parser resolves against another catalog, every name it parses into
        /// an enum, and every field naming something out of a registry of code rather than a catalog, said
        /// so in the schema. An unmarked one reads to the tool as free text; a field named like a reference
        /// and left unmarked reads as one nobody got to yet.</summary>
        [TestMethod]
        public void TheAbilitiesSchemaNamesTheReferencesChoicesAndRefusalsTheParserResolves()
        {
            CatalogSchema schema = Schema(DataCatalog.Abilities);

            foreach ((string section, string path, string catalog, string? into) in s_abilityReferences)
                Points(Leaf(Locate(Section(schema, section), path)), $"{section}.{path}", catalog, into);

            foreach ((string section, string path, Type members) in s_abilityChoices)
                Choice(Leaf(Locate(Section(schema, section), path)), members);

            RecordSchema augment = Section(schema, AbilitiesCatalogDescriptor.AugmentsKey);

            foreach (string path in s_augmentRefusals)
                Assert.IsTrue(Leaf(Locate(augment, path)).RefusedAsReference,
                    $"'{path}' names something out of a registry of code and neither points anywhere nor says it does not");

            FieldSchema pool = Locate(augment, EffectPoolField);
            Assert.AreEqual(FieldKind.Dictionary, pool.Kind, $"'{EffectPoolField}' is not a map");
            Points(
                pool.Key ?? throw new AssertFailedException($"'{EffectPoolField}' lets the author write its keys freely"),
                EffectPoolField,
                DataCatalog.Effects);
        }

        /// <summary>
        /// An augment answers the question of where it belongs in one of three ways, and the tool has to
        /// find all three under the augments section — that is what registering the shapes against the DTO
        /// buys. Which shape stands in a file is told by the key that is there, not by a value, and the
        /// shapes are offered narrowest first because that is the order the fitting rule reads them in.
        /// <para>The shipped file is walked with the tool's own reading: every record wears a shape, the
        /// shape it wears is the one the rule would read, and no record answers the question twice. How
        /// many carry the tags key beside a stronger one is a finding about the data — every augment writes
        /// tags — and the number is held rather than failed on.</para>
        /// </summary>
        [TestMethod]
        public void TheAbilitiesSchemaWearsOneBindingFormOnEveryAugmentTheShippedFileWrites()
        {
            CatalogSchema schema = Schema(DataCatalog.Abilities);
            RecordSchema record = Section(schema, AbilitiesCatalogDescriptor.AugmentsKey);
            VariantSet shapes = record.Variants ?? throw new AssertFailedException("an augment takes one shape only");

            Assert.IsNull(shapes.Discriminator, "the shapes are told apart by the key that is there, not by a value");
            CollectionAssert.AreEqual(
                new[]
                {
                    AbilitiesCatalogDescriptor.AbilityField,
                    AbilitiesCatalogDescriptor.AnyAbilityField,
                    AbilitiesCatalogDescriptor.TagsField
                },
                shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray(),
                "the shapes are no longer offered in the order the rule reads the declaration in");

            Assert.AreEqual(nameof(AugmentBoundToAbility), Shape(shapes, AbilitiesCatalogDescriptor.AbilityField).TypeName);
            Assert.AreEqual(nameof(AugmentForAnyAbility), Shape(shapes, AbilitiesCatalogDescriptor.AnyAbilityField).TypeName);
            Assert.AreEqual(nameof(AugmentBoundByTags), Shape(shapes, AbilitiesCatalogDescriptor.TagsField).TypeName);

            FieldSchema bound = Field(Shape(shapes, AbilitiesCatalogDescriptor.AbilityField), AbilitiesCatalogDescriptor.AbilityField);
            Assert.AreEqual(FieldKind.Reference, bound.Kind, "the shape binding an augment to one ability names no ability");
            Assert.IsFalse(bound.AllowEmpty, "the shape binding an augment to one ability may be written naming none");

            Dictionary<string, int> worn = shapes.Variants.ToDictionary(variant => variant.DiscriminatorValue, _ => 0, StringComparer.Ordinal);
            List<string> alongside = [];
            List<string> shapeless = [];
            int records = 0;

            foreach (JObject augment in SectionRecords(
                         JsonTreeDocument.Load(CatalogFile(schema, DataCatalog.Abilities)).Root,
                         AbilitiesCatalogDescriptor.AugmentsKey))
            {
                string id = Id(augment, AbilitiesCatalogDescriptor.IdField);
                records++;

                if (RecordTemplates.Worn(shapes, augment) is not { } form)
                {
                    shapeless.Add(id);
                    continue;
                }

                worn[form.DiscriminatorValue]++;
                Assert.AreEqual(Declares(augment), form.DiscriminatorValue,
                    $"'{id}' is worn in another shape than the one the fitting rule reads it in");

                string[] written = [.. shapes.Variants.Select(variant => variant.DiscriminatorValue).Where(augment.ContainsKey)];
                if (written.Length > 1) alongside.Add($"{id}: {string.Join(", ", written)} → {form.DiscriminatorValue}");
            }

            Assert.AreNotEqual(0, records, "the shipped file declares no augment, so the walk proves nothing");
            Assert.AreEqual(0, shapeless.Count,
                $"augments written in none of the shapes the schema lists: {string.Join(", ", shapeless)}");

            Report("Augments by the shape they are worn in",
                [.. worn.Select(shape => $"{shape.Key}: {shape.Value}")]);
            Report("Augments carrying the tags key beside the key their shape is picked by", alongside);

            Assert.AreEqual(AugmentsCarryingTagsBesideAStrongerKey, alongside.Count,
                "another number of augments writes tags beside a stronger key than the walk was pinned to");
        }

        /// <summary>The shipped file read back through the schema, the way the NPC one is: every key it
        /// writes is one the schema ranks, the shapes' keys included, and the canonical write loses
        /// nothing. The keys of a map are the exception the schema itself names — an augment's numbers are
        /// keyed by property names the author picks.</summary>
        [TestMethod]
        public void TheAbilitiesSchemaRanksEveryKeyTheShippedFileWrites()
        {
            List<string> unknown = UnknownKeys(Schema(DataCatalog.Abilities), DataCatalog.Abilities);

            Assert.AreEqual(0, unknown.Count,
                $"keys of {AbilitiesCatalogDescriptor.FileName} no field of the schema is written under:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", unknown)}");
        }

        /// <summary>
        /// Across every described catalog: a field written under a name ending in "id" either points into
        /// a catalog or says out loud that it does not. The one exception is the field a record IS found
        /// by, which names its own record rather than another.
        /// <para>Only what is written as a WORD is asked: a number, a flag or a nested object cannot name a
        /// record whatever it is called, and a faction's <c>canRaid</c> ends in those two letters by
        /// accident of English. Demanding a refusal of it would put markup on a field about which there was
        /// never a question.</para>
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
                        if (leaf.Kind != FieldKind.String) continue;

                        silent.Add($"{descriptor.Catalog}: {record.TypeName}.{field.JsonName}");
                    }

            Assert.AreEqual(0, silent.Count,
                $"fields named like an id that neither point anywhere nor say they do not: {string.Join(", ", silent.Distinct().Order(StringComparer.Ordinal))}");
        }

        /// <summary>
        /// Across every described catalog: a reference narrowed to a section names a section its target
        /// catalog actually writes. Nothing else can see this — a schema is built one catalog at a time,
        /// and the section a field names belongs to another one — so a misspelt narrowing would answer
        /// every id written in that field with "nothing is named this", and only in the running editor.
        /// <para>A section named in a catalog no descriptor covers is passed over: what that catalog is
        /// written in is not this build's to know.</para>
        /// </summary>
        [TestMethod]
        public void EverySectionAReferenceNamesIsOneItsCatalogWrites()
        {
            Dictionary<string, CatalogSchema> described =
                CatalogDescriptors.All.ToDictionary(descriptor => descriptor.Catalog, descriptor => Schema(descriptor.Catalog), StringComparer.Ordinal);

            List<string> lost = [];

            foreach ((string catalog, CatalogSchema schema) in described)
                foreach (RecordSchema record in Records(schema))
                    foreach (FieldSchema field in record.Fields)
                        foreach (ReferenceTarget target in Narrowed(field))
                        {
                            if (!described.TryGetValue(target.Catalog, out CatalogSchema? into)) continue;
                            if (into.Sections.Any(section => section.Key == target.Section)) continue;

                            lost.Add($"{catalog}: {record.TypeName}.{field.JsonName} → {target}");
                        }

            Assert.AreEqual(0, lost.Count,
                $"references narrowed to a section their catalog does not write: {string.Join(", ", lost.Distinct().Order(StringComparer.Ordinal))}");
        }

        /// <summary>
        /// A record taking several shapes says where a field points TWICE — once on the record every
        /// reader parses into, once on the form the inspector draws — and the two have to agree. The form
        /// is what the author is actually offered: the panel builds its rows out of the shape a record
        /// wears, so a narrowing left on the base record alone never reaches him, and one left on the form
        /// alone never reaches whatever reads the record whole.
        /// </summary>
        [TestMethod]
        public void EveryShapeOfARecordPointsWhereTheRecordItselfDoes()
        {
            List<string> apart = [];
            int compared = 0;

            foreach (ICatalogDescriptor descriptor in CatalogDescriptors.All)
                foreach (RecordSchema record in Records(Schema(descriptor.Catalog)))
                {
                    if (record.Variants is not { } shapes) continue;

                    foreach (VariantSchema shape in shapes.Variants)
                        foreach (FieldSchema field in shape.Record.Fields)
                        {
                            if (record.Fields.FirstOrDefault(own => own.JsonName == field.JsonName) is not { } own) continue;

                            compared++;

                            if (Leaf(field).RefTargets == Leaf(own).RefTargets) continue;

                            apart.Add($"{descriptor.Catalog}: {shape.Record.TypeName}.{field.JsonName} " +
                                      $"[{string.Join(", ", Leaf(field).RefTargets)}] against {record.TypeName} [{string.Join(", ", Leaf(own).RefTargets)}]");
                        }
                }

            Assert.AreNotEqual(0, compared, "no shape writes a key its record writes too — the check is checking nothing");
            Assert.AreEqual(0, apart.Count,
                $"a shape and the record it stands for point at different things:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", apart.Distinct().Order(StringComparer.Ordinal))}");
        }

        /// <summary>Every target a field points into that names a section — the field's own, and the ones
        /// its elements and its keys carry, which is where the markup of a list or a map travels to.</summary>
        private static IEnumerable<ReferenceTarget> Narrowed(FieldSchema field)
        {
            foreach (FieldSchema part in new[] { Leaf(field), Leaf(field).Key }.OfType<FieldSchema>())
                foreach (ReferenceTarget target in part.RefTargets)
                    if (target.Section is not null)
                        yield return target;
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

        /// <summary>That a field names a record of a catalog, and of the one section of it the parser
        /// resolves the id against: an unmarked field reads to the tool as free text, and one narrowed to
        /// nothing where the game narrows offers ids the game will drop. A caller naming no section is
        /// saying the whole catalog answers, which is checked just as closely.</summary>
        private static void Points(FieldSchema field, string path, string catalog, string? section = null)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{path}' is not a reference");

            ReferenceTarget target = new(catalog, section);
            CollectionAssert.Contains(field.RefTargets.ToArray(), target, $"'{path}' does not point into {target}");
        }

        /// <summary>The keys one map is written with, wherever in a document it stands. The finding of it
        /// is <see cref="Under"/>'s, which is the same walk: what is written under a name and what that
        /// something is KEYED by are one question asked twice.</summary>
        private static IEnumerable<string> Keys(JToken token, string map) =>
            Under(token, map).OfType<JObject>().SelectMany(keyed => keyed.Properties()).Select(key => key.Name);

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

        /// <summary>Which of the three the fitting rule reads a shipped augment as, in the rule's own
        /// order: a claim on the whole book settles it, a named ability is otherwise the whole answer, and
        /// everything else is tag work. Written out here rather than asked of the rule so that the shapes
        /// are held against what the game DOES with the file and not against another reading of it.</summary>
        private static string Declares(JObject augment)
        {
            if (augment.Value<bool?>(AbilitiesCatalogDescriptor.AnyAbilityField) == true) return AbilitiesCatalogDescriptor.AnyAbilityField;

            return string.IsNullOrWhiteSpace(augment.Value<string>(AbilitiesCatalogDescriptor.AbilityField))
                ? AbilitiesCatalogDescriptor.TagsField
                : AbilitiesCatalogDescriptor.AbilityField;
        }

        private static RecordSchema Shape(VariantSet shapes, string key) =>
            shapes.Variants.FirstOrDefault(variant => variant.DiscriminatorValue == key)?.Record
            ?? throw new AssertFailedException($"no shape is picked by '{key}'.");

        private static FieldSchema SectionKey(CatalogSchema schema, string section) =>
            Field(Section(schema, section), LootTablesCatalogDescriptor.KeyField);

        /// <summary>That a field names something to drop, out of everywhere droppable things live.</summary>
        private static void NamesADrop(FieldSchema field, string what)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"{what} does not name anything");
            Assert.IsFalse(field.AllowEmpty, $"{what} may be left empty, which prices a seat that drops nothing");
            CollectionAssert.AreEquivalent(
                s_dropTargets,
                field.RefTargets.ToArray(),
                $"{what} points somewhere other than where droppable things are written: {string.Join(", ", field.RefTargets)}");
        }

        /// <summary>That a field names something to hand over, out of everywhere an item is written.</summary>
        private static void NamesAHandOut(FieldSchema field, string what)
        {
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"{what} does not name anything");
            Assert.IsFalse(field.AllowEmpty, $"{what} may be left empty, which promises a reward of nothing");
            CollectionAssert.AreEquivalent(
                s_handedOutTargets,
                field.RefTargets.ToArray(),
                $"{what} points somewhere other than where items are written: {string.Join(", ", field.RefTargets)}");
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

        /// <summary>How many files each of the two narrative catalogs ships. Neither placement rule names
        /// a file — both leave it to the tool — and the catalogs are written one record apiece, so a file
        /// is a dialogue and a file is a quest. The counts are held here to say what the walks were run
        /// over; <see cref="EveryNarrativeFileHoldsTheOneRecordItIsNamedAfter"/> holds the rest of it.</summary>
        private const int ShippedDialogueCount = 9;

        private const int ShippedQuestCount = 5;

        /// <summary>What a dialogue writes its opening rules, its nodes and the parts of a node under. No
        /// descriptor names them — they are the DTOs' own fields, the way a table's tiers are — so the
        /// walks spell them out.</summary>
        private const string EntryRulesField = "entryRules";

        private const string NodesField = "nodes";

        private const string LinesField = "lines";

        private const string OptionsField = "options";

        private const string SpeakerField = "speaker";

        /// <summary>What a line and an option carry the localization key they are read under.</summary>
        private const string TextKeyField = "key";

        /// <summary>The three routes of a dialogue: the node an opening rule leads to, the node an option
        /// leads to, and the node a failed speech check falls back on.</summary>
        private const string EntryNodeField = "node";

        private const string NextField = "next";

        private const string SpeechCheckField = "speechCheck";

        private const string FailNextField = "failNext";

        /// <summary>What a record nested inside another names ITSELF by — a node in its dialogue, a stage
        /// in its quest. Not the id of anything a catalog answers for.</summary>
        private const string LocalIdField = "id";

        /// <summary>What a quest writes its stages, the objectives of a stage, its routes out and its
        /// ending under.</summary>
        private const string StagesField = "stages";

        private const string ObjectivesField = "objectives";

        private const string TransitionsField = "transitions";

        /// <summary>The stage one route leads to.</summary>
        private const string TransitionTargetField = "to";

        private const string OutcomeField = "outcome";

        private const string RewardsField = "rewards";

        private const string ItemIdField = "itemId";

        private const string PriorityField = "priority";

        /// <summary>Everywhere a quest may name a hand-out out of — the same targets the item vocabulary of
        /// the narrative reads an id from. Any one of them knowing the id makes the reward real; the
        /// resources answer with the two sections holding things, never with the categories beside them.</summary>
        private static readonly ReferenceTarget[] s_handedOutTargets =
        [
            ReferenceTarget.Whole(DataCatalog.EquipItems),
            ReferenceTarget.Whole(DataCatalog.Items),
            ReferenceTarget.Whole(DataCatalog.Recipes),
            ReferenceTarget.Whole(DataCatalog.Ornaments),
            new(DataCatalog.Resources, ResourcesCatalogDescriptor.UpgradeResourcesKey),
            new(DataCatalog.Resources, ResourcesCatalogDescriptor.CraftingResourcesKey)
        ];

        /// <summary>Every id the dialogue parser resolves against another catalog, addressed the way the
        /// file writes it. A dialogue names the npc definition that speaks it and nothing else: everything
        /// else it points at is a condition's or an action's business.</summary>
        private static readonly (string Path, string Catalog)[] s_dialogueReferences =
        [
            (DialoguesCatalogDescriptor.IdField, DataCatalog.Npc),
        ];

        /// <summary>Every field the dialogue parser turns into an enum member. Who speaks a line is parsed
        /// strictly, so a word naming no member drops the whole dialogue at load.</summary>
        private static readonly (string Path, Type Members)[] s_dialogueChoices =
        [
            ($"{NodesField}{PathSeparator}{LinesField}{PathSeparator}{SpeakerField}", typeof(Core.Narrative.Dialogues.DialogueSpeaker)),
        ];

        /// <summary>Every string a dialogue writes that is a localization key rather than text to read.
        /// Both are written out in full: nothing about a dialogue is worded from an id of its own.</summary>
        private static readonly string[] s_dialogueTextKeys =
        [
            $"{NodesField}{PathSeparator}{LinesField}{PathSeparator}{TextKeyField}",
            $"{NodesField}{PathSeparator}{OptionsField}{PathSeparator}{TextKeyField}",
        ];

        /// <summary>Every string of a dialogue that reads like a reference and is none: the ids naming a
        /// node and an option inside their own record, and the three routes between the nodes. The
        /// contract has no way of saying "a node of THIS dialogue", so the records say they point at no
        /// catalog and <see cref="EveryDialogueRouteNamesANodeOfItsOwnDialogue"/> holds the files to the
        /// rest of it.</summary>
        private static readonly string[] s_dialogueRefusals =
        [
            $"{NodesField}{PathSeparator}{LocalIdField}",
            $"{NodesField}{PathSeparator}{OptionsField}{PathSeparator}{LocalIdField}",
            $"{EntryRulesField}{PathSeparator}{EntryNodeField}",
            $"{NodesField}{PathSeparator}{OptionsField}{PathSeparator}{NextField}",
            $"{NodesField}{PathSeparator}{OptionsField}{PathSeparator}{SpeechCheckField}{PathSeparator}{FailNextField}",
        ];

        /// <summary>Everything of a dialogue the schema keeps as raw json: the vocabulary of conditions and
        /// actions is the narrative factories' own, and no record of this catalog describes it.</summary>
        private static readonly string[] s_dialogueFreeFormFields =
        [
            "conditions", "onEnter", "visibleConditions", "enabledConditions", "actions", "failActions"
        ];

        /// <summary>Every id the quest parser resolves against another catalog, addressed the way the file
        /// writes it. The giver and everyone who may take the quest back are npc definitions; a reward
        /// names something to hand over out of every catalog holding one.</summary>
        private static readonly (string Path, string Catalog)[] s_questReferences =
        [
            ("giverNpcId", DataCatalog.Npc),
            ("turnInNpcIds", DataCatalog.Npc),
        ];

        /// <summary>The two addresses a reward item is written at: the quest-wide list, and the list of an
        /// ending it may stop on. One record stands under both, and both are walked so that a second
        /// reward list added later cannot arrive unmarked.</summary>
        private static readonly string[] s_questRewardItems =
        [
            $"{RewardsField}{PathSeparator}{ItemsField}{PathSeparator}{ItemIdField}",
            $"{StagesField}{PathSeparator}{OutcomeField}{PathSeparator}{RewardsField}{PathSeparator}{ItemsField}{PathSeparator}{ItemIdField}",
        ];

        /// <summary>Every field the quest parser turns into an enum member. Both are parsed strictly: a
        /// word naming no member drops the whole quest at load.</summary>
        private static readonly (string Path, Type Members)[] s_questChoices =
        [
            ("faction", typeof(Fractions)),
            ("declinePolicy", typeof(Core.Narrative.Quests.DeclinePolicy)),
        ];

        /// <summary>Every string of a quest that reads like a reference and is none: the ids naming a
        /// stage, an objective and an ending inside their own quest, and the route out of a stage. The
        /// contract has no way of saying "a stage of THIS quest", so the records say they point at no
        /// catalog and <see cref="EveryQuestRouteNamesAStageOfItsOwnQuest"/> holds the files to the rest
        /// of it.</summary>
        private static readonly string[] s_questRefusals =
        [
            $"{StagesField}{PathSeparator}{LocalIdField}",
            $"{StagesField}{PathSeparator}{ObjectivesField}{PathSeparator}{LocalIdField}",
            $"{StagesField}{PathSeparator}{OutcomeField}{PathSeparator}{LocalIdField}",
            $"{StagesField}{PathSeparator}{TransitionsField}{PathSeparator}{TransitionTargetField}",
        ];

        /// <summary>Everything of a quest the schema keeps as raw json — the same vocabulary the dialogues
        /// are written with, read at every door a quest opens.</summary>
        private static readonly string[] s_questFreeFormFields =
        [
            "acceptConditions", "onAccept", "onDecline", "onFail", "condition", "conditions", "onEnter",
            "onComplete", "actions"
        ];

        /// <summary>A dialogue whose option leads to a node nobody wrote, which is what the walk over the
        /// shipped files exists to find.</summary>
        private const string ForgedDialogueJson = """
        {
          "dialogues": [
            {
              "npcId": "Npc_Forged",
              "entryRules": [ { "priority": 0, "conditions": [], "node": "Greeting" } ],
              "nodes": [
                {
                  "id": "Greeting",
                  "lines": [ { "speaker": "Npc", "key": "Dlg_Forged" } ],
                  "options": [ { "id": "Leave", "key": "Dlg_Forged_Leave", "next": "Farewell" } ]
                }
              ]
            }
          ]
        }
        """;

        /// <summary>Two dialogues written into one file, which is what the walk over the shipped files
        /// exists to find: the catalog is split one record to a file named after it.</summary>
        private const string ForgedSharedFileJson = """
        {
          "dialogues": [
            {
              "npcId": "Npc_Forged",
              "entryRules": [ { "priority": 0, "conditions": [], "node": "Greeting" } ],
              "nodes": [ { "id": "Greeting", "lines": [], "options": [ { "id": "Leave", "key": "Dlg_Opt_Leave" } ] } ]
            },
            {
              "npcId": "Npc_Forged_Second",
              "entryRules": [ { "priority": 0, "conditions": [], "node": "Greeting" } ],
              "nodes": [ { "id": "Greeting", "lines": [], "options": [ { "id": "Leave", "key": "Dlg_Opt_Leave" } ] } ]
            }
          ]
        }
        """;

        /// <summary>A quest whose route leads to a stage nobody wrote.</summary>
        private const string ForgedQuestRouteJson = """
        {
          "quests": [
            {
              "id": "Quest_Forged",
              "stages": [
                {
                  "id": "Hunt",
                  "objectives": [ { "id": "Kill", "counter": { "key": "Kill_Count:Npc_Wolf", "amount": 1 } } ],
                  "transitions": [ { "to": "Reward", "conditions": [] } ]
                }
              ]
            }
          ]
        }
        """;

        /// <summary>The schema of the dialogues, built from the shipped DTOs. A dialogue is found by the
        /// npc that speaks it rather than by an id of its own, nothing about it is worded from that id, and
        /// the file a new one goes to is the tool's to choose. Both reports gather what neither reflection
        /// nor the assembled parts could vouch for.</summary>
        [TestMethod]
        public void TheDialoguesSchemaBuildsFromTheRealDtosWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Dialogues));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(DialoguesCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(DialoguesCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count,
                "a dialogue words nothing from its own id: every line and option carries the key it is read under");

            Assert.IsInstanceOfType<FreeFilePlacement>(schema.Placement, "the dialogues name a file of their own");
            Assert.AreEqual(
                ShippedDialogueCount,
                ShippedFiles(DataCatalog.Dialogues).Count,
                "the catalog ships another number of files than the dialogues known today");

            CollectionAssert.AreEquivalent(
                s_dialogueFreeFormFields,
                FreeFormFields(schema).ToArray(),
                "a dialogue keeps other keys as raw json than the conditions and actions it is written with");

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the dialogue DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Dialogues catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the dialogue parser resolves against another catalog, every name it parses
        /// into an enum, every string it reads as a localization key, and every string that reads like a
        /// reference while naming something inside its own record, said so in the schema. An unmarked one
        /// reads to the tool as free text: the author types a name nothing answers, and the miss drops the
        /// whole dialogue at load.</summary>
        [TestMethod]
        public void TheDialoguesSchemaNamesTheReferencesChoicesKeysAndRefusalsTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.Dialogues).Sections[0].Record;

            foreach ((string path, string catalog) in s_dialogueReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach ((string path, Type members) in s_dialogueChoices) Choice(Leaf(Locate(record, path)), members);

            foreach (string path in s_dialogueTextKeys)
                Assert.AreEqual(FieldKind.LocalizedKey, Leaf(Locate(record, path)).Kind,
                    $"'{path}' is drawn as text to read instead of the key it holds");

            foreach (string path in s_dialogueRefusals)
                Assert.IsTrue(Leaf(Locate(record, path)).RefusedAsReference,
                    $"'{path}' names something inside its own dialogue and neither points anywhere nor says it does not");
        }

        /// <summary>The shipped file read back through the schema: every key it writes is one the schema
        /// ranks, bar what stands inside a condition or an action — the vocabulary is the narrative
        /// factories' own and reaches the tool from them, not from this catalog.</summary>
        [TestMethod]
        public void TheDialoguesSchemaRanksEveryKeyTheShippedFilesWriteOutsideAConditionOrAnAction()
        {
            InsideFreeFormOnly(Schema(DataCatalog.Dialogues), DataCatalog.Dialogues);
        }

        /// <summary>The schema of the quests, built from the shipped DTOs. A quest is found by its own id
        /// and named and described in the localization by it, and the file a new one goes to is the tool's
        /// to choose. Both reports gather what neither reflection nor the assembled parts could vouch
        /// for.</summary>
        [TestMethod]
        public void TheQuestsSchemaBuildsFromTheRealDtosWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Quests));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(QuestsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(QuestsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "a quest is named and described in the journal by its own id");

            Assert.IsInstanceOfType<FreeFilePlacement>(schema.Placement, "the quests name a file of their own");
            Assert.AreEqual(
                ShippedQuestCount,
                ShippedFiles(DataCatalog.Quests).Count,
                "the catalog ships another number of files than the quests known today");

            CollectionAssert.AreEquivalent(
                s_questFreeFormFields,
                FreeFormFields(schema).ToArray(),
                "a quest keeps other keys as raw json than the conditions and actions it is written with");

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the quest DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Quests catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the quest parser resolves against another catalog, every name it parses into
        /// an enum, and every string that reads like a reference while naming something inside its own
        /// quest, said so in the schema. A reward names its item out of everywhere an item is written and
        /// out of nowhere else, the way the GiveItem action does.</summary>
        [TestMethod]
        public void TheQuestsSchemaNamesTheReferencesChoicesAndRefusalsTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.Quests).Sections[0].Record;

            foreach ((string path, string catalog) in s_questReferences) Points(Leaf(Locate(record, path)), path, catalog);

            foreach (string path in s_questRewardItems)
                NamesAHandOut(Leaf(Locate(record, path)), $"the reward at {path}");

            foreach ((string path, Type members) in s_questChoices) Choice(Leaf(Locate(record, path)), members);

            foreach (string path in s_questRefusals)
                Assert.IsTrue(Leaf(Locate(record, path)).RefusedAsReference,
                    $"'{path}' names something inside its own quest and neither points anywhere nor says it does not");
        }

        /// <summary>
        /// The four ways an item is named — the markup of a quest reward, the two actions handing one over
        /// and the condition asking after one — say the same thing, because one list in the game states it
        /// and all four read that list. A half naming its own catalogs would offer the author ids the other
        /// three call broken.
        /// <para>The loot tables are held beside them and differ by exactly one target: an ornament comes
        /// through a quest and through nothing else, so no seat at a table may price one.</para>
        /// </summary>
        [TestMethod]
        public void EveryWayAnItemIsNamed_PointsWhereTheItemVocabularySays()
        {
            ReferenceTarget[] vocabulary = Stated(ItemReference.Targets);

            CollectionAssert.AreEquivalent(s_handedOutTargets, vocabulary,
                $"the item vocabulary of the game points elsewhere than this test pins: {string.Join(", ", vocabulary.Select(target => target.ToString()))}");

            (string Type, NarrativeRecordSpec Spec)[] readers =
            [
                ("GiveItem", new GiveItemActionFactory(Mock.Of<IItemMinter>(), Mock.Of<IInventory>()).Parameters),
                ("TakeItem", new TakeItemActionFactory(Mock.Of<IInventory>()).Parameters),
                ("HasItem", new HasItemConditionFactory(Mock.Of<IInventory>()).Parameters),
            ];

            foreach ((string type, NarrativeRecordSpec spec) in readers)
            {
                NarrativeParameterSpec item = spec.Parameters.Single(parameter => parameter.JsonName == ItemReference.Key);

                CollectionAssert.AreEquivalent(s_handedOutTargets, Stated(item.Targets),
                    $"'{type}' names its item somewhere other than where the item vocabulary points");
            }

            CollectionAssert.AreEquivalent(
                s_handedOutTargets.Where(target => target.Catalog != DataCatalog.Ornaments).ToArray(),
                s_dropTargets,
                "a loot seat and a hand-out differ by something other than the ornaments, which quests alone give out");
        }

        /// <summary>The game's own targets, as the authoring tool states them. The two vocabularies are
        /// separate types on purpose — the game names no tool type — so the crossing is spelled out.</summary>
        private static ReferenceTarget[] Stated(IReadOnlyList<NarrativeReferenceTarget> targets) =>
            [.. targets.Select(target => new ReferenceTarget(target.Catalog, target.Section))];

        /// <summary>The shipped file read back through the schema, the way the dialogues are: everything
        /// outside a condition or an action is ranked.</summary>
        [TestMethod]
        public void TheQuestsSchemaRanksEveryKeyTheShippedFilesWriteOutsideAConditionOrAnAction()
        {
            InsideFreeFormOnly(Schema(DataCatalog.Quests), DataCatalog.Quests);
        }

        /// <summary>
        /// Both narrative catalogs are written one record to a file, named by the id the record is found
        /// under: a dialogue under the npc that speaks it, a quest under its own id. The loader takes the
        /// whole folder and never reads a file name, so the rule is the author's alone — and it is the
        /// reason he opens the conversation he means instead of scrolling one file holding all of them.
        /// </summary>
        [TestMethod]
        public void EveryNarrativeFileHoldsTheOneRecordItIsNamedAfter()
        {
            List<string> misplaced =
            [
                .. ShippedFiles(DataCatalog.Dialogues)
                    .SelectMany(file => RecordsOutOfPlace(file, DialoguesCatalogDescriptor.RecordsKey, DialoguesCatalogDescriptor.IdField)),
                .. ShippedFiles(DataCatalog.Quests)
                    .SelectMany(file => RecordsOutOfPlace(file, QuestsCatalogDescriptor.RecordsKey, QuestsCatalogDescriptor.IdField))
            ];

            Report("Narrative files walked", [$"{ShippedDialogueCount} dialogue(s)", $"{ShippedQuestCount} quest(s)"]);

            Assert.AreEqual(0, misplaced.Count,
                $"narrative files holding other than the one record they are named after: {string.Join(", ", misplaced)}");
        }

        /// <summary>The mutation the walk exists for: two dialogues written into one file is the catalog
        /// growing a second monolith back, which the loader would read without a word.</summary>
        [TestMethod]
        public void ANarrativeFileHoldingTwoRecords_IsCaught()
        {
            List<string> misplaced = RecordsOutOfPlace(
                JsonTreeDocument.Parse(ForgedSharedFileJson).Root,
                "Npc_Forged",
                DialoguesCatalogDescriptor.RecordsKey,
                DialoguesCatalogDescriptor.IdField);

            Assert.AreEqual(1, misplaced.Count, "a file holding two dialogues went unnoticed");
        }

        /// <summary>What a narrative file has to say about itself: another number of records than the one
        /// it is named after, or a record whose id is not the name of the file it was written into.</summary>
        private static List<string> RecordsOutOfPlace(string file, string section, string idField) =>
            RecordsOutOfPlace(JsonTreeDocument.Load(file).Root, Path.GetFileNameWithoutExtension(file), section, idField);

        private static List<string> RecordsOutOfPlace(JToken root, string name, string section, string idField)
        {
            List<JObject> records = SectionRecords(root, section);

            if (records.Count != 1) return [$"{name} holds {records.Count} record(s)"];

            string id = Id(records[0], idField);

            return string.Equals(id, name, StringComparison.Ordinal) ? [] : [$"{name} holds '{id}'"];
        }

        /// <summary>
        /// Every route of every shipped dialogue leads to a node of the SAME dialogue: the opening rules,
        /// the options and the fallback of a speech check. The contract cannot say it — a node is not a
        /// record of any catalog — and the provider drops a dialogue whose route dangles, one npc falling
        /// silent with nothing but a line in the log, so the files are held to it here.
        /// </summary>
        [TestMethod]
        public void EveryDialogueRouteNamesANodeOfItsOwnDialogue()
        {
            List<string> dangling = [];
            int routes = 0;

            foreach (string file in ShippedFiles(DataCatalog.Dialogues))
            {
                (List<string> broken, int walked) = DialogueRoutes(JsonTreeDocument.Load(file).Root);
                dangling.AddRange(broken.Select(route => $"{Path.GetFileName(file)} {route}"));
                routes += walked;
            }

            Assert.AreNotEqual(0, routes, "the shipped dialogues hold no routes at all — the walk proves nothing");
            Report("Routes between the nodes of the shipped dialogues", [$"{routes} walked"]);

            Assert.AreEqual(0, dangling.Count,
                $"dialogue routes leading to a node nobody wrote: {string.Join(", ", dangling)}");
        }

        /// <summary>The mutation the walk exists for, run against the same reading: a dialogue whose
        /// option leads nowhere is the one the provider would drop in silence.</summary>
        [TestMethod]
        public void ADialogueRoutePointingAtAMissingNode_IsCaught()
        {
            (List<string> dangling, int routes) = DialogueRoutes(JsonTreeDocument.Parse(ForgedDialogueJson).Root);

            Assert.AreEqual(2, routes, "the forged dialogue writes another number of routes than the walk read");
            Assert.AreEqual(1, dangling.Count, $"a dialogue leading to a node nobody wrote went unnoticed: {string.Join(", ", dangling)}");
        }

        /// <summary>
        /// Every route out of every stage of every shipped quest leads to a stage of the SAME quest. The
        /// contract cannot say it — a stage is not a record of any catalog — and the provider drops a quest
        /// whose route dangles. The shipped quests are linear today and write no routes at all, so the
        /// stages they do write are counted and the number of routes is reported rather than demanded.
        /// </summary>
        [TestMethod]
        public void EveryQuestRouteNamesAStageOfItsOwnQuest()
        {
            List<string> dangling = [];
            int routes = 0;
            int stages = 0;

            foreach (string file in ShippedFiles(DataCatalog.Quests))
            {
                (List<string> broken, int walked, int written) = QuestRoutes(JsonTreeDocument.Load(file).Root);
                dangling.AddRange(broken.Select(route => $"{Path.GetFileName(file)} {route}"));
                routes += walked;
                stages += written;
            }

            Assert.AreNotEqual(0, stages, "the shipped quests hold no stages at all — the walk proves nothing");
            Report("Stages and routes of the shipped quests", [$"{stages} stages", $"{routes} routes"]);

            Assert.AreEqual(0, dangling.Count,
                $"quest routes leading to a stage nobody wrote: {string.Join(", ", dangling)}");
        }

        /// <summary>The mutation the walk exists for: a branch written to a stage nobody wrote is the one
        /// the provider would drop in silence.</summary>
        [TestMethod]
        public void AQuestRoutePointingAtAMissingStage_IsCaught()
        {
            (List<string> dangling, int routes, int stages) = QuestRoutes(JsonTreeDocument.Parse(ForgedQuestRouteJson).Root);

            Assert.AreEqual(1, stages, "the forged quest writes another number of stages than the walk read");
            Assert.AreEqual(1, routes, "the forged quest writes another number of routes than the walk read");
            Assert.AreEqual(1, dangling.Count, $"a quest leading to a stage nobody wrote went unnoticed: {string.Join(", ", dangling)}");
        }

        /// <summary>Json names of every field the schema keeps as raw json, read off the schema rather than
        /// listed: what stands inside one is the vocabulary's own business, and the walk stops there.</summary>
        private static HashSet<string> FreeFormFields(CatalogSchema schema) =>
        [
            .. Records(schema)
                .SelectMany(record => record.Fields)
                .Where(field => Leaf(field).Kind == FieldKind.Any)
                .Select(field => field.JsonName)
        ];

        /// <summary>That the keys the schema could not place all sit inside a field it keeps as raw json.
        /// A key ANYWHERE else is a field the tool would not let the author touch.</summary>
        private static void InsideFreeFormOnly(CatalogSchema schema, string catalog)
        {
            HashSet<string> freeForm = FreeFormFields(schema);
            Assert.AreNotEqual(0, freeForm.Count, $"no field of the {catalog} catalog is raw json — the check is checking nothing");

            List<string> unknown = UnknownKeys(schema, catalog);
            List<string> elsewhere =
            [
                .. unknown.Where(address => !freeForm.Any(field =>
                    address.Contains($"{PointerSeparator}{field}{PointerSeparator}", StringComparison.Ordinal)))
            ];

            Report($"Keys the {catalog} files write inside a condition or an action",
                [.. unknown.Select(LastKey).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);

            Assert.AreEqual(0, elsewhere.Count,
                $"keys of the {catalog} files outside a condition or an action that no field of the schema is written under: {string.Join(", ", elsewhere)}");
        }

        /// <summary>The routes of every dialogue of one document that lead to a node nobody wrote, and how
        /// many routes were read at all.</summary>
        private static (List<string> Dangling, int Routes) DialogueRoutes(JToken root)
        {
            List<string> dangling = [];
            int routes = 0;

            foreach (JObject dialogue in SectionRecords(root, DialoguesCatalogDescriptor.RecordsKey))
            {
                HashSet<string> nodes = [.. Nodes(dialogue).Select(node => Id(node, LocalIdField))];

                foreach ((string at, string target) in DialogueTargets(dialogue))
                {
                    routes++;

                    if (!nodes.Contains(target))
                        dangling.Add($"{Id(dialogue, DialoguesCatalogDescriptor.IdField)} {at} → '{target}'");
                }
            }

            return (dangling, routes);
        }

        private static IEnumerable<JObject> Nodes(JObject dialogue) =>
            (dialogue[NodesField] as JArray ?? []).OfType<JObject>();

        /// <summary>Every node one dialogue names, with where it names it: an opening rule, an option, and
        /// the fallback of an option's speech check. A route written as nothing at all ends the
        /// conversation and names no node.</summary>
        private static IEnumerable<(string At, string Target)> DialogueTargets(JObject dialogue)
        {
            foreach (JObject rule in (dialogue[EntryRulesField] as JArray ?? []).OfType<JObject>())
                if (rule[EntryNodeField]?.Value<string>() is { } opening)
                    yield return ($"{EntryRulesField}({rule[PriorityField]})", opening);

            foreach (JObject node in Nodes(dialogue))
                foreach (JObject option in (node[OptionsField] as JArray ?? []).OfType<JObject>())
                {
                    string at = $"{Id(node, LocalIdField)}/{Id(option, LocalIdField)}";

                    if (option[NextField]?.Value<string>() is { } next) yield return (at, next);

                    if (option[SpeechCheckField]?[FailNextField]?.Value<string>() is { } failNext)
                        yield return ($"{at} {SpeechCheckField}", failNext);
                }
        }

        /// <summary>The routes of every quest of one document that lead to a stage nobody wrote, how many
        /// routes were read, and how many stages they were read against.</summary>
        private static (List<string> Dangling, int Routes, int Stages) QuestRoutes(JToken root)
        {
            List<string> dangling = [];
            int routes = 0;
            int written = 0;

            foreach (JObject quest in SectionRecords(root, QuestsCatalogDescriptor.RecordsKey))
            {
                List<JObject> stages = [.. (quest[StagesField] as JArray ?? []).OfType<JObject>()];
                HashSet<string> ids = [.. stages.Select(stage => Id(stage, LocalIdField))];
                written += stages.Count;

                foreach (JObject stage in stages)
                    foreach (JObject transition in (stage[TransitionsField] as JArray ?? []).OfType<JObject>())
                    {
                        routes++;
                        string target = transition[TransitionTargetField]?.Value<string>() ?? string.Empty;

                        if (!ids.Contains(target))
                            dangling.Add($"{Id(quest, QuestsCatalogDescriptor.IdField)} {Id(stage, LocalIdField)} → '{target}'");
                    }
            }

            return (dangling, routes, written);
        }

        /// <summary>What the reflector may still have to say about the DTOs of the settings documents —
        /// one list for all of them, because it is empty for all of them: a settings document is plain
        /// properties the walk reads whole. A note appearing here is either a DTO to fix or a fact to
        /// write down — never something to silence by widening the check.</summary>
        private static readonly (string About, string Word)[] s_settingsNotes = [];

        /// <summary>Every field of a settings document the parser turns into an enum member, addressed
        /// the way the file writes it. The tool offers the members; a field the markup missed would take
        /// any word and fail at load instead.</summary>
        private static readonly (string Catalog, string Path, Type Members)[] s_settingsChoices =
        [
            (DataCatalog.CombatRules,
                $"{CombatRulesCatalogDescriptor.ControlResistanceField}{PathSeparator}{CombatRulesCatalogDescriptor.HardControlStatusesField}",
                typeof(StatusEffects)),
            (DataCatalog.CombatRules,
                $"{CombatRulesCatalogDescriptor.ControlResistanceField}{PathSeparator}{CombatRulesCatalogDescriptor.AppliesToField}",
                typeof(EntityType)),
            (DataCatalog.Formatting, FormattingCatalogDescriptor.IdField, typeof(EntityParameter)),
            (DataCatalog.Formatting, FormattingCatalogDescriptor.UnitField, typeof(ParameterUnit)),
        ];

        /// <summary>Every map of a settings document whose KEYS are members of an enum rather than words
        /// the author picks. None of the readers takes a word it does not know: the budgets and the price
        /// multipliers throw at load, the mastery's rarity weights and the player's baseline drop the
        /// entry with a report — a rarity paying nothing and a stat worth zero — and a mode the exp
        /// rewards do not name pays the FULL rate rather than none. Either way the tool has to offer the
        /// members rather than a text box.</summary>
        private static readonly (string Catalog, string Path, Type Members)[] s_settingsMapKeys =
        [
            (DataCatalog.LootConfiguration, LootConfigurationCatalogDescriptor.BaseBudgetField, typeof(EntityType)),
            (DataCatalog.LootConfiguration, LootConfigurationCatalogDescriptor.RarityMultipliersField, typeof(Rarity)),
            (DataCatalog.Trade, TradeCatalogDescriptor.RarityMultipliersField, typeof(Rarity)),
            (DataCatalog.CraftingMastery, CraftingMasteryCatalogDescriptor.RarityWeightsField, typeof(Rarity)),
            (DataCatalog.CraftingMastery,
                $"{CraftingMasteryCatalogDescriptor.ExpRewardsField}{PathSeparator}{CraftingMasteryCatalogDescriptor.ExpByRarityField}",
                typeof(Rarity)),
            (DataCatalog.CraftingMastery,
                $"{CraftingMasteryCatalogDescriptor.ExpRewardsField}{PathSeparator}{CraftingMasteryCatalogDescriptor.ExpModeFactorsField}",
                typeof(CraftingMode)),
            (DataCatalog.PlayerStats, PlayerStatsData.UnarmedField, typeof(EntityParameter)),
        ];

        /// <summary>Every number of a settings document whose ends the game enforces: a multicast row
        /// naming a share outside them is dropped at load, so the tool has to refuse it at the keyboard
        /// instead of letting the author write a stage that silently never rolls.</summary>
        private static readonly (string Catalog, string Path, double Min, double Max)[] s_settingsRanges =
        [
            (DataCatalog.CombatRules,
                $"{CombatRulesCatalogDescriptor.MulticastField}{PathSeparator}{CombatRulesCatalogDescriptor.StagesField}" +
                $"{PathSeparator}{CombatRulesCatalogDescriptor.ChanceField}",
                0, 1),
            (DataCatalog.CombatRules,
                $"{CombatRulesCatalogDescriptor.MulticastField}{PathSeparator}{CombatRulesCatalogDescriptor.StagesField}" +
                $"{PathSeparator}{CombatRulesCatalogDescriptor.CapField}",
                0, 1),
        ];

        /// <summary>
        /// The settings documents: a catalog whose file is one object with nothing to add to it. Every
        /// one is held against the same four facts — the root IS the record, it carries no id, nothing
        /// in it is keyed off one in the localization, and it lives in the one file named here.
        /// <para>The shape is one shape, so it is written once and the catalogs are rows: a copy per
        /// catalog would be a copy of the thing being checked.</para>
        /// </summary>
        [DataTestMethod]
        [DataRow(DataCatalog.CombatRules, "CombatRules")]
        [DataRow(DataCatalog.LootConfiguration, "LootConfiguration")]
        [DataRow(DataCatalog.PassiveTreeRules, "PassiveTreeRules")]
        [DataRow(DataCatalog.Player, "PlayerLifecycle")]
        [DataRow(DataCatalog.Raids, "Raids")]
        [DataRow(DataCatalog.Recovery, "Recovery")]
        [DataRow(DataCatalog.World, "WorldClock")]
        [DataRow(DataCatalog.Trade, "TradeConfiguration")]
        [DataRow(DataCatalog.Influence, "InfluenceMastery")]
        [DataRow(DataCatalog.MartialArtMastery, "MartialArtMastery")]
        [DataRow(DataCatalog.CraftingMastery, "CraftingMastery")]
        [DataRow(DataCatalog.Factions, "FactionRelations")]
        [DataRow(DataCatalog.NpcSpawnRolls, "NpcSpawnRolls")]
        [DataRow(DataCatalog.PlayerStats, "PlayerStats")]
        public void ASettingsCatalogIsOneRecordAtTheRootOfItsOneFile(string catalog, string fileName)
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(catalog));

            Assert.AreEqual(RootShape.Single, schema.Shape, $"the {catalog} file is not read as one record");
            Assert.AreEqual(1, schema.Sections.Count, $"a settings document has one section, and {catalog} states several");
            Assert.AreEqual(SingleObjectDescriptor.RootKey, schema.Sections[0].Key,
                $"the one section of {catalog} is written under a key rather than at the root");
            Assert.IsNull(schema.Sections[0].Record.IdField, $"the {catalog} document names itself, and there is only one of it");
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count,
                $"{catalog} is tuning: nothing in it is keyed off an id it does not have");
            Assert.AreEqual(fileName, schema.Placement.FileFor(_ => null), $"the {catalog} catalog names another file");
            Assert.AreNotEqual(0, schema.Sections[0].Record.Fields.Count, $"the {catalog} record was read with no fields at all");

            // The placement names ONE file, so a folder holding a second one holds a document the tool
            // never opens: the author edits it and the game reads it, and the two say different things.
            Assert.AreEqual(1, ShippedFiles(catalog).Count,
                $"the {catalog} folder ships more than the one file its placement names: "
                + string.Join(", ", ShippedFiles(catalog).Select(Path.GetFileName)));

            Unexpected(builder.Reflection.Notes, s_settingsNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled {catalog} catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>The shipped file of every settings catalog read back through its schema: each key it
        /// writes is one the schema ranks, and the canonical write loses nothing. A key the tool cannot
        /// place is a field the author is quietly locked out of.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.CombatRules)]
        [DataRow(DataCatalog.LootConfiguration)]
        [DataRow(DataCatalog.PassiveTreeRules)]
        [DataRow(DataCatalog.Player)]
        [DataRow(DataCatalog.Raids)]
        [DataRow(DataCatalog.Recovery)]
        [DataRow(DataCatalog.World)]
        [DataRow(DataCatalog.Trade)]
        [DataRow(DataCatalog.Influence)]
        [DataRow(DataCatalog.MartialArtMastery)]
        [DataRow(DataCatalog.CraftingMastery)]
        [DataRow(DataCatalog.Formatting)]
        [DataRow(DataCatalog.PlayerStats)]
        public void ASettingsCatalogRanksEveryKeyItsShippedFileWrites(string catalog)
        {
            List<string> unknown = UnknownKeys(Schema(catalog), catalog);

            Assert.AreEqual(0, unknown.Count,
                $"keys of the {catalog} file no field of the schema is written under: {string.Join(", ", unknown)}");
        }

        /// <summary>The shape of the Formatting catalog: a row per parameter, found by the parameter it
        /// formats, in the one file. The row has no id of its own — the parameter IS the address — so a
        /// second row for one parameter is a rule the game silently overwrites.</summary>
        [TestMethod]
        public void TheFormattingSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Formatting));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(FormattingCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(FormattingCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(FormattingCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "a parameter is named in the localization by the very word the row is found by");

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the formatting DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Formatting catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>
        /// Every word a parser turns into an enum member, said so in the schema and held against what the
        /// shipped file writes. An unmarked one reads to the tool as free text: the author types a name
        /// nothing answers, and the miss surfaces as a throw or a dropped record at load.
        /// <para>One walk for the settings documents and for the standing wave alike: the two tables are
        /// the same three facts about the same kind of field, and a second copy of the check would be a
        /// second place for the rule to drift.</para>
        /// </summary>
        [TestMethod]
        public void EveryChoiceOffersItsMembersAndTheShippedFilesStayInThem()
        {
            foreach ((string catalog, string path, Type members) in s_settingsChoices.Concat(s_standingChoices))
            {
                CatalogSchema schema = Schema(catalog);
                Choice(Leaf(Locate(schema.Sections[0].Record, path)), members);

                List<string> written = [.. ValuesAt(Root(schema, catalog), DocumentPath(schema, path))];

                Assert.AreNotEqual(0, written.Count, $"the shipped {catalog} file writes no '{path}' — the check is checking nothing");
                CollectionAssert.AreEqual(
                    Array.Empty<string>(),
                    Strangers(written, members).ToArray(),
                    $"the shipped {catalog} file writes something other than a {members.Name} at '{path}'");
            }
        }

        /// <summary>Every map whose keys are enum members, said so in the schema and held against what the
        /// shipped file keys it by. A key naming no member is dropped with a report where the weights are
        /// read and takes the whole file down where the ladders are — either way it is a rule that never
        /// fires, which is what a picker instead of a text box prevents.</summary>
        [TestMethod]
        public void EveryMapOffersItsKeysAndTheShippedFilesStayInThem()
        {
            foreach ((string catalog, string path, Type members) in s_settingsMapKeys.Concat(s_standingMapKeys))
            {
                CatalogSchema schema = Schema(catalog);
                FieldSchema map = Locate(schema.Sections[0].Record, path);

                Assert.AreEqual(FieldKind.Dictionary, map.Kind, $"'{catalog}.{path}' is not a map");
                Choice(
                    map.Key ?? throw new AssertFailedException($"'{catalog}.{path}' lets the author write its keys freely"),
                    members);

                List<string> written = [.. Keys(Root(schema, catalog), LastSegment(path))];

                Assert.AreNotEqual(0, written.Count, $"the shipped {catalog} file keys nothing under '{path}' — the check is checking nothing");
                CollectionAssert.AreEqual(
                    Array.Empty<string>(),
                    Strangers(written, members).ToArray(),
                    $"the shipped {catalog} file keys '{path}' by something other than a {members.Name}");
            }
        }

        /// <summary>Where a document writes what a path down a RECORD addresses: under the key the one
        /// section stands at, which a settings document has none of because its record IS the root.</summary>
        private static string[] DocumentPath(CatalogSchema schema, string path) =>
        [
            .. schema.Sections[0].Key.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries),
            .. path.Split(PathSeparator)
        ];

        /// <summary>Every number whose ends the game enforces, said so in the schema and held against
        /// what the shipped file writes. The ends are the point: a row outside them is dropped at load,
        /// and a dropped row is a stage that never rolls with nothing on screen to say why.</summary>
        [TestMethod]
        public void EverySettingsRangeStatesItsEndsAndTheShippedFilesStayInside()
        {
            foreach ((string catalog, string path, double min, double max) in s_settingsRanges)
            {
                CatalogSchema schema = Schema(catalog);
                FieldSchema field = Leaf(Locate(schema.Sections[0].Record, path));
                NumericRange stated = field.Range
                                      ?? throw new AssertFailedException($"'{catalog}.{path}' states no ends at all, and the game enforces {min}..{max}");

                Assert.AreEqual(new NumericRange(min, max), stated, $"'{catalog}.{path}' states other ends than the ones the game enforces");

                List<double> written = [.. Numbers(Root(schema, catalog), LastSegment(path))];

                Assert.AreNotEqual(0, written.Count, $"the shipped {catalog} file writes no '{path}' — the check is checking nothing");
                Assert.AreEqual(0, written.Count(value => value < min || value > max),
                    $"the shipped {catalog} file writes '{path}' outside {min}..{max}: {string.Join(", ", written.Where(value => value < min || value > max))}");
            }
        }

        /// <summary>The one shipped file of a catalog, parsed. Named by the schema's own placement rule,
        /// the way every other check of a shipped document names it.</summary>
        private static JToken Root(CatalogSchema schema, string catalog) =>
            JsonTreeDocument.Load(CatalogFile(schema, catalog)).Root;

        /// <summary>The last step of a path down a record, which is the json name the file writes.</summary>
        private static string LastSegment(string path) => path.Split(PathSeparator)[^1];

        /// <summary>Every word written under one name anywhere in a document — the value itself, or each
        /// element when the name holds a list of them. Only what is written AS a word: one name may stand
        /// for a member of an enum in one record and for a number in another (a threshold's
        /// <c>from</c>), and reading the number as the text it prints as would call the digits a
        /// stranger to the enum.</summary>
        private static IEnumerable<string> Words(JToken token, string field) =>
            Leaves(token, field)
                .Where(value => value.Type == JTokenType.String)
                .Select(value => value.Value<string>())
                .OfType<string>();

        /// <summary>Every number written under one name anywhere in a document.</summary>
        private static IEnumerable<double> Numbers(JToken token, string field) =>
            Leaves(token, field)
                .Where(value => value.Type is JTokenType.Integer or JTokenType.Float)
                .Select(value => value.Value<double>());

        /// <summary>The plain values written under one name: what stands there, and the elements of it
        /// when a list stands there instead. A field naming enum members is written both ways.</summary>
        private static IEnumerable<JValue> Leaves(JToken token, string field)
        {
            foreach (JToken written in Under(token, field))
            {
                if (written is JValue value) yield return value;

                if (written is not JArray list) continue;

                foreach (JValue element in list.OfType<JValue>()) yield return element;
            }
        }

        /// <summary>Everything written under one name, wherever in a document it stands.</summary>
        private static IEnumerable<JToken> Under(JToken token, string field)
        {
            switch (token)
            {
                case JObject holder:
                    foreach (JProperty property in holder.Properties())
                    {
                        if (property.Name == field) yield return property.Value;

                        foreach (JToken nested in Under(property.Value, field)) yield return nested;
                    }

                    break;

                case JArray array:
                    foreach (JToken item in array)
                        foreach (JToken nested in Under(item, field))
                            yield return nested;

                    break;
            }
        }

        /// <summary>What a cost line and a recipe requirement name what they are paid with under. No
        /// descriptor names it — it is the DTOs' own field, the way a table's tiers are — so the walks
        /// spell it out.</summary>
        private const string RequirementIdField = "id";

        /// <summary>What a requirement and a cost line write the KIND of demand under.</summary>
        private const string RequirementTypeField = "type";

        /// <summary>What a recipe writes the kind of thing it makes under, and the words the optional slots
        /// are meant for. Both are the DTO's own fields, so the walks spell them out.</summary>
        private const string ItemTypeField = "itemType";

        private const string OptionalCategoriesField = "optionalResourceCategories";

        /// <summary>What stands for the keys of a map in the address of a value. A path down a RECORD does
        /// not spell them — the schema steps straight from the map to what it holds — and a document does,
        /// so the two addresses differ by exactly this step.</summary>
        private const string AnyKey = "*";

        /// <summary>The catalogs of the bench whose ids are held against the files that answer them. Only
        /// the catalogs are named: WHERE each writes a reference is read off its schema, so a
        /// <see cref="CatalogRefAttribute"/> added to a crafting DTO joins the walk without anyone
        /// remembering to add it here.</summary>
        private static readonly string[] s_craftingCatalogs =
        [
            DataCatalog.Recipes,
            DataCatalog.UpgradeCosts,
            DataCatalog.CraftingAdditives,
            DataCatalog.ItemEffects,
            DataCatalog.Ornaments
        ];

        /// <summary>
        /// The ids the shipped crafting files name that nothing in the game answers — findings about the
        /// data and not about the schemas, which is why they are named rather than fixed: four recipes mint
        /// an equipment template no file declares.
        /// </summary>
        /// <remarks>Held as the whole list rather than as a count, so that one going away is as loud as
        /// one arriving: a pin nothing matches any more is a fact that has moved on.</remarks>
        private static readonly string[] s_unansweredCraftingIds =
        [
            $"{DataCatalog.Recipes} {RecipesCatalogDescriptor.ResultField} → 'Body_Iron_Bastion'",
            $"{DataCatalog.Recipes} {RecipesCatalogDescriptor.ResultField} → 'Boots_Iron_Bastion'",
            $"{DataCatalog.Recipes} {RecipesCatalogDescriptor.ResultField} → 'Gloves_Iron_Bastion'",
            $"{DataCatalog.Recipes} {RecipesCatalogDescriptor.ResultField} → 'Helmet_Iron_Bastion'",
        ];

        /// <summary>Records the shipped data writes twice under one id, by the catalog and the id. A
        /// catalog states an id field to be FOUND by, so a second record under one id is a record nothing
        /// can address: whichever the reader keeps, the other is out of reach — and where the id is a
        /// weight in a roll, both are kept and the roll is loaded.</summary>
        private static readonly (string Catalog, string Id)[] s_recordsWrittenTwice =
        [
            (DataCatalog.ItemEffects, "Passive_Skill_Regeneration"),
        ];

        /// <summary>A recipe minting an equipment template nobody wrote, beside a requirement paid in a
        /// resource the game does ship: the walk has to say the first and stay quiet about the second.</summary>
        private const string ForgedRecipeJson = """
        {
          "craftingRecipes": [
            {
              "id": "Recipe_Forged",
              "resultItemId": "Ring_Nobody_Wrote",
              "rarity": "Rare",
              "itemType": "Equipment",
              "requirements": [ { "type": "Resource", "id": "Crafting_Resource_Iron_Ore", "amount": 1 } ]
            }
          ]
        }
        """;

        /// <summary>A cost line whose rarity override is paid in a resource nobody wrote, while the line's
        /// own default names one that exists — the override is the half a walk stopping at the record
        /// would never read.</summary>
        private const string ForgedUpgradeCostJson = """
        {
          "upgrade": [
            {
              "category": "Weapon",
              "requirements": [
                {
                  "type": "Resource",
                  "id": "Upgrade_Resource_Weapon_Rune",
                  "byRarity": { "Rare": { "id": "Upgrade_Resource_Nobody_Wrote", "amount": 2 } }
                }
              ]
            }
          ],
          "recraft": [],
          "ascend": []
        }
        """;

        /// <summary>The schema of the recipes: one array under a key, each found by its own id and named in
        /// the localization by it. Both reports gather what neither reflection nor the assembled parts could
        /// vouch for, and an editor drawn from a schema with holes in it draws those holes as fields the
        /// author may not touch.</summary>
        [TestMethod]
        public void TheRecipesSchemaBuildsFromTheRealDtosWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Recipes));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(RecipesCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(RecipesCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "a recipe is named in the localization by its own id and reads its description off what it makes");
            Assert.AreEqual(RecipesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));
            CollectionAssert.AreEqual(
                new[] { RecipesCatalogDescriptor.FileName },
                ShippedFiles(DataCatalog.Recipes).Select(Path.GetFileNameWithoutExtension).ToArray(),
                "the catalog ships other files than the one its placement names");

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the recipe DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Recipes catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>
        /// Every id the recipe parser resolves, every name it parses into an enum, and the one field that
        /// reads like a reference and is none, said so in the schema. What a line is paid with answers from
        /// two sections at once: a Resource line whose id names a category IS a category line, so the two
        /// spellings behave alike and both sections have to be offered.
        /// </summary>
        [TestMethod]
        public void TheRecipesSchemaNamesTheReferencesChoicesAndRefusalTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.Recipes).Sections[0].Record;

            FieldSchema result = Leaf(Locate(record, RecipesCatalogDescriptor.ResultField));
            Points(result, RecipesCatalogDescriptor.ResultField, DataCatalog.EquipItems, WholeCatalog);
            Assert.IsFalse(result.AllowEmpty, "a recipe may be written minting nothing, which is a bench that creates nothing");

            string paidWith = $"{RecipesCatalogDescriptor.RequirementsField}{PathSeparator}{RequirementIdField}";
            Points(Leaf(Locate(record, paidWith)), paidWith, DataCatalog.Resources, ResourcesCatalogDescriptor.CraftingResourcesKey);
            Points(Leaf(Locate(record, paidWith)), paidWith, DataCatalog.Resources, ResourcesCatalogDescriptor.MaterialCategoriesKey);

            Choice(Leaf(Locate(record, RarityField)), typeof(Rarity));
            Choice(Leaf(Locate(record, ItemTypeField)), typeof(ItemType));
            Choice(
                Leaf(Locate(record, $"{RecipesCatalogDescriptor.RequirementsField}{PathSeparator}{RequirementTypeField}")),
                typeof(RequirementType));

            Assert.IsTrue(
                Leaf(Locate(record, OptionalCategoriesField)).RefusedAsReference,
                "the words the optional slots are meant for read as ids of a catalog, and nothing says they are not");
        }

        /// <summary>The schema of the upgrade costs: three sections of one file, one per operation that is
        /// paid for, each price list found by the equipment category it prices. Both reports gather what
        /// neither reflection nor the assembled parts could vouch for.</summary>
        [TestMethod]
        public void TheUpgradeCostsSchemaBuildsFromTheRealDtosWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.UpgradeCosts));

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[]
                {
                    UpgradeCostsCatalogDescriptor.UpgradeKey,
                    UpgradeCostsCatalogDescriptor.RecraftKey,
                    UpgradeCostsCatalogDescriptor.AscendKey
                },
                schema.Sections.Select(section => section.Key).ToArray());

            foreach (SectionSchema section in schema.Sections)
                Assert.AreEqual(UpgradeCostsCatalogDescriptor.IdField, section.Record.IdField,
                    $"a price list of '{section.Key}' is found by another field");

            Assert.AreEqual(0, schema.LocalizedSuffixes.Count, "a price is spent and never read: what it is spent on is named in its own catalog");
            Assert.AreEqual(UpgradeCostsCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the upgrade cost DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled UpgradeCosts catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>
        /// Every id the cost parser resolves and every name it parses into an enum, said so in the schema —
        /// on the line itself and on the rarity that overrides it, because either half may name the
        /// resource. The keys of the override map are the rarities: the parser reads them strictly, and one
        /// naming no member drops the WHOLE line rather than its own entry.
        /// </summary>
        [TestMethod]
        public void TheUpgradeCostsSchemaNamesTheReferencesChoicesAndRarityKeysTheParserResolves()
        {
            CatalogSchema schema = Schema(DataCatalog.UpgradeCosts);
            string line = $"{UpgradeCostsCatalogDescriptor.RequirementsField}{PathSeparator}{RequirementIdField}";
            string overridden =
                $"{UpgradeCostsCatalogDescriptor.RequirementsField}{PathSeparator}{UpgradeCostsCatalogDescriptor.ByRarityField}" +
                $"{PathSeparator}{RequirementIdField}";

            foreach (SectionSchema section in schema.Sections)
            {
                Choice(Leaf(Locate(section.Record, UpgradeCostsCatalogDescriptor.IdField)), typeof(EquipmentCategory));
                Choice(
                    Leaf(Locate(section.Record, $"{UpgradeCostsCatalogDescriptor.RequirementsField}{PathSeparator}{RequirementTypeField}")),
                    typeof(RequirementType));

                foreach (string path in new[] { line, overridden })
                {
                    Points(Leaf(Locate(section.Record, path)), path, DataCatalog.Resources, ResourcesCatalogDescriptor.UpgradeResourcesKey);
                    Points(Leaf(Locate(section.Record, path)), path, DataCatalog.Resources, ResourcesCatalogDescriptor.CraftingResourcesKey);
                    Assert.IsTrue(Leaf(Locate(section.Record, path)).AllowEmpty,
                        $"'{path}' must name a resource, and a line whose rarities each name their own writes none");
                }

                FieldSchema byRarity = Locate(section.Record, $"{UpgradeCostsCatalogDescriptor.RequirementsField}{PathSeparator}{UpgradeCostsCatalogDescriptor.ByRarityField}");
                Assert.AreEqual(FieldKind.Dictionary, byRarity.Kind, $"'{UpgradeCostsCatalogDescriptor.ByRarityField}' is not a map");
                Choice(
                    byRarity.Key ?? throw new AssertFailedException($"'{UpgradeCostsCatalogDescriptor.ByRarityField}' lets the author write its keys freely"),
                    typeof(Rarity));
            }
        }

        /// <summary>The schema of the crafting additives: one array under a key, each record found by the
        /// resource it speaks for and carrying no text of its own. Both reports gather what neither
        /// reflection nor the assembled parts could vouch for.</summary>
        [TestMethod]
        public void TheCraftingAdditivesSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.CraftingAdditives));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(CraftingAdditivesCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(CraftingAdditivesCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count,
                "an additive is not an item: the flux in the player's hand is worded in the resources catalog");
            Assert.AreEqual(CraftingAdditivesCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the additive DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled CraftingAdditives catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>Every id the additive parser resolves and the one name it parses into an enum, said so
        /// in the schema. The record is found by a reference: the resource IS the address, and an id
        /// nothing answers is a record no slot will ever reach.</summary>
        [TestMethod]
        public void TheCraftingAdditivesSchemaNamesTheReferencesAndChoiceTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.CraftingAdditives).Sections[0].Record;

            foreach (string section in new[] { ResourcesCatalogDescriptor.UpgradeResourcesKey, ResourcesCatalogDescriptor.CraftingResourcesKey })
                Points(
                    Leaf(Locate(record, CraftingAdditivesCatalogDescriptor.IdField)),
                    CraftingAdditivesCatalogDescriptor.IdField,
                    DataCatalog.Resources,
                    section);

            FieldSchema pool = Leaf(Locate(record, CraftingAdditivesCatalogDescriptor.PoolField));
            Points(pool, CraftingAdditivesCatalogDescriptor.PoolField, DataCatalog.ModifierPools, WholeCatalog);
            Assert.IsTrue(pool.AllowEmpty, "an additive that lends no pool has to be able to say so");

            Choice(Leaf(Locate(record, CraftingAdditivesCatalogDescriptor.RarityFloorField)), typeof(Rarity));
        }

        /// <summary>The schema of the item effects: one array under a key, each record found by the very
        /// behaviour it hands out. Both reports gather what neither reflection nor the assembled parts could
        /// vouch for.</summary>
        [TestMethod]
        public void TheItemEffectsSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.ItemEffects));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(ItemEffectsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(ItemEffectsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            Assert.AreEqual(0, schema.LocalizedSuffixes.Count,
                "an entry is a weight and a payload: the grant reads its own name where it is declared");
            Assert.AreEqual(ItemEffectsCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the item effect DTOs:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled ItemEffects catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>The id an entry is found by is also the reference it makes, and the kind beside it says
        /// which catalog answers. The payload is a map the author fills himself: its keys are the grant
        /// factory's own, so the schema offers a map and no picker for what goes in it.</summary>
        [TestMethod]
        public void TheItemEffectsSchemaNamesTheReferenceChoiceAndFreePayloadTheParserResolves()
        {
            RecordSchema record = Schema(DataCatalog.ItemEffects).Sections[0].Record;

            foreach (string catalog in new[] { DataCatalog.PassiveSkills, DataCatalog.Effects })
                Points(Leaf(Locate(record, ItemEffectsCatalogDescriptor.IdField)), ItemEffectsCatalogDescriptor.IdField, catalog, WholeCatalog);

            Choice(Leaf(Locate(record, ItemEffectsCatalogDescriptor.KindField)), typeof(GrantKind));

            FieldSchema properties = Locate(record, ItemEffectsCatalogDescriptor.PropertiesField);
            Assert.AreEqual(FieldKind.Dictionary, properties.Kind, $"'{ItemEffectsCatalogDescriptor.PropertiesField}' is not a map");
            Assert.IsNull(properties.Key, "the payload's keys are the grant factory's own, and the schema offers a list of them");
        }

        /// <summary>The schema of the ornaments: one array under a key, each found by its own id and both
        /// named and described in the localization by it. An ornament points at nothing — which ability
        /// wears it lives in a save — so the whole of the record is its tier and its worth.</summary>
        [TestMethod]
        public void TheOrnamentsSchemaBuildsFromTheRealDtoWithoutAReport()
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(DataCatalog.Ornaments));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(OrnamentsCatalogDescriptor.RecordsKey, schema.Sections[0].Key);
            Assert.AreEqual(OrnamentsCatalogDescriptor.IdField, schema.Sections[0].Record.IdField);
            CollectionAssert.AreEqual(
                new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix },
                schema.LocalizedSuffixes.ToArray(),
                "an ornament is a named artefact: it is read in a tooltip, not only listed");
            Assert.AreEqual(OrnamentsCatalogDescriptor.FileName, schema.Placement.FileFor(_ => null));

            RecordSchema record = schema.Sections[0].Record;
            Assert.AreEqual(FieldKind.Integer, Field(record, OrnamentsCatalogDescriptor.TierField).Kind);
            Choice(Leaf(Locate(record, RarityField)), typeof(Rarity));

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Reflection.Notes.ToArray(),
                $"reflection has something to say about the ornament DTO:{Environment.NewLine}{builder.Reflection}");
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled Ornaments catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>The shipped files of the bench read back through their schemas: every key they write is
        /// one the schema ranks, and the canonical write loses nothing. A key the tool cannot place is a
        /// field the author is quietly locked out of.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.Recipes)]
        [DataRow(DataCatalog.UpgradeCosts)]
        [DataRow(DataCatalog.CraftingAdditives)]
        [DataRow(DataCatalog.ItemEffects)]
        [DataRow(DataCatalog.Ornaments)]
        public void ACraftingCatalogRanksEveryKeyItsShippedFilesWrite(string catalog)
        {
            List<string> unknown = UnknownKeys(Schema(catalog), catalog);

            Assert.AreEqual(0, unknown.Count,
                $"keys of the {catalog} files no field of the schema is written under:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", unknown)}");
        }

        /// <summary>
        /// Every id the shipped bench names is one the catalog it points into actually writes. Nothing else
        /// can see this: a schema is built one catalog at a time, and the ids answering a recipe live in
        /// another one — so a template that was renamed leaves a recipe minting nothing, and says so only
        /// when the player presses the button.
        /// <para>What the data owes is named rather than failed on: the ids nothing answers are pinned as
        /// findings, so one arriving fails and one going away fails just as loudly.</para>
        /// </summary>
        [TestMethod]
        public void EveryIdTheShippedCraftingFilesNameIsOneItsCatalogWrites()
        {
            List<string> unanswered = [];
            int walked = 0;
            int addressed = 0;

            foreach (string catalog in s_craftingCatalogs)
            {
                CatalogSchema schema = Schema(catalog);

                foreach (SectionSchema section in schema.Sections)
                    foreach (string[] path in References(section.Record))
                    {
                        addressed++;

                        foreach (string file in ShippedFiles(catalog))
                        {
                            (List<string> missing, int read) = Unanswered(JsonTreeDocument.Load(file).Root, section, catalog, path);
                            unanswered.AddRange(missing);
                            walked += read;
                        }
                    }
            }

            Assert.AreNotEqual(0, addressed, "the crafting schemas write no reference at all — the walk has nothing to read");
            Assert.AreNotEqual(0, walked, "the shipped crafting files name nothing at all — the walk proves nothing");

            List<string> named = [.. unanswered.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            Report("Ids the shipped crafting files name that nothing answers", named);

            CollectionAssert.AreEquivalent(
                s_unansweredCraftingIds,
                named.ToArray(),
                $"the shipped crafting files name other ids than the known ones nothing answers:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", named)}");
        }

        /// <summary>The mutation the walk exists for: a recipe minting a template nobody wrote is the one
        /// the bench would take payment for and hand back nothing. What the same recipe is PAID in exists,
        /// and the walk has to stay quiet about it.</summary>
        [TestMethod]
        public void ARecipeMintingATemplateNobodyWrote_IsCaught()
        {
            CatalogSchema schema = Schema(DataCatalog.Recipes);
            JToken root = JsonTreeDocument.Parse(ForgedRecipeJson).Root;

            SectionSchema section = schema.Sections[0];
            (List<string> minted, int mints) = Unanswered(root, section, DataCatalog.Recipes, [RecipesCatalogDescriptor.ResultField]);
            (List<string> paid, int lines) = Unanswered(
                root, section, DataCatalog.Recipes, [RecipesCatalogDescriptor.RequirementsField, RequirementIdField]);

            Assert.AreEqual(1, mints, "the forged recipe mints another number of things than the walk read");
            Assert.AreEqual(1, lines, "the forged recipe is paid in another number of resources than the walk read");
            Assert.AreEqual(1, minted.Count, $"a recipe minting a template nobody wrote went unnoticed: {string.Join(", ", minted)}");
            Assert.AreEqual(0, paid.Count, $"the walk called a shipped resource broken: {string.Join(", ", paid)}");
        }

        /// <summary>The second mutation, one level below the first: a rarity override paid in a resource
        /// nobody wrote. The override is the half a walk stopping at the cost line would never read, and the
        /// line's own default names a resource that exists.</summary>
        [TestMethod]
        public void AnUpgradeCostOverriddenIntoAResourceNobodyWrote_IsCaught()
        {
            CatalogSchema schema = Schema(DataCatalog.UpgradeCosts);
            JToken root = JsonTreeDocument.Parse(ForgedUpgradeCostJson).Root;

            SectionSchema section = Part(schema, UpgradeCostsCatalogDescriptor.UpgradeKey);
            (List<string> defaults, int lines) = Unanswered(
                root, section, DataCatalog.UpgradeCosts, [UpgradeCostsCatalogDescriptor.RequirementsField, RequirementIdField]);
            (List<string> overridden, int rarities) = Unanswered(
                root,
                section,
                DataCatalog.UpgradeCosts,
                [
                    UpgradeCostsCatalogDescriptor.RequirementsField, UpgradeCostsCatalogDescriptor.ByRarityField,
                    AnyKey, RequirementIdField
                ]);

            Assert.AreEqual(1, lines, "the forged cost writes another number of default resources than the walk read");
            Assert.AreEqual(1, rarities, "the forged cost writes another number of rarity overrides than the walk read");
            Assert.AreEqual(0, defaults.Count, $"the walk called a shipped resource broken: {string.Join(", ", defaults)}");
            Assert.AreEqual(1, overridden.Count,
                $"a rarity paid in a resource nobody wrote went unnoticed: {string.Join(", ", overridden)}");
        }

        /// <summary>
        /// Across every described catalog: no section writes two records under one id. A catalog states the
        /// field a record is FOUND by, so a second record under one id is a record nothing can address —
        /// whichever of the two the reader keeps, the other is out of reach. Where the id is not an address
        /// but a weight in a roll, both are kept instead and the roll is quietly loaded.
        /// <para>What the shipped data owes is named rather than failed on, so that a duplicate arriving
        /// fails and one going away fails just as loudly.</para>
        /// </summary>
        [TestMethod]
        public void NoSectionOfADescribedCatalogWritesTwoRecordsUnderOneId()
        {
            List<string> twice = [];
            int counted = 0;

            foreach (ICatalogDescriptor descriptor in CatalogDescriptors.All)
            {
                CatalogSchema schema = Schema(descriptor.Catalog);

                if (schema.Shape is not (RootShape.ArrayUnderKey or RootShape.SectionsOfArrays)) continue;

                foreach (SectionSchema section in schema.Sections)
                {
                    if (section.Record.IdField is not { } idField) continue;

                    Dictionary<string, int> written = new(StringComparer.Ordinal);

                    foreach (string file in ShippedFiles(descriptor.Catalog))
                        foreach (JObject record in SectionRecords(JsonTreeDocument.Load(file).Root, section.Key))
                        {
                            if (Id(record, idField) is not { Length: > 0 } id) continue;

                            counted++;
                            written[id] = written.GetValueOrDefault(id) + 1;
                        }

                    twice.AddRange(written.Where(pair => pair.Value > 1).Select(pair => Named(descriptor.Catalog, pair.Key)));
                }
            }

            Assert.AreNotEqual(0, counted, "no record of any described catalog was read — the check is checking nothing");

            List<string> named = [.. twice.Order(StringComparer.Ordinal)];
            Report("Records the shipped data writes twice under one id", named);

            CollectionAssert.AreEquivalent(
                s_recordsWrittenTwice.Select(known => Named(known.Catalog, known.Id)).ToArray(),
                named.ToArray(),
                $"the shipped data writes other records twice than the known ones:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", named)}");
        }

        /// <summary>One record of one catalog, as both the finding and the pin spell it.</summary>
        private static string Named(string catalog, string id) => $"{catalog}: {id}";

        /// <summary>One keyed part of a catalog, where the walk needs the key as much as the record.</summary>
        private static SectionSchema Part(CatalogSchema schema, string section) =>
            schema.Sections.FirstOrDefault(candidate => candidate.Key == section)
            ?? throw new AssertFailedException($"the catalog holds no '{section}' section.");

        /// <summary>
        /// Every address one record writes a reference at, read off the schema rather than listed: a
        /// catalog whose DTO gains a reference joins the walk without anyone remembering it. The addresses
        /// are the FILE's — a map is a step of its own in a document and none in a path down a record — so
        /// every map met on the way puts an <see cref="AnyKey"/> in.
        /// <para>What a map is KEYED by is not among them: a key is not written where a value is, and no
        /// catalog of the bench points into one.</para>
        /// </summary>
        private static IEnumerable<string[]> References(RecordSchema record, string[] at, HashSet<RecordSchema>? reading = null)
        {
            reading ??= new HashSet<RecordSchema>(ReferenceEqualityComparer.Instance);

            if (!reading.Add(record)) yield break;

            foreach (RecordSchema shape in Shapes(record))
                foreach (FieldSchema field in shape.Fields)
                {
                    string[] address = [.. at, field.JsonName, .. MapSteps(field)];
                    FieldSchema leaf = Leaf(field);

                    if (leaf.Kind == FieldKind.Reference) yield return address;

                    if (leaf.Record is not { } nested) continue;

                    foreach (string[] found in References(nested, address, reading)) yield return found;
                }

            reading.Remove(record);
        }

        /// <summary>The addresses of one section's record, without duplicates: two shapes writing one key
        /// name one place in the file.</summary>
        private static IEnumerable<string[]> References(RecordSchema record) =>
            References(record, []).DistinctBy(address => string.Join(PathSeparator, address), StringComparer.Ordinal);

        /// <summary>A record and the shapes it may take: a reference belongs to whichever of them a record
        /// on disk turns out to be.</summary>
        private static IEnumerable<RecordSchema> Shapes(RecordSchema record)
        {
            yield return record;

            if (record.Variants is not { } variants) yield break;

            foreach (VariantSchema variant in variants.Variants) yield return variant.Record;
        }

        /// <summary>An <see cref="AnyKey"/> for every map standing between a field and what it holds.</summary>
        private static IEnumerable<string> MapSteps(FieldSchema field)
        {
            for (FieldSchema node = field; node.Kind is FieldKind.Array or FieldKind.Dictionary && node.Item is { } item; node = item)
                if (node.Kind == FieldKind.Dictionary)
                    yield return AnyKey;
        }

        /// <summary>The ids one section of a document writes at one address that nothing answers, and how
        /// many were read at all. A field pointing into a catalog this build cannot read is passed over
        /// whole: half an answer would call every id of the other half broken.</summary>
        private static (List<string> Unanswered, int Walked) Unanswered(JToken root, SectionSchema section, string catalog, string[] path)
        {
            List<string> unanswered = [];
            int walked = 0;
            string address = string.Join(PathSeparator, path.Where(step => step != AnyKey));

            FieldSchema field = Leaf(Locate(section.Record, address));
            Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{catalog}.{address}' is not a reference");

            List<HashSet<string>> answering = [.. field.RefTargets.Select(Answers).OfType<HashSet<string>>()];
            if (answering.Count != field.RefTargets.Count) return (unanswered, walked);

            foreach (JObject record in SectionRecords(root, section.Key))
                foreach (string id in ValuesAt(record, path, NamesNoRecord))
                {
                    if (id.Length == 0) continue;

                    walked++;

                    if (answering.Exists(ids => ids.Contains(id))) continue;

                    unanswered.Add($"{catalog} {address} → '{id}'");
                }

            return (unanswered, walked);
        }

        /// <summary>
        /// Records whose id the walk must not read as a reference. Two cases, and both are the price of a
        /// markup that cannot see the field beside it: a requirement demanding a level of mastery writes a
        /// LOCALIZATION KEY where its siblings write a resource, and a grant handing over a MODIFIER names
        /// no record anywhere — its id is the label its own lines are minted under. The markup states the
        /// catalogs without regard to the field that says which, so the shipped word answering nothing
        /// there is the limit of the contract and not broken data.
        /// </summary>
        private static bool NamesNoRecord(JObject holder) =>
            string.Equals(holder.Value<string>(RequirementTypeField), nameof(RequirementType.MasteryLevel), StringComparison.Ordinal)
            || string.Equals(holder.Value<string>(GrantKindField), nameof(GrantKind.Modifier), StringComparison.Ordinal);

        /// <summary>Every id one target answers with, read out of the shipped files of the catalog it names
        /// and off that catalog's own schema. Null for a catalog no descriptor covers: what that one is
        /// written in is not this build's to know, and reading it as no ids at all would report every
        /// reference into it broken.</summary>
        private static HashSet<string>? Answers(ReferenceTarget target)
        {
            if (CatalogDescriptors.All.All(descriptor => descriptor.Catalog != target.Catalog)) return null;

            CatalogSchema schema = Schema(target.Catalog);

            // The ids are gathered out of arrays under a section key, which is the one shape that holds
            // several records. A catalog whose records are keyed by the author, or which is one record,
            // would answer with nothing at all and call every reference into it broken.
            Assert.IsTrue(
                schema.Shape is RootShape.ArrayUnderKey or RootShape.SectionsOfArrays,
                $"the {target.Catalog} catalog is written as {schema.Shape}, which this walk cannot read ids out of");

            HashSet<string> ids = new(StringComparer.Ordinal);

            foreach (string file in ShippedFiles(target.Catalog))
            {
                JToken root = JsonTreeDocument.Load(file).Root;

                foreach (SectionSchema section in schema.Sections)
                {
                    if (target.Section is { } named && section.Key != named) continue;
                    if (section.Record.IdField is not { } idField) continue;

                    foreach (JObject record in SectionRecords(root, section.Key))
                        if (Id(record, idField) is { Length: > 0 } written)
                            ids.Add(written);
                }
            }

            return ids;
        }

        /// <summary>
        /// Every value one address reaches inside a record, as the text it is written as: through the
        /// lists standing on the way, and through the keys of a map wherever the address writes a
        /// <see cref="AnyKey"/>. A number met where a word was expected is read as its digits rather than
        /// passed over — that is the miss the walk exists to name.
        /// </summary>
        /// <param name="stepOver">Records the walk must not read, with everything under them. Named by the
        /// caller and not by the walk: what a REFERENCE may skip is the reference walk's own rule, and a
        /// walk reading what is simply written skips nothing.</param>
        private static IEnumerable<string> ValuesAt(JToken token, string[] path, Func<JObject, bool>? stepOver = null, int step = 0)
        {
            if (token is JArray array)
            {
                foreach (JToken item in array)
                    foreach (string found in ValuesAt(item, path, stepOver, step))
                        yield return found;

                yield break;
            }

            if (step == path.Length)
            {
                if (token is JValue value && value.Value<string>() is { } text) yield return text;

                yield break;
            }

            if (token is not JObject holder || stepOver?.Invoke(holder) == true) yield break;

            if (path[step] == AnyKey)
            {
                foreach (JProperty property in holder.Properties())
                    foreach (string found in ValuesAt(property.Value, path, stepOver, step + 1))
                        yield return found;

                yield break;
            }

            if (holder[path[step]] is not { } child) yield break;

            foreach (string nested in ValuesAt(child, path, stepOver, step + 1)) yield return nested;
        }

        /// <summary>What the reflector may still have to say about the DTOs of the standing wave — one
        /// list for all of them, because it is empty for all of them: every record there is plain
        /// properties the walk reads whole. A note appearing here is either a DTO to fix or a fact to
        /// write down — never something to silence by widening the check.</summary>
        private static readonly (string About, string Word)[] s_allowedStandingNotes = [];

        /// <summary>
        /// Every field of the standing wave a parser turns into an enum member, addressed the way the file
        /// writes it. All of them are read strictly: a faction, a level or a value type naming no member
        /// takes its whole file down at load, and a trader naming no faction loses that trader. The tool
        /// has to offer the members rather than a text box, or the author types the miss himself.
        /// </summary>
        private static readonly (string Catalog, string Path, Type Members)[] s_standingChoices =
        [
            (DataCatalog.Traders, TradersCatalogDescriptor.FractionField, typeof(Fractions)),
            (DataCatalog.ReputationDeeds, ReputationDeedsCatalogDescriptor.FloorField, typeof(RelationLevel)),
            (DataCatalog.ReputationPerks, ReputationPerksCatalogDescriptor.IdField, typeof(RelationLevel)),
            (DataCatalog.NpcBuffs,
                $"{NpcBuffsCatalogDescriptor.ModifiersField}{PathSeparator}{NpcBuffsCatalogDescriptor.ParameterField}",
                typeof(EntityParameter)),
            (DataCatalog.NpcBuffs,
                $"{NpcBuffsCatalogDescriptor.ModifiersField}{PathSeparator}{NpcBuffsCatalogDescriptor.ValueTypeField}",
                typeof(ModifierValueType)),
            (DataCatalog.NpcBuffs,
                $"{NpcBuffsCatalogDescriptor.GrantsField}{PathSeparator}{NpcBuffsCatalogDescriptor.KindField}",
                typeof(GrantKind)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.RelationsField}{PathSeparator}{FactionsCatalogDescriptor.FromField}",
                typeof(Fractions)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.RelationsField}{PathSeparator}{FactionsCatalogDescriptor.ToField}",
                typeof(Fractions)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.RelationsField}{PathSeparator}{FactionsCatalogDescriptor.LevelField}",
                typeof(RelationLevel)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.ScaleField}{PathSeparator}{FactionsCatalogDescriptor.ThresholdsField}" +
                $"{PathSeparator}{FactionsCatalogDescriptor.LevelField}",
                typeof(RelationLevel)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.TraitsField}{PathSeparator}{FactionsCatalogDescriptor.FractionField}",
                typeof(Fractions)),
            (DataCatalog.Factions,
                $"{FactionsCatalogDescriptor.DefaultsField}{PathSeparator}{FactionsCatalogDescriptor.FractionField}",
                typeof(Fractions)),
        ];

        /// <summary>Every map of the wave whose KEYS are members of an enum rather than words the author
        /// picks. A key naming no member is dropped with a report where the shelf is weighed and takes the
        /// whole file down where the spawn ladders are read — either way it is a rule that never fires,
        /// which is exactly what a picker instead of a text box prevents.</summary>
        private static readonly (string Catalog, string Path, Type Members)[] s_standingMapKeys =
        [
            (DataCatalog.Traders,
                $"{TradersCatalogDescriptor.RandomEquipField}{PathSeparator}{TradersCatalogDescriptor.RarityWeightsField}",
                typeof(Rarity)),
            (DataCatalog.NpcSpawnRolls, NpcSpawnRollsCatalogDescriptor.ModifiersField, typeof(EntityType)),
            (DataCatalog.NpcSpawnRolls, NpcSpawnRollsCatalogDescriptor.AbilitiesField, typeof(EntityType)),
            (DataCatalog.NpcSpawnRolls, NpcSpawnRollsCatalogDescriptor.RarityMultipliersField, typeof(Rarity)),
        ];

        /// <summary>
        /// The keys the shipped files of the wave write that no field of a schema is written under, by the
        /// catalog and the key. Two facts and not a leniency: a deed catalog states one tuning number
        /// beside its records and the shape has room for sections of records only, and the spawn ladders
        /// carry the author's own commentary in keys the reader ignores, json having no comments.
        /// <para>Held as the whole list so that one arriving fails and one going away fails just as
        /// loudly: a key the tool cannot place is a field the author is quietly locked out of.</para>
        /// </summary>
        private static readonly (string Catalog, string Key)[] s_standingUnknownKeys =
        [
            (DataCatalog.ReputationDeeds, ReputationDeedsCatalogDescriptor.WitnessRadiusField),
            (DataCatalog.NpcSpawnRolls, "_comment"),
            (DataCatalog.NpcSpawnRolls, "_formula"),
            (DataCatalog.NpcSpawnRolls, "_seedWarning"),
            (DataCatalog.NpcSpawnRolls, "_requiredFields"),
            (DataCatalog.NpcSpawnRolls, "_modifiersNote"),
            (DataCatalog.NpcSpawnRolls, "_abilitiesNote"),
            (DataCatalog.NpcSpawnRolls, "_rarityNote"),
        ];

        /// <summary>A deed whose no-penalty floor names a rung of the ladder nobody wrote, beside two
        /// deeds spelling their own fields right: the walk has to say the first and stay quiet about the
        /// rest.</summary>
        private const string ForgedDeedJson = """
        {
          "witnessRadius": 600,
          "deeds": [
            { "id": "Deed_Forged", "reputation": -50, "noPenaltyAtOrBelow": "Loathing" },
            { "id": "Deed_Sound", "reputation": -50, "noPenaltyAtOrBelow": "Hostility" }
          ]
        }
        """;

        /// <summary>A trader weighing a rarity nobody wrote, beside a shelf whose own faction is spelled
        /// right. The weight is a KEY and not a value — the half a walk reading only what is written
        /// under a name would never see.</summary>
        private const string ForgedTraderJson = """
        {
          "traders": [
            {
              "id": "Trader_Forged",
              "fraction": "Human",
              "catalog": [ { "itemId": "Crafting_Resource_Iron_Ore", "count": 1 } ],
              "randomEquip": { "count": 1, "rarityWeights": { "Common": 50, "Mythical": 5 } }
            }
          ]
        }
        """;

        /// <summary>The catalogs whose records stand in one array under one key: the shops, the deeds a
        /// standing is moved by, the perks a standing buys and the buffs an npc modifier pulls. Each is
        /// held to the same five facts — the shape, the key, the field a record is found by, whether
        /// anything in it is worded for the player, and the one file it lives in.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.Traders, TradersCatalogDescriptor.RecordsKey, TradersCatalogDescriptor.IdField,
            TradersCatalogDescriptor.FileName, true)]
        [DataRow(DataCatalog.ReputationDeeds, ReputationDeedsCatalogDescriptor.RecordsKey,
            ReputationDeedsCatalogDescriptor.IdField, ReputationDeedsCatalogDescriptor.FileName, false)]
        [DataRow(DataCatalog.ReputationPerks, ReputationPerksCatalogDescriptor.RecordsKey,
            ReputationPerksCatalogDescriptor.IdField, ReputationPerksCatalogDescriptor.FileName, false)]
        [DataRow(DataCatalog.NpcBuffs, NpcBuffsCatalogDescriptor.RecordsKey, NpcBuffsCatalogDescriptor.IdField,
            NpcBuffsCatalogDescriptor.FileName, false)]
        public void AStandingCatalogIsOneArrayUnderAKeyOfItsOneFile(
            string catalog, string recordsKey, string idField, string fileName, bool named)
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(catalog));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape, $"the {catalog} file is not read as one array under a key");
            Assert.AreEqual(1, schema.Sections.Count, $"{catalog} states several sections where its file holds one");
            Assert.AreEqual(recordsKey, schema.Sections[0].Key, $"the records of {catalog} are read from another key");
            Assert.AreEqual(idField, schema.Sections[0].Record.IdField, $"a record of {catalog} is found by another field");
            Assert.AreNotEqual(0, schema.Sections[0].Record.Fields.Count, $"the {catalog} record was read with no fields at all");
            CollectionAssert.AreEqual(
                named ? new[] { LocalizedKeyAttribute.NoSuffix } : [],
                schema.LocalizedSuffixes.ToArray(),
                $"{catalog} words another part of itself for the player than it is read for");
            Assert.AreEqual(fileName, schema.Placement.FileFor(_ => null), $"the {catalog} catalog names another file");
            CollectionAssert.AreEqual(
                new[] { fileName },
                ShippedFiles(catalog).Select(Path.GetFileNameWithoutExtension).ToArray(),
                $"the {catalog} catalog ships other files than the one its placement names");

            Unexpected(builder.Reflection.Notes, s_allowedStandingNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled {catalog} catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>The shipped files of the wave read back through their schemas: every key they write is
        /// one the schema ranks, save the two known cases, and the canonical write loses nothing.</summary>
        [TestMethod]
        public void EveryStandingCatalogRanksEveryKeyItsShippedFileWritesBarTheKnownOnes()
        {
            List<string> named = [];

            foreach (string catalog in s_standingCatalogs)
                named.AddRange(UnknownKeys(Schema(catalog), catalog).Select(LastKey).Distinct(StringComparer.Ordinal)
                    .Select(key => Named(catalog, key)));

            named.Sort(StringComparer.Ordinal);
            Report("Keys the shipped standing files write that no field of a schema is written under", named);

            CollectionAssert.AreEquivalent(
                s_standingUnknownKeys.Select(known => Named(known.Catalog, known.Key)).ToArray(),
                named.ToArray(),
                $"the shipped files write other unplaceable keys than the known ones:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", named));
        }

        /// <summary>
        /// What a shelf sells and what a buff hands over: the two references of the wave, said so in the
        /// schema. The shelf answers from exactly where a kill's drop does — a shop resolves an equipment
        /// template through the minter and everything else out of the item store, which is the same store
        /// a drop is copied from — so the targets are one list and not two copies of one.
        /// </summary>
        [TestMethod]
        public void TheShelfAndTheBuffNameWhatTheyHandOver()
        {
            string good = $"{TradersCatalogDescriptor.ShelfField}{PathSeparator}{TradersCatalogDescriptor.GoodField}";
            FieldSchema sold = Leaf(Locate(Schema(DataCatalog.Traders).Sections[0].Record, good));

            Assert.AreEqual(FieldKind.Reference, sold.Kind, $"'{good}' names nothing");
            Assert.IsFalse(sold.AllowEmpty, $"'{good}' may be left empty, which is a seat on the shelf selling nothing");
            CollectionAssert.AreEquivalent(
                s_dropTargets,
                sold.RefTargets.ToArray(),
                $"'{good}' points somewhere other than where sellable things are written: {string.Join(", ", sold.RefTargets)}");

            string granted = $"{NpcBuffsCatalogDescriptor.GrantsField}{PathSeparator}{NpcBuffsCatalogDescriptor.IdField}";
            RecordSchema buff = Schema(DataCatalog.NpcBuffs).Sections[0].Record;

            foreach (string catalog in new[] { DataCatalog.PassiveSkills, DataCatalog.Effects })
                Points(Leaf(Locate(buff, granted)), granted, catalog, WholeCatalog);

            FieldSchema properties = Locate(
                buff, $"{NpcBuffsCatalogDescriptor.GrantsField}{PathSeparator}{NpcBuffsCatalogDescriptor.PropertiesField}");
            Assert.AreEqual(FieldKind.Dictionary, properties.Kind, $"'{NpcBuffsCatalogDescriptor.PropertiesField}' is not a map");
            Assert.IsNull(properties.Key, "the payload's keys are the grant factory's own, and the schema offers a list of them");
        }

        /// <summary>The first mutation: a deed floored at a rung of the ladder nobody wrote. The parser
        /// reads that word strictly and takes the whole catalog down with it, so nothing in the game says
        /// which deed did it — while the two deeds spelling their own right have to stay unremarked.</summary>
        [TestMethod]
        public void ADeedFlooredAtALevelNobodyNamed_IsCaught()
        {
            List<string> written = [.. Words(JsonTreeDocument.Parse(ForgedDeedJson).Root, ReputationDeedsCatalogDescriptor.FloorField)];
            List<string> strangers = Strangers(written, typeof(RelationLevel));

            Assert.AreEqual(2, written.Count, "the forged deeds write another number of floors than the walk read");
            CollectionAssert.AreEqual(new[] { "Loathing" }, strangers.ToArray(),
                $"a deed floored at a rung nobody wrote went unnoticed: {string.Join(", ", strangers)}");
        }

        /// <summary>The second mutation, on the other half of a document: a rarity nobody wrote standing as
        /// the KEY of a weight. The shelf drops that weight with a report and rolls on without it, so the
        /// author gets the mix he never asked for and no error at all.</summary>
        [TestMethod]
        public void ATraderWeighingARarityNobodyNamed_IsCaught()
        {
            JToken root = JsonTreeDocument.Parse(ForgedTraderJson).Root;
            List<string> keyed = [.. Keys(root, TradersCatalogDescriptor.RarityWeightsField)];
            List<string> sold = [.. Words(root, TradersCatalogDescriptor.FractionField)];

            Assert.AreEqual(2, keyed.Count, "the forged trader weighs another number of rarities than the walk read");
            CollectionAssert.AreEqual(new[] { "Mythical" }, Strangers(keyed, typeof(Rarity)).ToArray(),
                "a weight keyed by a rarity nobody wrote went unnoticed");
            CollectionAssert.AreEqual(Array.Empty<string>(), Strangers(sold, typeof(Fractions)).ToArray(),
                "the walk called the forged trader's own faction a stranger");
        }

        /// <summary>The catalogs of the standing wave, named once: the shops, the deeds, the perks, the
        /// buffs, the faction ledger and the spawn ladders.</summary>
        private static readonly string[] s_standingCatalogs =
        [
            DataCatalog.Traders,
            DataCatalog.ReputationDeeds,
            DataCatalog.ReputationPerks,
            DataCatalog.NpcBuffs,
            DataCatalog.Factions,
            DataCatalog.NpcSpawnRolls
        ];

        /// <summary>The words of a document that name no member of the enum they were written for, each of
        /// them once and in one order. One walk for the shipped files and for the forged ones: a check
        /// whose mutation is caught by other code than the one holding the data proves nothing about
        /// it.</summary>
        private static List<string> Strangers(IEnumerable<string> written, Type members) =>
            Strangers(written, Enum.GetNames(members));

        /// <summary>The same walk against the words a SCHEMA offers rather than against a type's members:
        /// what the author is given to pick from is the schema's list, and holding a document against the
        /// enum instead would pass a field whose markup was never applied.</summary>
        private static List<string> Strangers(IEnumerable<string> written, IEnumerable<string> offered)
        {
            HashSet<string> named = [.. offered];

            return [.. written.Where(word => !named.Contains(word)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        }

        /// <summary>What a record holding several grants writes them under, and what one grant names the
        /// behaviour it hands over by. Both are the DTOs' own fields — the equipment template's and the npc
        /// buff's alike — so the walks spell them out.</summary>
        private const string GrantsField = "grants";

        private const string GrantIdField = "id";

        /// <summary>What a grant writes the KIND of behaviour it hands over under — the field that says
        /// whether its id names a record at all.</summary>
        private const string GrantKindField = "kind";

        /// <summary>What the reflector may still have to say about the DTOs of the grant-target wave — one
        /// list for all three catalogs, because it is empty for all three: an effect row, a passive entry
        /// and the forms of a condition are plain properties the walk reads whole. A note appearing here is
        /// either a DTO to fix or a fact to write down — never something to silence by widening the
        /// check.</summary>
        private static readonly (string About, string Word)[] s_allowedGrantTargetNotes = [];

        /// <summary>The catalogs of the wave: what an effect is worth, which passive a node or a grant may
        /// name, and the predicates a line is held up by. Nothing hands these out — they are where the
        /// behaviour is declared, and every grant in the game points into two of them.</summary>
        private static readonly string[] s_grantTargetCatalogs =
        [
            DataCatalog.Effects,
            DataCatalog.PassiveSkills,
            DataCatalog.Conditions
        ];

        /// <summary>
        /// Every field of the wave a parser turns into an enum member on the RECORD itself, addressed the
        /// way the file writes it. It is read strictly and the miss costs a whole record: an unreadable
        /// strength refuses the effect row, and the effect keeps whatever balance its factory was built
        /// with. The words a condition is narrowed by are held per form as well as per field, so they are
        /// stated in <see cref="s_conditionChoices"/> instead.
        /// </summary>
        private static readonly (string Catalog, string Path, Type Members)[] s_grantTargetChoices =
        [
            (DataCatalog.Effects, EffectsCatalogDescriptor.PowerField, typeof(EffectPower)),
        ];

        /// <summary>Every word of a condition a factory parses into an enum member, by the predicate that
        /// reads it. Held per FORM as well as per field: the form is what the inspector draws, so a
        /// narrowing left on the record alone never reaches the author — he types the miss himself, and the
        /// entry is refused whole at load, which drops every line naming it.</summary>
        private static readonly (string Type, string Path, Type Members)[] s_conditionChoices =
        [
            (ConditionTypes.ResourceThreshold, ConditionsCatalogDescriptor.ResourceField, typeof(Costs)),
            (ConditionTypes.TargetResourceThreshold, ConditionsCatalogDescriptor.ResourceField, typeof(Costs)),
            (ConditionTypes.ResourceState, ConditionsCatalogDescriptor.ResourceField, typeof(Costs)),
            (ConditionTypes.ResourceState, ConditionsCatalogDescriptor.StateField, typeof(ResourceState)),
            (ConditionTypes.Effect, ConditionsCatalogDescriptor.ScopeField, typeof(EffectScope)),
            (ConditionTypes.Stance, ConditionsCatalogDescriptor.StanceField, typeof(Stance)),
            (ConditionTypes.TurnAction, ConditionsCatalogDescriptor.ActionField, typeof(TurnAction)),
        ];

        /// <summary>What each form REQUIRES, which is what its factory refuses the record without. A form
        /// asking for less hands the author a blank entry the reader drops; a form asking for more locks
        /// him out of a record the game reads perfectly well.</summary>
        private static readonly (string Type, string[] Demanded)[] s_conditionDemands =
        [
            (ConditionTypes.ResourceThreshold,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.ResourceField, ConditionsCatalogDescriptor.ValueField]),
            (ConditionTypes.TargetResourceThreshold,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.ResourceField, ConditionsCatalogDescriptor.ValueField]),
            (ConditionTypes.ResourceState,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.ResourceField, ConditionsCatalogDescriptor.StateField]),
            (ConditionTypes.Status,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.StatusesField]),
            (ConditionTypes.TargetStatus,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.StatusesField]),
            (ConditionTypes.Effect,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.ScopeField]),
            (ConditionTypes.Stance,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.StanceField]),
            (ConditionTypes.TurnAction,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.ActionField]),
            (ConditionTypes.BattleTurn,
                [ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField,
                    ConditionsCatalogDescriptor.TurnField]),
        ];

        /// <summary>Which form the inspector draws for which predicate. Held as the whole mapping rather
        /// than as a count: a type drawn by the wrong form offers the author keys its factory never reads,
        /// and the two families asked about the fighter being hit share the owner-side form on purpose —
        /// one shape is written the same way whichever fighter it asks about.</summary>
        private static readonly (string Type, string Form)[] s_conditionShapes =
        [
            (ConditionTypes.ResourceThreshold, nameof(ConditionOverAResourceShare)),
            (ConditionTypes.TargetResourceThreshold, nameof(ConditionOverAResourceShare)),
            (ConditionTypes.ResourceState, nameof(ConditionOverAResourceBoundary)),
            (ConditionTypes.Status, nameof(ConditionOverStatuses)),
            (ConditionTypes.TargetStatus, nameof(ConditionOverStatuses)),
            (ConditionTypes.Effect, nameof(ConditionOverCarriedEffects)),
            (ConditionTypes.Stance, nameof(ConditionOverTheStance)),
            (ConditionTypes.TurnAction, nameof(ConditionOverTurnActions)),
            (ConditionTypes.BattleTurn, nameof(ConditionOverTheBattleTurn)),
        ];

        /// <summary>The three places the shipped data hands a behaviour over, addressed the way each file
        /// writes it: the roll an item effect IS, the grants an npc buff carries, and the grants written on
        /// an equipment template. Until the two catalogs behind them were described, every one of these ids
        /// was passed over whole — the walk could not tell a renamed passive from one it simply could not
        /// read.</summary>
        private static readonly (string Catalog, string Section, string[] Path)[] s_grantSites =
        [
            (DataCatalog.ItemEffects, ItemEffectsCatalogDescriptor.RecordsKey, [ItemEffectsCatalogDescriptor.IdField]),
            (DataCatalog.NpcBuffs, NpcBuffsCatalogDescriptor.RecordsKey, [NpcBuffsCatalogDescriptor.GrantsField, NpcBuffsCatalogDescriptor.IdField]),
            (DataCatalog.EquipItems, EquipItemsCatalogDescriptor.RecordsKey, [GrantsField, GrantIdField]),
        ];

        /// <summary>The grants the shipped data writes that nothing declares. Empty, and held as the whole
        /// list rather than as a count: one arriving fails, and one going away fails just as loudly. Grants
        /// of kind Modifier are not among them at all — they name no record anywhere, and the walk steps
        /// over them the way it steps over a requirement asking for a level of mastery.</summary>
        private static readonly string[] s_unansweredGrants = [];

        /// <summary>A grant naming a passive nobody wrote, beside one naming a passive the catalog does
        /// carry: the walk has to say the first and stay quiet about the second.</summary>
        private const string ForgedItemEffectsJson = """
        {
          "effects": [
            { "id": "Passive_Skill_Nobody_Wrote", "kind": "Passive", "weight": 100 },
            { "id": "Passive_Skill_Execute", "kind": "Passive", "weight": 100 }
          ]
        }
        """;

        /// <summary>A grant handing over a MODIFIER, whose id names no record and never could, beside a
        /// passive grant naming nothing: the walk has to pass the first over entirely and still say the
        /// second.</summary>
        private const string ForgedModifierGrantJson = """
        {
          "effects": [
            { "id": "Item_Effect_Of_Its_Own_Lines", "kind": "Modifier", "weight": 100 },
            { "id": "Passive_Skill_Nobody_Wrote", "kind": "Passive", "weight": 100 }
          ]
        }
        """;

        /// <summary>An effect standing at a strength nobody named, beside one spelling its own right. The
        /// reader refuses the whole row over that word — the balance of the effect silently becomes
        /// whatever its factory was built with — so the picker is what has to prevent it being typed.</summary>
        private const string ForgedEffectJson = """
        {
          "effects": [
            { "id": "Effect_Forged", "power": "Unbreakable", "properties": { "duration": 3 } },
            { "id": "Effect_Sound", "power": "Absolute", "properties": { "duration": 3 } }
          ]
        }
        """;

        /// <summary>The catalogs a grant and a gated line point INTO, each held to the same five facts —
        /// the shape, the key, the field a record is found by, what of it is worded for the player, and the
        /// one file it lives in.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.Effects, EffectsCatalogDescriptor.RecordsKey, EffectsCatalogDescriptor.IdField,
            EffectsCatalogDescriptor.FileName,
            new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix, LocalizationService.TooltipSuffix })]
        [DataRow(DataCatalog.PassiveSkills, PassiveSkillsCatalogDescriptor.RecordsKey, PassiveSkillsCatalogDescriptor.IdField,
            PassiveSkillsCatalogDescriptor.FileName,
            new[] { LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix })]
        [DataRow(DataCatalog.Conditions, ConditionsCatalogDescriptor.RecordsKey, ConditionsCatalogDescriptor.IdField,
            ConditionsCatalogDescriptor.FileName, new string[0])]
        public void AGrantTargetCatalogIsOneArrayUnderAKeyOfItsOneFile(
            string catalog, string recordsKey, string idField, string fileName, string[] worded)
        {
            CatalogSchemaBuilder builder = new(new SchemaReflector());
            CatalogSchema schema = builder.Build(Descriptor(catalog));

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape, $"the {catalog} file is not read as one array under a key");
            Assert.AreEqual(1, schema.Sections.Count, $"{catalog} states several sections where its file holds one");
            Assert.AreEqual(recordsKey, schema.Sections[0].Key, $"the records of {catalog} are read from another key");
            Assert.AreEqual(idField, schema.Sections[0].Record.IdField, $"a record of {catalog} is found by another field");
            Assert.AreNotEqual(0, schema.Sections[0].Record.Fields.Count, $"the {catalog} record was read with no fields at all");
            CollectionAssert.AreEqual(
                worded,
                schema.LocalizedSuffixes.ToArray(),
                $"{catalog} words another part of itself for the player than it is read for");
            Assert.AreEqual(fileName, schema.Placement.FileFor(_ => null), $"the {catalog} catalog names another file");
            CollectionAssert.AreEqual(
                new[] { fileName },
                ShippedFiles(catalog).Select(Path.GetFileNameWithoutExtension).ToArray(),
                $"the {catalog} catalog ships other files than the one its placement names");

            Unexpected(builder.Reflection.Notes, s_allowedGrantTargetNotes, builder.Reflection);
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                builder.Checks.Notes.ToArray(),
                $"the assembled {catalog} catalog disagrees with itself:{Environment.NewLine}{builder.Checks}");
        }

        /// <summary>The shipped files of the wave read back through their schemas: every key they write is
        /// one the schema ranks, and the canonical write loses nothing. A key the tool cannot place is a
        /// field the author is quietly locked out of.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.Effects)]
        [DataRow(DataCatalog.PassiveSkills)]
        [DataRow(DataCatalog.Conditions)]
        public void AGrantTargetCatalogRanksEveryKeyItsShippedFileWrites(string catalog)
        {
            List<string> unknown = UnknownKeys(Schema(catalog), catalog);

            Assert.AreEqual(0, unknown.Count,
                $"keys of the {catalog} file no field of the schema is written under:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", unknown)}");
        }

        /// <summary>Every word a parser of the wave turns into an enum member, said so in the schema and
        /// held against what the shipped file writes. An unmarked one reads to the tool as free text: the
        /// author types a name nothing answers, and the miss surfaces as a dropped record at load.</summary>
        [TestMethod]
        public void EveryGrantTargetChoiceOffersItsMembersAndTheShippedFilesStayInThem()
        {
            foreach ((string catalog, string path, Type members) in s_grantTargetChoices)
            {
                CatalogSchema schema = Schema(catalog);
                Choice(Leaf(Locate(schema.Sections[0].Record, path)), members);

                List<string> written = [.. Words(Root(schema, catalog), LastSegment(path))];

                Assert.AreNotEqual(0, written.Count, $"the shipped {catalog} file writes no '{path}' — the check is checking nothing");
                CollectionAssert.AreEqual(
                    Array.Empty<string>(),
                    Strangers(written, members).ToArray(),
                    $"the shipped {catalog} file writes something other than a {members.Name} at '{path}'");
            }
        }

        /// <summary>
        /// The numbers of an effect and the fields of a passive are maps and lists the author fills
        /// himself: the keys belong to the factory that builds the thing, which lives battle-side where the
        /// tool cannot reach it, so the schema offers a map and no picker for what goes in it.
        /// </summary>
        [TestMethod]
        public void TheEffectNumbersAndThePassiveFieldsAreTheFactorysOwnWords()
        {
            FieldSchema properties = Locate(Schema(DataCatalog.Effects).Sections[0].Record, EffectsCatalogDescriptor.PropertiesField);

            Assert.AreEqual(FieldKind.Dictionary, properties.Kind, $"'{EffectsCatalogDescriptor.PropertiesField}' is not a map");
            Assert.IsNull(properties.Key, "the canonical numbers are keyed by the building factory's own names, and the schema offers a list of them");
            Assert.AreEqual(FieldKind.Number, Leaf(properties).Kind, "a canonical number is not a number");

            FieldSchema fields = Locate(Schema(DataCatalog.PassiveSkills).Sections[0].Record, PassiveSkillsCatalogDescriptor.FieldsField);

            Assert.AreEqual(FieldKind.Array, fields.Kind, $"'{PassiveSkillsCatalogDescriptor.FieldsField}' is not a list");
            Assert.AreEqual(FieldKind.String, Leaf(fields).Kind, "the fields a passive's factory reads are not words");
        }

        /// <summary>
        /// The forms of a condition against the predicates the parser actually registers. The catalog is
        /// the one described file the game parses without a DTO — a record goes to whichever factory its
        /// type names — so the forms are the only statement of its shapes there is, and a type the game
        /// grows without one is drawn as the whole record's worth of optional keys.
        /// <para>Every form is a narrowing of the record and never more: a key on a form that the record
        /// does not carry is a field the reader would never look for. And every form begins with the two
        /// keys of a condition — the form is what the inspector draws and what it offers keys out of, so a
        /// form without them draws an entry no line can name and no factory can pick.</para>
        /// </summary>
        [TestMethod]
        public void TheConditionsSchemaDrawsAFormForEveryPredicateTheParserRegisters()
        {
            RecordSchema record = Schema(DataCatalog.Conditions).Sections[0].Record;
            VariantSet shapes = record.Variants
                                ?? throw new AssertFailedException("the conditions catalog states one shape for nine predicates");

            Assert.AreEqual(ConditionsCatalogDescriptor.TypeField, shapes.Discriminator,
                "the shapes of a condition are told apart by another field than the one the parser reads");
            CollectionAssert.AreEquivalent(
                ConditionParser.BuiltInFactories().Select(factory => factory.Type).ToArray(),
                shapes.Variants.Select(shape => shape.DiscriminatorValue).ToArray(),
                "the forms and the registered predicates are not the same set");
            CollectionAssert.AreEqual(
                s_conditionShapes,
                shapes.Variants.Select(shape => (shape.DiscriminatorValue, shape.Record.TypeName)).ToArray(),
                "a predicate is drawn by another form than the one it is written in");

            HashSet<string> carried = [.. record.Fields.Select(field => field.JsonName)];
            List<string> stray =
            [
                .. shapes.Variants
                    .SelectMany(shape => shape.Record.Fields.Select(field => $"{shape.Record.TypeName}.{field.JsonName}"))
                    .Where(named => !carried.Contains(named.Split(PathSeparator)[^1]))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ];

            Assert.AreEqual(0, stray.Count, $"forms writing keys the record does not carry: {string.Join(", ", stray)}");

            foreach (RecordSchema shape in shapes.Variants.Select(variant => variant.Record).Distinct())
                CollectionAssert.AreEqual(
                    new[] { ConditionsCatalogDescriptor.IdField, ConditionsCatalogDescriptor.TypeField },
                    shape.Fields.Take(2).Select(field => field.JsonName).ToArray(),
                    $"'{shape.TypeName}' does not begin with the two keys every condition on disk begins with");

            Assert.AreEqual(
                ConditionsCatalogDescriptor.NegateField,
                record.Fields[^1].JsonName,
                "the inversion is written after whatever the type asked for, and the record has to keep it there");
        }

        /// <summary>Every word a condition's factory parses into an enum member, offered on the record AND
        /// on the form the author is actually handed, and held against what the shipped file writes.</summary>
        [TestMethod]
        public void EveryConditionChoiceIsOfferedOnTheRecordAndOnItsFormAndTheShippedFileStaysInIt()
        {
            CatalogSchema schema = Schema(DataCatalog.Conditions);
            RecordSchema record = schema.Sections[0].Record;
            VariantSet shapes = record.Variants
                                ?? throw new AssertFailedException("the conditions catalog states one shape for nine predicates");

            foreach ((string type, string path, Type members) in s_conditionChoices)
            {
                Choice(Leaf(Locate(record, path)), members);
                Choice(Leaf(Locate(Shape(shapes, type), path)), members);
            }

            foreach (string path in s_conditionChoices.Select(choice => choice.Path).Distinct(StringComparer.Ordinal))
            {
                Type members = s_conditionChoices.First(choice => choice.Path == path).Members;
                List<string> written = [.. Words(Root(schema, DataCatalog.Conditions), path)];

                Assert.AreNotEqual(0, written.Count, $"the shipped conditions write no '{path}' — the check is checking nothing");
                CollectionAssert.AreEqual(
                    Array.Empty<string>(),
                    Strangers(written, members).ToArray(),
                    $"the shipped conditions write something other than a {members.Name} at '{path}'");
            }
        }

        /// <summary>
        /// What each form asks for against what its factory refuses the record without. The forms are what
        /// a freshly made entry is built from — a blank record carries the keys a form REQUIRES and nothing
        /// else — so a key demanded here and left optional there is an entry the tool hands the author and
        /// the reader drops without reading.
        /// </summary>
        [TestMethod]
        public void EveryFormAsksForExactlyWhatItsFactoryRefusesTheRecordWithout()
        {
            VariantSet shapes = Schema(DataCatalog.Conditions).Sections[0].Record.Variants
                                ?? throw new AssertFailedException("the conditions catalog states one shape for nine predicates");

            foreach ((string type, string[] demanded) in s_conditionDemands)
            {
                RecordSchema form = Shape(shapes, type);

                CollectionAssert.AreEquivalent(
                    demanded,
                    form.Fields.Where(field => field.Required).Select(field => field.JsonName).ToArray(),
                    $"'{type}' asks the author for other keys than the ones its factory refuses the record without");
            }

            foreach (string counting in new[] { ConditionTypes.Effect, ConditionTypes.TurnAction })
                Assert.IsFalse(
                    Field(Shape(shapes, counting), ConditionsCatalogDescriptor.CountField).Required,
                    $"'{counting}' demands a count, and a record writing none counts one rather than nothing");
        }

        /// <summary>A shipped file of a catalog whose records take several shapes, written back out through
        /// the schema key for key. The key order of such a catalog is assembled from the forms and the
        /// record together — and an order that disagrees with the file has the tool silently rewrite every
        /// record the first time an author saves one of them.</summary>
        [DataTestMethod]
        [DataRow(DataCatalog.Conditions)]
        [DataRow(DataCatalog.LootTables)]
        public void TheCanonicalWriteOfAShippedPolymorphicCatalogMovesNothing(string catalog)
        {
            CatalogSchema schema = Schema(catalog);
            JToken root = JsonTreeDocument.Load(CatalogFile(schema, catalog)).Root;

            (List<string> unknown, List<string> reordered) = Written(root, schema, new SchemaKeyOrder(schema));

            Assert.AreEqual(0, unknown.Count,
                $"keys of the shipped {catalog} file no field of the schema is written under: {string.Join(", ", unknown)}");
            Assert.AreEqual(0, reordered.Count,
                $"the canonical write would move the keys of records the author never touched:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", reordered)}");
        }

        /// <summary>The one word of a condition the schema cannot narrow: a status name is a member of the
        /// enum OR one of the groups the design talks in, and the markup states one vocabulary or none. The
        /// words are left free there, so the check the picker would have made is made here instead.</summary>
        [TestMethod]
        public void EveryStatusAConditionNamesIsOneTheMaskReaderResolves()
        {
            CatalogSchema schema = Schema(DataCatalog.Conditions);
            FieldSchema statuses = Locate(schema.Sections[0].Record, ConditionsCatalogDescriptor.StatusesField);

            Assert.AreEqual(FieldKind.String, Leaf(statuses).Kind, "a status is written as a word");

            List<string> written = [.. Words(Root(schema, DataCatalog.Conditions), ConditionsCatalogDescriptor.StatusesField)];
            List<string> strangers = [.. written.Where(word => !Resolves(word)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

            Assert.AreNotEqual(0, written.Count, "the shipped conditions name no status at all — the check is checking nothing");
            CollectionAssert.AreEqual(Array.Empty<string>(), strangers.ToArray(),
                $"the shipped conditions name statuses no mask resolves: {string.Join(", ", strangers)}");
        }

        /// <summary>Whether the one reader of a status name makes anything of it. Asked of that reader
        /// rather than of a vocabulary assembled here: it matches names its own way, and a second spelling
        /// of the same list would be free to disagree with it about a letter's case.</summary>
        private static bool Resolves(string word)
        {
            try
            {
                StatusMasks.Resolve(word);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>The one id a condition writes: the effect whose stacks it counts. Said on the record
        /// every reader parses into and on the form the author is offered, which is what makes the picker
        /// reach him at all.</summary>
        [TestMethod]
        public void TheConditionCountingStacksNamesTheEffectItCounts()
        {
            RecordSchema record = Schema(DataCatalog.Conditions).Sections[0].Record;
            VariantSet shapes = record.Variants
                                ?? throw new AssertFailedException("the conditions catalog states one shape for nine predicates");

            foreach (RecordSchema shape in new[] { record, Shape(shapes, ConditionTypes.Effect) })
            {
                FieldSchema counted = Leaf(Locate(shape, ConditionsCatalogDescriptor.EffectIdField));

                Points(counted, ConditionsCatalogDescriptor.EffectIdField, DataCatalog.Effects, WholeCatalog);
                Assert.IsTrue(counted.AllowEmpty,
                    "every scope but the stack-counting one counts a family and names no effect, and has to be able to say so");
            }
        }

        /// <summary>
        /// Every behaviour the shipped data hands over is one the catalog declaring it actually writes.
        /// Nothing else can see this: the ids answering a grant live in another catalog than the record
        /// carrying it, so a renamed passive leaves an item granting nothing — and says so only when the
        /// player equips it.
        /// <para>What the data owes is named rather than failed on, so that one arriving fails and one
        /// going away fails just as loudly.</para>
        /// </summary>
        [TestMethod]
        public void EveryGrantTheShippedDataWritesNamesABehaviourItsCatalogDeclares()
        {
            List<string> unanswered = [];
            int walked = 0;

            foreach ((string catalog, string section, string[] path) in s_grantSites)
            {
                SectionSchema handing = Part(Schema(catalog), section);

                foreach (string file in ShippedFiles(catalog))
                {
                    (List<string> missing, int read) = Unanswered(JsonTreeDocument.Load(file).Root, handing, catalog, path);
                    unanswered.AddRange(missing);
                    walked += read;
                }
            }

            Assert.AreNotEqual(0, walked, "the shipped data hands nothing over at all — the walk proves nothing");

            List<string> named = [.. unanswered.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
            Report("Grants the shipped data writes that no catalog declares", named);

            CollectionAssert.AreEquivalent(
                s_unansweredGrants,
                named.ToArray(),
                $"the shipped data hands over other behaviours than the known ones nothing declares:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", named)}");
        }

        /// <summary>The first mutation, and the one the whole wave exists for: a grant naming a passive
        /// nobody wrote. The roll picks it, the factory is handed an id it does not know, and the item
        /// comes out of the mint carrying a promise it cannot keep — while the grant beside it, naming a
        /// passive the catalog does carry, has to stay unremarked.</summary>
        [TestMethod]
        public void AGrantNamingAPassiveNobodyWrote_IsCaught()
        {
            SectionSchema section = Part(Schema(DataCatalog.ItemEffects), ItemEffectsCatalogDescriptor.RecordsKey);

            (List<string> missing, int read) = Unanswered(
                JsonTreeDocument.Parse(ForgedItemEffectsJson).Root, section, DataCatalog.ItemEffects, [ItemEffectsCatalogDescriptor.IdField]);

            Assert.AreEqual(2, read, "the forged catalog hands over another number of behaviours than the walk read");
            CollectionAssert.AreEqual(
                new[] { $"{DataCatalog.ItemEffects} {ItemEffectsCatalogDescriptor.IdField} → 'Passive_Skill_Nobody_Wrote'" },
                missing.ToArray(),
                $"a grant naming a passive nobody wrote went unnoticed: {string.Join(", ", missing)}");
        }

        /// <summary>The other half of that rule: a grant of kind Modifier is not a reference at all. Its id
        /// is the label its own minted lines carry, so reading it as a reference would report every such
        /// grant broken — while the passive grant beside it, naming nothing, still has to be said.</summary>
        [TestMethod]
        public void AGrantHandingOverAModifier_IsNotReadAsAReference()
        {
            SectionSchema section = Part(Schema(DataCatalog.ItemEffects), ItemEffectsCatalogDescriptor.RecordsKey);

            (List<string> missing, int read) = Unanswered(
                JsonTreeDocument.Parse(ForgedModifierGrantJson).Root, section, DataCatalog.ItemEffects, [ItemEffectsCatalogDescriptor.IdField]);

            Assert.AreEqual(1, read, "the walk read the modifier grant's own label as an id it could answer");
            CollectionAssert.AreEqual(
                new[] { $"{DataCatalog.ItemEffects} {ItemEffectsCatalogDescriptor.IdField} → 'Passive_Skill_Nobody_Wrote'" },
                missing.ToArray(),
                $"the walk said other grants than the passive one naming nothing: {string.Join(", ", missing)}");
        }

        /// <summary>The second mutation, on the other half of the wave and on the other kind of miss: an
        /// effect standing at a strength that names no rung of the ladder. The reader refuses that row
        /// whole, so the effect keeps whatever balance its factory was built with and nothing in the game
        /// says which one — while the row spelling its own strength right stays unremarked.</summary>
        [TestMethod]
        public void AnEffectAtAStrengthNobodyNamed_IsCaught()
        {
            FieldSchema power = Leaf(Locate(Schema(DataCatalog.Effects).Sections[0].Record, EffectsCatalogDescriptor.PowerField));
            List<string> written = [.. Words(JsonTreeDocument.Parse(ForgedEffectJson).Root, EffectsCatalogDescriptor.PowerField)];
            List<string> strangers = Strangers(written, power.EnumValues);

            Assert.AreEqual(2, written.Count, "the forged catalog writes another number of strengths than the walk read");
            Assert.AreNotEqual(0, power.EnumValues.Count, "the schema offers no strengths at all, so nothing could be a stranger to them");
            CollectionAssert.AreEqual(new[] { "Unbreakable" }, strangers.ToArray(),
                $"an effect standing at a strength nobody named went unnoticed: {string.Join(", ", strangers)}");
        }

        /// <summary>Every catalog of the wave answers with ids at all. A described catalog whose shipped
        /// file the walk reads as empty would call every grant into it broken, and the pin above would go
        /// on excusing the lot.</summary>
        [TestMethod]
        public void EveryGrantTargetCatalogAnswersWithTheIdsItsShippedFileWrites()
        {
            foreach (string catalog in s_grantTargetCatalogs)
            {
                HashSet<string>? ids = Answers(ReferenceTarget.Whole(catalog));

                Assert.IsNotNull(ids, $"the {catalog} catalog is described and still answers nothing");
                Assert.AreNotEqual(0, ids.Count, $"the shipped {catalog} file writes no id at all — every reference into it would read as broken");
            }
        }
    }
}
