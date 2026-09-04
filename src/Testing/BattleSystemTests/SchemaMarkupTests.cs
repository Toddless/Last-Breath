namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using Core.Data.Schema;

    /// <summary>
    /// The data markup the game writes on its DTOs, and the game's half of the naming convention the
    /// authoring tool reads it by. The tool does not reference the game, so it recognises a piece of
    /// markup by the NAME of its type and reads what it carries by the NAMES of its properties: a rename
    /// here costs nothing at build time and shows up as an editor quietly offering a text box.
    /// <para>The names below are spelt out a second time on purpose — the tool spells them out for
    /// itself. Two lists that must agree is what a convention is; one list shared between them would be
    /// the reference this whole split exists to remove.</para>
    /// </summary>
    [TestClass]
    public class SchemaMarkupTests
    {
        private const string CatalogRefName = "CatalogRefAttribute";
        private const string NotARefName = "NotARefAttribute";
        private const string EnumOfName = "EnumOfAttribute";
        private const string DictionaryKeyName = "DictionaryKeyAttribute";
        private const string RangeName = "RangeAttribute";
        private const string LocalizedKeyName = "LocalizedKeyAttribute";
        private const string DiscriminatorName = "DiscriminatorAttribute";
        private const string HiddenName = "HiddenAttribute";
        private const string SuggestsName = "SuggestsAttribute";

        private const string CatalogProperty = "Catalog";
        private const string SectionProperty = "Section";
        private const string AllowEmptyProperty = "AllowEmpty";
        private const string EnumTypeProperty = "EnumType";
        private const string MinProperty = "Min";
        private const string MaxProperty = "Max";
        private const string SuffixProperty = "Suffix";
        private const string FieldProperty = "Field";
        private const string SourceProperty = "Source";

        /// <summary>Markup that is the whole answer by being written at all.</summary>
        private const string NothingCarried = "";

        private const string MarkupNamespace = "Core.Data.Schema";

        private const string AbilitiesCatalog = "Abilities";
        private const string NpcBuffsCatalog = "NpcBuffs";
        private const string EquipItemsCatalog = "EquipItems";
        private const string ResourcesCatalog = "Resources";
        private const string RecipesCatalog = "Recipes";

        /// <summary>The section of the abilities catalog a list of casts points into — a catalog holding
        /// more than the casts is what narrowing a reference to one section is for.</summary>
        private const string CastsSection = "abilities";

        private const string DescriptionSuffix = "_Description";

        private const string KindField = "kind";

        private const double MinTier = 0d;
        private const double MaxTier = 3d;

        /// <summary>The game's half of the naming convention: every attribute the tool reads by name is
        /// called that, carries what it is read for, and carries it as the kind of thing the tool asks
        /// for. Nothing here is checked against the tool's own list — that is the point.</summary>
        [TestMethod]
        public void Markup_CarriesTheNamesTheToolReadsItBy()
        {
            foreach ((string attribute, string property, Type carried) in Convention())
            {
                Type markup = Markup(attribute);

                if (property.Length == 0) continue;

                PropertyInfo? held = markup.GetProperty(property);

                Assert.IsNotNull(held, $"{attribute} carries no '{property}'.");
                Assert.AreEqual(carried, Nullable.GetUnderlyingType(held.PropertyType) ?? held.PropertyType, $"{attribute}.{property}");
            }
        }

        /// <summary>Markup the tool has never been told about reads to the author as a field nobody has
        /// marked up yet, and the schema comes out with a text box where a picker belongs.</summary>
        [TestMethod]
        public void Markup_IsExactlyTheAttributesTheConventionNames()
        {
            CollectionAssert.AreEquivalent(
                Convention().Select(part => part.Attribute).Distinct().ToArray(),
                MarkupTypes().Select(markup => markup.Name).ToArray());
        }

        /// <summary>The markup describes fields of a record and nothing else; anywhere else it would be
        /// read by nobody. The two that may be written more than once are the two answering a question a
        /// field can answer twice: which catalogs it may point into.</summary>
        [TestMethod]
        public void Markup_TargetsPropertiesAndFieldsOnly()
        {
            IReadOnlyList<Type> attributes = MarkupTypes();

            Assert.AreNotEqual(0, attributes.Count);
            foreach (Type attribute in attributes)
            {
                AttributeUsageAttribute? usage = attribute.GetCustomAttribute<AttributeUsageAttribute>();

                Assert.IsNotNull(usage, $"{attribute.Name} does not say where it may be written.");
                Assert.AreEqual(AttributeTargets.Property | AttributeTargets.Field, usage.ValidOn, attribute.Name);
                Assert.IsTrue(attribute.IsSealed, $"{attribute.Name} is open to inheritance.");
                Assert.AreEqual(RepeatedMarkup.Contains(attribute.Name), usage.AllowMultiple, attribute.Name);
            }
        }

        /// <summary>The tool reads the markup off the properties and draws a picker, a range or a text
        /// box accordingly. Read back one by one: an attribute carrying the wrong argument fails here
        /// and nowhere earlier.</summary>
        [TestMethod]
        public void Markup_IsReadBackFromTheMarkedProperties()
        {
            Assert.AreEqual(AbilitiesCatalog, Written<CatalogRefAttribute>(nameof(MarkedDto.Abilities)).Catalog);
            Assert.AreEqual(CastsSection, Written<CatalogRefAttribute>(nameof(MarkedDto.Abilities)).Section);
            Assert.IsFalse(Written<CatalogRefAttribute>(nameof(MarkedDto.Abilities)).AllowEmpty);
            Assert.IsNull(Written<CatalogRefAttribute>(nameof(MarkedDto.NpcBuffId)).Section, "a reference names no section unless it was narrowed to one");
            Assert.IsTrue(Written<CatalogRefAttribute>(nameof(MarkedDto.NpcBuffId)).AllowEmpty);
            Assert.AreEqual(typeof(TestFraction), Written<EnumOfAttribute>(nameof(MarkedDto.Fraction)).EnumType);
            Assert.AreEqual(MinTier, Written<RangeAttribute>(nameof(MarkedDto.Tier)).Min);
            Assert.AreEqual(MaxTier, Written<RangeAttribute>(nameof(MarkedDto.Tier)).Max);
            Assert.AreEqual(LocalizedKeyAttribute.NoSuffix, Written<LocalizedKeyAttribute>(nameof(MarkedDto.Title)).Suffix);
            Assert.AreEqual(DescriptionSuffix, Written<LocalizedKeyAttribute>(nameof(MarkedDto.Summary)).Suffix);
            Assert.AreEqual(KindField, Written<DiscriminatorAttribute>(nameof(MarkedDto.Positions)).Field);
            Assert.AreEqual(typeof(TestParameter), Written<DictionaryKeyAttribute>(nameof(MarkedDto.BaseParameters)).EnumType);
            Assert.IsNull(Written<DictionaryKeyAttribute>(nameof(MarkedDto.BaseParameters)).Catalog);
            Assert.AreEqual(AbilitiesCatalog, Written<DictionaryKeyAttribute>(nameof(MarkedDto.AbilityWeights)).Catalog);
            Assert.IsNull(Written<DictionaryKeyAttribute>(nameof(MarkedDto.AbilityWeights)).EnumType);
            Assert.AreEqual(SuggestionSources.FactKeys, Written<SuggestsAttribute>(nameof(MarkedDto.Fact)).Source);
            Assert.IsNotNull(Written<NotARefAttribute>(nameof(MarkedDto.Key)));
            Assert.IsNotNull(Written<HiddenAttribute>(nameof(MarkedDto.Version)));
        }

        /// <summary>One field, several catalogs. A loot position's id names a piece of equipment, a
        /// crafting resource or a recipe, and a field allowed only one of them would report two thirds
        /// of every loot table as pointing at nothing.</summary>
        [TestMethod]
        public void CatalogRef_NamesEveryCatalogTheFieldMayPointInto()
        {
            IEnumerable<CatalogRefAttribute> references = Property(nameof(MarkedDto.Id)).GetCustomAttributes<CatalogRefAttribute>();

            CollectionAssert.AreEquivalent(
                new[] { EquipItemsCatalog, ResourcesCatalog, RecipesCatalog },
                references.Select(reference => reference.Catalog).ToArray());
        }

        /// <summary>An unmarked property answers nothing, which is what lets the tool tell a decision
        /// from a silence.</summary>
        [TestMethod]
        public void Markup_IsAbsentFromWhateverWasNotMarked()
        {
            PropertyInfo property = Property(nameof(MarkedDto.Price));

            Assert.IsNull(property.GetCustomAttribute<CatalogRefAttribute>());
            Assert.IsNull(property.GetCustomAttribute<NotARefAttribute>());
            Assert.IsNull(property.GetCustomAttribute<DictionaryKeyAttribute>());
            Assert.IsNull(property.GetCustomAttribute<HiddenAttribute>());
        }

        [TestMethod]
        public void CatalogRef_NamingNoCatalog_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new CatalogRefAttribute(" "));

        /// <summary>A field pointed at something that is not an enum would offer an empty list of
        /// members, which reads to the author as "this one has no values yet".</summary>
        [TestMethod]
        public void EnumOf_SomethingThatIsNotAnEnum_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new EnumOfAttribute(typeof(MarkedDto)));

        /// <summary>Keys are described by their own markup, which is refused on the same terms as the markup
        /// describing values: an empty catalog names nothing, and a type with no members offers nothing.</summary>
        [TestMethod]
        public void DictionaryKey_NamingNoCatalog_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new DictionaryKeyAttribute(" "));

        [TestMethod]
        public void DictionaryKey_OfSomethingThatIsNotAnEnum_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new DictionaryKeyAttribute(typeof(MarkedDto)));

        /// <summary>A field answered from a list with no name is answered by nobody: the tool matches the
        /// source by the word written here, and a blank one would offer an empty list forever.</summary>
        [TestMethod]
        public void Suggests_NamingNoSource_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new SuggestsAttribute(" "));

        [TestMethod]
        public void Discriminator_NamingNoField_IsRefused() =>
            Assert.ThrowsException<ArgumentException>(() => new DiscriminatorAttribute(" "));

        [TestMethod]
        public void Range_EndingBelowItsStart_IsRefused() =>
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new RangeAttribute(MaxTier, MinTier));

        /// <summary>The markup that may be written more than once on one member.</summary>
        private static IReadOnlyList<string> RepeatedMarkup => [CatalogRefName, DictionaryKeyName];

        /// <summary>The markup by name: what each attribute is called, what it carries and of what type.
        /// One that carries nothing is named with no property.</summary>
        private static IEnumerable<(string Attribute, string Property, Type Carried)> Convention() =>
        [
            (CatalogRefName, CatalogProperty, typeof(string)),
            (CatalogRefName, SectionProperty, typeof(string)),
            (CatalogRefName, AllowEmptyProperty, typeof(bool)),
            (DictionaryKeyName, CatalogProperty, typeof(string)),
            (DictionaryKeyName, EnumTypeProperty, typeof(Type)),
            (EnumOfName, EnumTypeProperty, typeof(Type)),
            (RangeName, MinProperty, typeof(double)),
            (RangeName, MaxProperty, typeof(double)),
            (LocalizedKeyName, SuffixProperty, typeof(string)),
            (DiscriminatorName, FieldProperty, typeof(string)),
            (SuggestsName, SourceProperty, typeof(string)),
            (NotARefName, NothingCarried, typeof(void)),
            (HiddenName, NothingCarried, typeof(void))
        ];

        private static IReadOnlyList<Type> MarkupTypes() =>
            [.. typeof(CatalogRefAttribute).Assembly.GetTypes()
                .Where(type => type.Namespace == MarkupNamespace && type.IsSubclassOf(typeof(Attribute)))];

        private static Type Markup(string name) =>
            MarkupTypes().FirstOrDefault(markup => markup.Name == name)
            ?? throw new AssertFailedException($"No markup is called '{name}'.");

        private static PropertyInfo Property(string name) =>
            typeof(MarkedDto).GetProperty(name) ?? throw new AssertFailedException($"MarkedDto has no property {name}.");

        private static T Written<T>(string property) where T : Attribute =>
            Property(property).GetCustomAttribute<T>()
            ?? throw new AssertFailedException($"MarkedDto.{property} carries no {typeof(T).Name}.");

        private enum TestFraction
        {
            Human,
            Undead
        }

        private enum TestParameter
        {
            Health,
            Damage
        }

        /// <summary>Every piece of markup on one record, written the way the game's own DTOs write it.
        /// Standing in for them on purpose: what the real catalogs say is the schema tests' question, and
        /// this one is only whether the markup carries what it was given.</summary>
        private sealed record MarkedDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            /// <summary>Answered from an open list of words rather than from a catalog: what the list does
            /// not know is still written, which is what tells a suggestion from a reference.</summary>
            [NotARef][Suggests(SuggestionSources.FactKeys)] public string Fact { get; init; } = string.Empty;

            [CatalogRef(EquipItemsCatalog), CatalogRef(ResourcesCatalog), CatalogRef(RecipesCatalog)]
            public string Id { get; init; } = string.Empty;

            [CatalogRef(AbilitiesCatalog, Section = CastsSection)] public List<string> Abilities { get; init; } = [];

            [CatalogRef(NpcBuffsCatalog, AllowEmpty = true)] public string NpcBuffId { get; init; } = string.Empty;

            [EnumOf(typeof(TestFraction))] public string Fraction { get; init; } = string.Empty;

            [DictionaryKey(typeof(TestParameter))] public Dictionary<string, float> BaseParameters { get; init; } = [];

            [DictionaryKey(AbilitiesCatalog)] public Dictionary<string, float> AbilityWeights { get; init; } = [];

            [Range(MinTier, MaxTier)] public int Tier { get; init; }

            [LocalizedKey] public string Title { get; init; } = string.Empty;

            [LocalizedKey(DescriptionSuffix)] public string Summary { get; init; } = string.Empty;

            [Discriminator(KindField)] public List<string> Positions { get; init; } = [];

            [Hidden] public int Version { get; init; }

            public float Price { get; init; }
        }
    }
}
