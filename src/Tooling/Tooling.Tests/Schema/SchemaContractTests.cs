namespace Tooling.Tests.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// The contract between the game and the authoring tool: what a DTO may say about itself, what a
    /// description of a catalog is made of, and what a descriptor has to be able to express.
    /// <para>The four descriptors are the proof by use, written against the shapes the files actually
    /// take — loot positions two arrays down from the section they belong to, modifier sections told
    /// apart by their own key, equipment split across nine files by its slot, and the plain case.</para>
    /// </summary>
    [TestClass]
    public class SchemaContractTests
    {
        private const string NpcCatalog = "Npc";
        private const string AbilitiesCatalog = "Abilities";
        private const string NpcBuffsCatalog = "NpcBuffs";
        private const string LootTablesCatalog = "LootTables";
        private const string NpcModifiersCatalog = "NpcModifiers";
        private const string EquipItemsCatalog = "EquipItems";
        private const string ResourcesCatalog = "Resources";
        private const string RecipesCatalog = "Recipes";

        private const string IdField = "id";
        private const string KeyField = "key";
        private const string SlotField = "slot";
        private const string PriceField = "price";
        private const string TierField = "tier";
        private const string RarityField = "rarity";
        private const string AugmentsField = "augments";
        private const string ItemsField = "items";
        private const string TiersField = "tiers";
        private const string ModifiersField = "modifiers";
        private const string UniqueScopeField = "uniqueScope";
        private const string FractionField = "fraction";
        private const string AbilitiesField = "abilities";
        private const string BaseParametersField = "baseParameters";
        private const string NpcBuffIdField = "npcBuffId";
        private const string WeightField = "weight";
        private const string ScaleField = "scale";
        private const string TierUpgradeChanceField = "tierUpgradeChance";

        private const string NpcsKey = "npcs";
        private const string ModsKey = "mods";
        private const string GeneralKey = "general";
        private const string FractionsKey = "fractions";
        private const string TypesKey = "types";
        private const string IndividualKey = "individual";

        private const string ScaleKey = "scale";
        private const string TierUpgradeKey = "tierUpgrade";

        private const string NameSuffix = "";
        private const string DescriptionSuffix = "_Description";

        private const string WeaponSlot = "Weapon";

        private const double MinTier = 0d;
        private const double MaxTier = 3d;

        private const string ModelNamespace = "Tooling.Schema.Model";

        /// <summary>The tool reads the markup off the properties and draws a picker, a range or a text
        /// box accordingly. Read back one by one: an attribute carrying the wrong argument fails here
        /// and nowhere earlier.</summary>
        [TestMethod]
        public void Attributes_AreReadBackFromTheMarkedProperties()
        {
            Assert.AreEqual(AbilitiesCatalog, Attribute<CatalogRefAttribute>(typeof(NpcDto), nameof(NpcDto.Abilities)).Catalog);
            Assert.IsFalse(Attribute<CatalogRefAttribute>(typeof(NpcDto), nameof(NpcDto.Abilities)).AllowEmpty);
            Assert.IsTrue(Attribute<CatalogRefAttribute>(typeof(NpcModifierDto), nameof(NpcModifierDto.NpcBuffId)).AllowEmpty);
            Assert.AreEqual(typeof(TestFraction), Attribute<EnumOfAttribute>(typeof(NpcDto), nameof(NpcDto.Fraction)).EnumType);
            Assert.AreEqual(MinTier, Attribute<RangeAttribute>(typeof(LootTierDto), nameof(LootTierDto.Tier)).Min);
            Assert.AreEqual(MaxTier, Attribute<RangeAttribute>(typeof(LootTierDto), nameof(LootTierDto.Tier)).Max);
            Assert.AreEqual(LocalizedKeyAttribute.NoSuffix, Attribute<LocalizedKeyAttribute>(typeof(DialogueNodeDto), nameof(DialogueNodeDto.Text)).Suffix);
            Assert.AreEqual(DescriptionSuffix, Attribute<LocalizedKeyAttribute>(typeof(DialogueNodeDto), nameof(DialogueNodeDto.Summary)).Suffix);
            Assert.AreEqual(KeyField, Attribute<DiscriminatorAttribute>(typeof(NpcModifiersDocumentDto), nameof(NpcModifiersDocumentDto.Mods)).Field);
            Assert.AreEqual(typeof(TestParameter), Attribute<DictionaryKeyAttribute>(typeof(NpcDto), nameof(NpcDto.BaseParameters)).EnumType);
            Assert.IsNull(Attribute<DictionaryKeyAttribute>(typeof(NpcDto), nameof(NpcDto.BaseParameters)).Catalog);
            Assert.AreEqual(AbilitiesCatalog, Attribute<DictionaryKeyAttribute>(typeof(NpcDto), nameof(NpcDto.AbilityWeights)).Catalog);
            Assert.IsNull(Attribute<DictionaryKeyAttribute>(typeof(NpcDto), nameof(NpcDto.AbilityWeights)).EnumType);
            Assert.IsNotNull(Attribute<NotARefAttribute>(typeof(DialogueNodeDto), nameof(DialogueNodeDto.NodeId)));
            Assert.IsNotNull(Attribute<HiddenAttribute>(typeof(NpcDto), nameof(NpcDto.Version)));
        }

        /// <summary>One field, several catalogs. A loot position's id names a piece of equipment, a
        /// crafting resource or a recipe, and a field allowed only one of them would report two thirds
        /// of every loot table as pointing at nothing.</summary>
        [TestMethod]
        public void CatalogRef_NamesEveryCatalogTheFieldMayPointInto()
        {
            IEnumerable<CatalogRefAttribute> references = Property(typeof(LootPositionByIdDto), nameof(LootPositionByIdDto.Id))
                .GetCustomAttributes<CatalogRefAttribute>();

            CollectionAssert.AreEquivalent(
                new[] { EquipItemsCatalog, ResourcesCatalog, RecipesCatalog },
                references.Select(reference => reference.Catalog).ToArray());
        }

        /// <summary>An unmarked property answers nothing, which is what lets the pin in the game tell a
        /// decision from a silence.</summary>
        [TestMethod]
        public void Attributes_AreAbsentFromWhateverWasNotMarked()
        {
            PropertyInfo property = Property(typeof(LootPositionByIdDto), nameof(LootPositionByIdDto.Price));

            Assert.IsNull(property.GetCustomAttribute<CatalogRefAttribute>());
            Assert.IsNull(property.GetCustomAttribute<NotARefAttribute>());
            Assert.IsNull(property.GetCustomAttribute<DictionaryKeyAttribute>());
            Assert.IsNull(property.GetCustomAttribute<HiddenAttribute>());
        }

        /// <summary>The markup describes fields of a record and nothing else; anywhere else it would be
        /// read by nobody.</summary>
        [TestMethod]
        public void Attributes_TargetPropertiesAndFieldsOnly()
        {
            IReadOnlyList<Type> attributes = SchemaAttributes();

            Assert.AreNotEqual(0, attributes.Count);
            foreach (Type attribute in attributes)
            {
                AttributeUsageAttribute? usage = attribute.GetCustomAttribute<AttributeUsageAttribute>();

                Assert.IsNotNull(usage, $"{attribute.Name} does not say where it may be written.");
                Assert.AreEqual(AttributeTargets.Property | AttributeTargets.Field, usage.ValidOn, attribute.Name);
                Assert.IsTrue(attribute.IsSealed, $"{attribute.Name} is open to inheritance.");
            }
        }

        [TestMethod]
        public void CatalogRef_NamingNoCatalog_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new CatalogRefAttribute(" "));

        /// <summary>A field pointed at something that is not an enum would offer an empty list of
        /// members, which reads to the author as "this one has no values yet".</summary>
        [TestMethod]
        public void EnumOf_SomethingThatIsNotAnEnum_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new EnumOfAttribute(typeof(NpcDto)));

        /// <summary>Keys are described by their own markup, which is refused on the same terms as the markup
        /// describing values: an empty catalog names nothing, and a type with no members offers nothing.</summary>
        [TestMethod]
        public void DictionaryKey_NamingNoCatalog_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new DictionaryKeyAttribute(" "));

        [TestMethod]
        public void DictionaryKey_OfSomethingThatIsNotAnEnum_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new DictionaryKeyAttribute(typeof(NpcDto)));

        [TestMethod]
        public void Range_EndingBelowItsStart_IsRefused() =>
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RangeAttribute(MaxTier, MinTier));

        /// <summary>A schema is handed around, held for comparison against the file on disk and kept
        /// while the author edits: anything settable on it is a way for one holder to change what
        /// another is looking at.</summary>
        [TestMethod]
        public void Model_HasNothingSettableAfterItIsBuilt()
        {
            foreach (Type type in ModelTypes())
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    MethodInfo? setter = property.SetMethod;
                    if (setter == null) continue;

                    bool initOnly = setter.ReturnParameter
                        .GetRequiredCustomModifiers()
                        .Any(modifier => modifier == typeof(IsExternalInit));

                    Assert.IsTrue(initOnly, $"{type.Name}.{property.Name} can be written after construction.");
                }
        }

        /// <summary>Copying a record with one part replaced is how the tool edits a schema, and it goes
        /// nowhere near the constructor. A check that only the constructor makes is a check the second
        /// version of a schema does not get.</summary>
        [TestMethod]
        public void Guards_HoldWhenAPartIsReplaced()
        {
            RecordSchema record = Record(nameof(NpcDto), Text(IdField));
            CatalogSchema catalog = TwoSectionCatalog();

            Assert.ThrowsException<ArgumentException>(() => { _ = record with { TypeName = " " }; });
            Assert.ThrowsException<ArgumentException>(() => { _ = catalog with { Sections = [] }; });
            Assert.ThrowsException<ArgumentException>(() => { _ = catalog with { Shape = RootShape.Single }; });
            Assert.ThrowsException<ArgumentNullException>(() => { _ = catalog with { Placement = null! }; });
        }

        /// <summary>Two descriptions of one catalog are one description. They are built independently —
        /// by the reflector now, from a saved file later — so anything comparing them by reference
        /// answers "changed" every time and no comparison of schemas is worth making.</summary>
        [TestMethod]
        public void Schemas_CompareByTheirContents()
        {
            CatalogSchema first = new NpcDescriptor().Describe(new StubSchemaBuilder());
            CatalogSchema second = new NpcDescriptor().Describe(new StubSchemaBuilder());

            Assert.AreNotSame(first, second);
            Assert.AreEqual(first, second);
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>Including the parts held in lists, however deep they sit: a field renamed inside the
        /// record of a section is exactly the change a comparison is asked about.</summary>
        [TestMethod]
        public void Schemas_DifferingDeepInsideAList_AreNotEqual()
        {
            CatalogSchema plain = OneSectionCatalog(Record(nameof(NpcDto), Text(IdField)));
            CatalogSchema renamed = OneSectionCatalog(Record(nameof(NpcDto), Text(KeyField)));

            Assert.AreNotEqual(plain, renamed);
        }

        /// <summary>A list sharing an array with whoever built it is immutable only until that builder
        /// writes to the array again.</summary>
        [TestMethod]
        public void List_CopiesWhatItWasGiven()
        {
            string[] source = [IdField, KeyField];
            SchemaList<string> list = source;

            source[0] = SlotField;

            Assert.AreEqual(IdField, list[0]);
        }

        /// <summary>An unset list and an empty one are one value: a catalog with no localized text says
        /// so by leaving the list out, and nothing downstream has two cases to handle.</summary>
        [TestMethod]
        public void EmptyList_AndAnUnsetOne_AreTheSameValue()
        {
            SchemaList<string> unset = default;
            SchemaList<string> empty = Array.Empty<string>();

            Assert.AreEqual(unset, empty);
            Assert.AreEqual(0, unset.Count);
            Assert.AreEqual(unset.GetHashCode(), empty.GetHashCode());
        }

        [TestMethod]
        public void List_WithAnEmptyEntry_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new SchemaList<string>([IdField, null!]));

        /// <summary>Two sections under one key means one of them is never written to the file.</summary>
        [TestMethod]
        public void Catalog_WithTwoSectionsUnderOneKey_IsRefused()
        {
            RecordSchema record = Record(nameof(NpcDto), Text(IdField));

            Assert.ThrowsException<ArgumentException>(() => new CatalogSchema(
                RootShape.SectionsOfArrays,
                [Section(NpcsKey, record), Section(NpcsKey, record)],
                [],
                new FreeFilePlacement()));
        }

        [TestMethod]
        public void Catalog_WithNoSections_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new CatalogSchema(
                RootShape.ArrayUnderKey, [], [], new FreeFilePlacement()));

        /// <summary>A file that is one record, or one map of records, has one section to put them in.</summary>
        [TestMethod]
        public void Catalog_OfASingleRecordWithSeveralSections_IsRefused()
        {
            RecordSchema record = Record(nameof(NpcDto), Text(IdField));

            Assert.ThrowsException<ArgumentException>(() => new CatalogSchema(
                RootShape.Single,
                [Section(NpcsKey, record), Section(ModsKey, record)],
                [],
                new FreeFilePlacement()));
        }

        /// <summary>A record said to take several shapes and given none leaves the inspector with a form
        /// to draw and no idea what is in it.</summary>
        [TestMethod]
        public void Variants_WithNothingToChooseFrom_AreRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new VariantSet([]));

        /// <summary>The field telling the shapes apart lives on the shapes. A variant without it cannot
        /// be recognised in a file it is read from, nor written back so that it can be.</summary>
        [TestMethod]
        public void Variants_NotCarryingWhatTellsThemApart_AreRefused()
        {
            VariantSchema nameless = Variant(ScaleKey, Record(nameof(ScaleSectionDto), Text(WeightField)));
            VariantSchema keyed = Variant(ScaleKey, Record(nameof(ScaleSectionDto), Text(KeyField), Text(ScaleField)));
            VariantSchema present = Variant(ScaleKey, Record(nameof(ScaleSectionDto), Text(ScaleField)));

            Assert.ThrowsException<ArgumentException>(() => new VariantSet([nameless], KeyField));
            Assert.ThrowsException<ArgumentException>(() => new VariantSet([nameless]));

            Assert.AreEqual(1, new VariantSet([keyed], KeyField).Variants.Count);
            Assert.AreEqual(1, new VariantSet([present]).Variants.Count);
        }

        [TestMethod]
        public void Npc_IsOneSectionOfRecordsCarryingAnIdAndTwoLocalizedKeys()
        {
            CatalogSchema schema = new NpcDescriptor().Describe(new StubSchemaBuilder());

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(1, schema.Sections.Count);
            Assert.AreEqual(NpcsKey, schema.Sections[0].Key);
            Assert.AreEqual(nameof(NpcDto), schema.Sections[0].Record.TypeName);
            Assert.AreEqual(IdField, schema.Sections[0].Record.IdField);
            Assert.IsNull(schema.Sections[0].Record.Variants);
            CollectionAssert.AreEqual(new[] { NameSuffix, DescriptionSuffix }, schema.LocalizedSuffixes.ToArray());
            Assert.AreEqual(NpcCatalog, schema.Placement.FileFor(_ => null));
        }

        /// <summary>A dictionary the author does not key freely says what its keys may be: base
        /// parameters are named by an enum, and an inspector that did not know it would take any word.</summary>
        [TestMethod]
        public void Npc_DescribesTheKeysOfItsParameterMapAsWellAsTheValues()
        {
            CatalogSchema schema = new NpcDescriptor().Describe(new StubSchemaBuilder());
            FieldSchema parameters = Field(schema.Sections[0].Record, BaseParametersField);

            Assert.AreEqual(FieldKind.Dictionary, parameters.Kind);
            Assert.IsNotNull(parameters.Key);
            Assert.AreEqual(FieldKind.Enum, parameters.Key.Kind);
            Assert.AreNotEqual(0, parameters.Key.EnumValues.Count);
            Assert.IsNotNull(parameters.Item);
            Assert.AreEqual(FieldKind.Number, parameters.Item.Kind);
        }

        /// <summary>The polymorphic thing in a loot table is the position, and a position sits two
        /// arrays below the section it belongs to. The descriptor names its shapes; the builder is what
        /// carries them down to where the positions actually are.</summary>
        [TestMethod]
        public void LootTables_CarryTheirPositionsShapesTwoArraysDown()
        {
            CatalogSchema schema = new LootTablesDescriptor().Describe(new StubSchemaBuilder());

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            CollectionAssert.AreEqual(
                new[] { GeneralKey, FractionsKey, TypesKey, IndividualKey },
                schema.Sections.Select(section => section.Key).ToArray());

            RecordSchema table = schema.Sections[0].Record;
            Assert.AreEqual(nameof(LootTableDto), table.TypeName);
            Assert.AreEqual(KeyField, table.IdField);
            Assert.IsNull(table.Variants);

            RecordSchema position = Nested(Nested(table, TiersField), ItemsField);
            Assert.AreEqual(nameof(LootPositionDto), position.TypeName);
            Assert.IsNull(position.IdField);

            VariantSet? shapes = position.Variants;
            Assert.IsNotNull(shapes);
            Assert.IsNull(shapes.Discriminator);
            CollectionAssert.AreEqual(
                new[] { IdField, AugmentsField },
                shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray());
            Assert.AreEqual(nameof(LootPositionByAugmentsDto), shapes.Variants[1].Record.TypeName);
        }

        /// <summary>A position naming an item names it out of three catalogs at once.</summary>
        [TestMethod]
        public void LootTables_LetAPositionNameAnythingThatDrops()
        {
            CatalogSchema schema = new LootTablesDescriptor().Describe(new StubSchemaBuilder());
            RecordSchema position = Nested(Nested(schema.Sections[0].Record, TiersField), ItemsField);
            FieldSchema named = Field(position.Variants!.Variants[0].Record, IdField);

            Assert.AreEqual(FieldKind.Reference, named.Kind);
            CollectionAssert.AreEquivalent(
                new[] { EquipItemsCatalog, ResourcesCatalog, RecipesCatalog },
                named.RefCatalogs.ToArray());
        }

        /// <summary>The modifier sections are told apart by the value of their own key, and each value
        /// brings a different record with it.</summary>
        [TestMethod]
        public void NpcModifiers_AreSectionsToldApartByTheValueOfTheirKey()
        {
            CatalogSchema schema = new NpcModifiersDescriptor().Describe(new StubSchemaBuilder());

            Assert.AreEqual(RootShape.ArrayUnderKey, schema.Shape);
            Assert.AreEqual(ModsKey, schema.Sections[0].Key);

            RecordSchema section = schema.Sections[0].Record;
            Assert.AreEqual(KeyField, section.IdField);

            VariantSet? shapes = section.Variants;
            Assert.IsNotNull(shapes);
            Assert.AreEqual(KeyField, shapes.Discriminator);
            CollectionAssert.AreEqual(
                new[] { ScaleKey, TierUpgradeKey },
                shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray());

            RecordSchema scaled = shapes.Variants[0].Record;
            Assert.AreEqual(nameof(ScaleSectionDto), scaled.TypeName);
            Assert.AreEqual(nameof(ScaleModifierDto), Nested(scaled, ModifiersField).TypeName);
        }

        /// <summary>Nine files, one per slot, and the record itself says which of them it belongs in.</summary>
        [TestMethod]
        public void EquipItems_GoToTheFileNamedByTheirSlot()
        {
            CatalogSchema schema = new EquipItemsDescriptor().Describe(new StubSchemaBuilder());

            Assert.AreEqual(WeaponSlot, schema.Placement.FileFor(name => name == SlotField ? WeaponSlot : null));
        }

        /// <summary>A record whose slot is not filled in yet cannot name its file, and the rule says so
        /// rather than inventing one: the tool falls back to its own choice.</summary>
        [TestMethod]
        public void Placement_ByAFieldTheRecordHasNotFilledIn_LeavesTheChoiceToTheTool()
        {
            FilePlacement placement = new FieldFilePlacement { FieldName = SlotField };

            Assert.IsNull(placement.FileFor(_ => null));
            Assert.IsNull(placement.FileFor(_ => " "));
            Assert.IsNull(new FreeFilePlacement().FileFor(_ => WeaponSlot));
        }

        private static CatalogSchema OneSectionCatalog(RecordSchema record) => new(
            RootShape.ArrayUnderKey,
            [Section(NpcsKey, record)],
            [NameSuffix, DescriptionSuffix],
            new SingleFilePlacement { FileName = NpcCatalog });

        private static CatalogSchema TwoSectionCatalog()
        {
            RecordSchema record = Record(nameof(NpcDto), Text(IdField));
            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [Section(GeneralKey, record), Section(IndividualKey, record)],
                [],
                new FreeFilePlacement());
        }

        private static SectionSchema Section(string key, RecordSchema record) => new() { Key = key, Record = record };

        private static RecordSchema Record(string typeName, params FieldSchema[] fields) =>
            new() { TypeName = typeName, Fields = fields };

        private static VariantSchema Variant(string value, RecordSchema record) =>
            new() { DiscriminatorValue = value, Record = record };

        private static FieldSchema Text(string name) => new() { JsonName = name, Kind = FieldKind.String };

        private static FieldSchema Field(RecordSchema record, string name) =>
            record.Fields.FirstOrDefault(field => field.JsonName == name)
            ?? throw new AssertFailedException($"{record.TypeName} has no field '{name}'.");

        /// <summary>The record of the array under the named field — how a test walks down to something
        /// the descriptor never assembled itself.</summary>
        private static RecordSchema Nested(RecordSchema record, string name) =>
            Field(record, name).Item?.Record
            ?? throw new AssertFailedException($"{record.TypeName}.{name} holds no records.");

        private static IReadOnlyList<Type> ModelTypes() =>
            [.. typeof(CatalogSchema).Assembly.GetTypes().Where(type => type.IsPublic && type.Namespace == ModelNamespace)];

        private static IReadOnlyList<Type> SchemaAttributes() =>
            [.. typeof(CatalogRefAttribute).Assembly.GetTypes().Where(type => type.IsPublic && type.IsSubclassOf(typeof(Attribute)))];

        private static PropertyInfo Property(Type dto, string name) =>
            dto.GetProperty(name) ?? throw new AssertFailedException($"{dto.Name} has no property {name}.");

        private static T Attribute<T>(Type dto, string property) where T : Attribute =>
            Property(dto, property).GetCustomAttribute<T>() ?? throw new AssertFailedException($"{dto.Name}.{property} carries no {typeof(T).Name}.");

        private enum TestFraction
        {
            Human,
            Undead,
            Animal
        }

        private enum TestParameter
        {
            Health,
            Damage,
            Armor
        }

        private enum TestUniqueScope
        {
            Group,
            Id
        }

        private enum TestRarity
        {
            Legendary,
            Common
        }

        private enum TestSlot
        {
            Weapon,
            Helmet,
            Ring
        }

        private sealed record NpcDto
        {
            public string Id { get; init; } = string.Empty;

            [EnumOf(typeof(TestFraction))] public string Fraction { get; init; } = string.Empty;

            [CatalogRef(AbilitiesCatalog)] public List<string> Abilities { get; init; } = [];

            [DictionaryKey(typeof(TestParameter))] public Dictionary<string, float> BaseParameters { get; init; } = [];

            [DictionaryKey(AbilitiesCatalog)] public Dictionary<string, float> AbilityWeights { get; init; } = [];

            [Hidden] public int Version { get; init; }
        }

        private sealed record LootTableDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            public List<LootTierDto> Tiers { get; init; } = [];
        }

        private sealed record LootTierDto
        {
            [Range(MinTier, MaxTier)] public int Tier { get; init; }

            public List<LootPositionDto> Items { get; init; } = [];
        }

        /// <summary>What every position has whatever shape it takes; the shapes themselves are the two
        /// below.</summary>
        private sealed record LootPositionDto
        {
            public float Price { get; init; }
        }

        private sealed record LootPositionByIdDto
        {
            [CatalogRef(EquipItemsCatalog)]
            [CatalogRef(ResourcesCatalog)]
            [CatalogRef(RecipesCatalog)]
            public string Id { get; init; } = string.Empty;

            public float Price { get; init; }
        }

        private sealed record LootPositionByAugmentsDto
        {
            public AugmentGroupDto? Augments { get; init; }

            public float Price { get; init; }
        }

        private sealed record AugmentGroupDto
        {
            [Range(MinTier, MaxTier)] public int Tier { get; init; }

            [EnumOf(typeof(TestRarity))] public string Rarity { get; init; } = string.Empty;
        }

        private sealed record NpcModifiersDocumentDto
        {
            [Discriminator(KeyField)] public List<NpcModifierSectionDto> Mods { get; init; } = [];
        }

        private sealed record NpcModifierSectionDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            [EnumOf(typeof(TestUniqueScope))] public string UniqueScope { get; init; } = string.Empty;

            public List<NpcModifierDto> Modifiers { get; init; } = [];
        }

        private sealed record ScaleSectionDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            [EnumOf(typeof(TestUniqueScope))] public string UniqueScope { get; init; } = string.Empty;

            public List<ScaleModifierDto> Modifiers { get; init; } = [];
        }

        private sealed record TierUpgradeSectionDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            [EnumOf(typeof(TestUniqueScope))] public string UniqueScope { get; init; } = string.Empty;

            public List<TierUpgradeModifierDto> Modifiers { get; init; } = [];
        }

        private record NpcModifierDto
        {
            public string Id { get; init; } = string.Empty;

            [CatalogRef(NpcBuffsCatalog, AllowEmpty = true)] public string NpcBuffId { get; init; } = string.Empty;

            public float Weight { get; init; }
        }

        private sealed record ScaleModifierDto : NpcModifierDto
        {
            public float Scale { get; init; }
        }

        private sealed record TierUpgradeModifierDto : NpcModifierDto
        {
            public float TierUpgradeChance { get; init; }
        }

        private sealed record EquipItemDto
        {
            public string Id { get; init; } = string.Empty;

            [EnumOf(typeof(TestSlot))] public string Slot { get; init; } = string.Empty;
        }

        /// <summary>Stands for a dialogue node: the one place a field IS a localization key rather than
        /// deriving one from an id.</summary>
        private sealed record DialogueNodeDto
        {
            [NotARef] public string NodeId { get; init; } = string.Empty;

            [LocalizedKey] public string Text { get; init; } = string.Empty;

            [LocalizedKey(DescriptionSuffix)] public string Summary { get; init; } = string.Empty;

            [CatalogRef(NpcCatalog)] public string SpeakerId { get; init; } = string.Empty;
        }

        /// <summary>
        /// Stands in for the reflector: hands back the json shape of a test DTO and hangs the registered
        /// shapes on every record of that type, wherever the walk reaches it. The shapes are written out
        /// here rather than read off the DTOs — reading them off is the reflector's job, and this has to
        /// be able to fail independently of it.
        /// </summary>
        private sealed class StubSchemaBuilder : ISchemaBuilder
        {
            private readonly Dictionary<Type, VariantSet> _variants = [];

            public void Polymorphic(Type dtoType, VariantSet variants) => _variants[dtoType] = variants;

            public RecordSchema Record(Type dtoType)
            {
                RecordSchema record = Shape(dtoType);
                return _variants.TryGetValue(dtoType, out VariantSet? variants) ? record with { Variants = variants } : record;
            }

            private static FieldSchema Reference(string name, params string[] catalogs) =>
                new() { JsonName = name, Kind = FieldKind.Reference, RefCatalogs = catalogs };

            private static FieldSchema Choice<T>(string name) where T : struct, Enum =>
                new() { JsonName = name, Kind = FieldKind.Enum, EnumValues = Enum.GetNames<T>() };

            private static FieldSchema Number(string name) => new() { JsonName = name, Kind = FieldKind.Number };

            private static FieldSchema Whole(string name) => new() { JsonName = name, Kind = FieldKind.Integer };

            private static FieldSchema Words(string name) => new() { JsonName = name, Kind = FieldKind.String };

            private FieldSchema ArrayOf(string name, Type element) => new()
            {
                JsonName = name,
                Kind = FieldKind.Array,
                Item = new FieldSchema { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Object, Record = Record(element) }
            };

            private FieldSchema ObjectOf(string name, Type nested) => new()
            {
                JsonName = name,
                Kind = FieldKind.Object,
                Record = Record(nested)
            };

            private RecordSchema Shape(Type dtoType)
            {
                if (dtoType == typeof(NpcDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(NpcDto),
                        IdField = IdField,
                        Fields =
                        [
                            Words(IdField),
                            Choice<TestFraction>(FractionField),
                            new FieldSchema
                            {
                                JsonName = AbilitiesField,
                                Kind = FieldKind.Array,
                                Item = Reference(FieldSchema.Unnamed, AbilitiesCatalog)
                            },
                            new FieldSchema
                            {
                                JsonName = BaseParametersField,
                                Kind = FieldKind.Dictionary,
                                Key = Choice<TestParameter>(FieldSchema.Unnamed),
                                Item = Number(FieldSchema.Unnamed)
                            }
                        ]
                    };

                if (dtoType == typeof(LootTableDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(LootTableDto),
                        IdField = KeyField,
                        Fields = [Words(KeyField), ArrayOf(TiersField, typeof(LootTierDto))]
                    };

                if (dtoType == typeof(LootTierDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(LootTierDto),
                        Fields = [Whole(TierField), ArrayOf(ItemsField, typeof(LootPositionDto))]
                    };

                if (dtoType == typeof(LootPositionDto))
                    return new RecordSchema { TypeName = nameof(LootPositionDto), Fields = [Number(PriceField)] };

                if (dtoType == typeof(LootPositionByIdDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(LootPositionByIdDto),
                        Fields = [Reference(IdField, EquipItemsCatalog, ResourcesCatalog, RecipesCatalog), Number(PriceField)]
                    };

                if (dtoType == typeof(LootPositionByAugmentsDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(LootPositionByAugmentsDto),
                        Fields = [ObjectOf(AugmentsField, typeof(AugmentGroupDto)), Number(PriceField)]
                    };

                if (dtoType == typeof(AugmentGroupDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(AugmentGroupDto),
                        Fields = [Whole(TierField), Choice<TestRarity>(RarityField)]
                    };

                if (dtoType == typeof(NpcModifierSectionDto))
                    return Section(nameof(NpcModifierSectionDto), typeof(NpcModifierDto));

                if (dtoType == typeof(ScaleSectionDto))
                    return Section(nameof(ScaleSectionDto), typeof(ScaleModifierDto));

                if (dtoType == typeof(TierUpgradeSectionDto))
                    return Section(nameof(TierUpgradeSectionDto), typeof(TierUpgradeModifierDto));

                if (dtoType == typeof(NpcModifierDto))
                    return new RecordSchema { TypeName = nameof(NpcModifierDto), IdField = IdField, Fields = [.. ModifierFields()] };

                if (dtoType == typeof(ScaleModifierDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(ScaleModifierDto),
                        IdField = IdField,
                        Fields = [.. ModifierFields(), Number(ScaleField)]
                    };

                if (dtoType == typeof(TierUpgradeModifierDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(TierUpgradeModifierDto),
                        IdField = IdField,
                        Fields = [.. ModifierFields(), Number(TierUpgradeChanceField)]
                    };

                if (dtoType == typeof(EquipItemDto))
                    return new RecordSchema
                    {
                        TypeName = nameof(EquipItemDto),
                        IdField = IdField,
                        Fields = [Words(IdField), Choice<TestSlot>(SlotField)]
                    };

                throw new AssertFailedException($"The stub has no shape written for {dtoType.Name}.");
            }

            private static IEnumerable<FieldSchema> ModifierFields() =>
                [Words(IdField), Reference(NpcBuffIdField, NpcBuffsCatalog), Number(WeightField)];

            private RecordSchema Section(string typeName, Type modifier) => new()
            {
                TypeName = typeName,
                IdField = KeyField,
                Fields = [Words(KeyField), Choice<TestUniqueScope>(UniqueScopeField), ArrayOf(ModifiersField, modifier)]
            };
        }

        private sealed class NpcDescriptor : ICatalogDescriptor
        {
            public string Catalog => NpcCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(NpcsKey, builder.Record(typeof(NpcDto)))],
                [NameSuffix, DescriptionSuffix],
                new SingleFilePlacement { FileName = NpcCatalog });
        }

        /// <summary>Four sections of tables, each table a list of tiers, each tier a list of positions —
        /// and the position is the thing that takes two shapes.</summary>
        private sealed class LootTablesDescriptor : ICatalogDescriptor
        {
            public string Catalog => LootTablesCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                builder.Polymorphic(typeof(LootPositionDto), new VariantSet(
                [
                    Variant(IdField, builder.Record(typeof(LootPositionByIdDto))),
                    Variant(AugmentsField, builder.Record(typeof(LootPositionByAugmentsDto)))
                ]));

                RecordSchema table = builder.Record(typeof(LootTableDto));

                return new CatalogSchema(
                    RootShape.SectionsOfArrays,
                    [
                        Section(GeneralKey, table),
                        Section(FractionsKey, table),
                        Section(TypesKey, table),
                        Section(IndividualKey, table)
                    ],
                    [],
                    new SingleFilePlacement { FileName = LootTablesCatalog });
            }
        }

        private sealed class NpcModifiersDescriptor : ICatalogDescriptor
        {
            public string Catalog => NpcModifiersCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                builder.Polymorphic(typeof(NpcModifierSectionDto), new VariantSet(
                    [
                        Variant(ScaleKey, builder.Record(typeof(ScaleSectionDto))),
                        Variant(TierUpgradeKey, builder.Record(typeof(TierUpgradeSectionDto)))
                    ],
                    KeyField));

                return new CatalogSchema(
                    RootShape.ArrayUnderKey,
                    [Section(ModsKey, builder.Record(typeof(NpcModifierSectionDto)))],
                    [],
                    new SingleFilePlacement { FileName = NpcModifiersCatalog });
            }
        }

        private sealed class EquipItemsDescriptor : ICatalogDescriptor
        {
            public string Catalog => EquipItemsCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(ItemsField, builder.Record(typeof(EquipItemDto)))],
                [NameSuffix, DescriptionSuffix],
                new FieldFilePlacement { FieldName = SlotField });
        }
    }
}
