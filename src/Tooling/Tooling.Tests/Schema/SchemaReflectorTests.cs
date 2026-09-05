namespace Tooling.Tests.Schema
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// Reading a DTO into a schema. The DTOs here repeat the shapes the game's catalogs actually take —
    /// an NPC record with a list of ids and a map of parameters, a loot table whose positions sit two
    /// arrays down and take two shapes, a record that leads back to itself — because the promise is that
    /// the tool draws what the game will parse, and the only proof of that is the shapes it parses.
    /// </summary>
    [TestClass]
    public class SchemaReflectorTests
    {
        private const string AbilitiesCatalog = "Abilities";
        private const string EquipItemsCatalog = "EquipItems";
        private const string ResourcesCatalog = "Resources";
        private const string NpcBuffsCatalog = "NpcBuffs";

        private const string IdField = "id";
        private const string KeyField = "key";
        private const string FractionField = "fraction";
        private const string AbilitiesField = "abilities";
        private const string BaseParametersField = "baseParameters";
        private const string ParametersField = "parameters";
        private const string AuthoredField = "authored";
        private const string ConditionsField = "acceptConditions";
        private const string LevelField = "lvl";
        private const string AuthoredLevelField = "level";
        private const string ScalingField = "levelScaling";
        private const string AggressiveField = "aggressive";
        private const string VersionField = "version";
        private const string NpcIdField = "npcId";
        private const string NoteField = "note";
        private const string BuffField = "npcBuffId";
        private const string CategoryField = "categoryId";
        private const string SectionlessField = "sectionless";
        private const string CataloglessField = "catalogless";

        /// <summary>The section of the resources catalog a category is written in — a catalog whose
        /// sections answer to nothing each other is what narrowing a reference is for.</summary>
        private const string CategoriesKey = "materialCategories";

        private const string TitleField = "title";
        private const string SummaryField = "summary";
        private const string TiersField = "tiers";
        private const string ItemsField = "items";
        private const string PriceField = "price";
        private const string AmountField = "amount";
        private const string TierField = "tier";
        private const string AugmentsField = "augments";
        private const string ChildrenField = "children";
        private const string ParentField = "parent";
        private const string LooseField = "loose";
        private const string AnythingField = "anything";
        private const string BrokenField = "broken";
        private const string WeightsField = "weights";
        private const string NameField = "name";
        private const string ScaleField = "scale";
        private const string ModifiersField = "modifiers";
        private const string TagsField = "tags";
        private const string NamesADropField = "namesADrop";
        private const string FragileField = "fragile";
        private const string ValueField = "value";
        private const string RarityField = "rarity";
        private const string PrimaryField = "primary";
        private const string FallbackField = "fallback";
        private const string PassivesField = "passives";
        private const string FactField = "fact";
        private const string FactsField = "facts";
        private const string NamedField = "named";

        /// <summary>The list of words a suggested field is answered from, as a test names one.</summary>
        private const string FactsSource = "factKeys";

        /// <summary>Enough of a note to tell it from the others said about the same field.</summary>
        private const string SaidComputed = "worked out from other fields";
        private const string SaidThrew = "threw when it was read";
        private const string SaidReadThrough = "is read through";
        private const string SaidMap = "is a map";
        private const string SaidTwice = "registered twice";
        private const string SaidNoArguments = "cannot be built without arguments";
        private const string SaidNotText = "is not text";
        private const string SaidNotANumber = "is not a number";
        private const string SaidNotAMap = "is not a map";
        private const string SaidNotSuggestable = "the words it is answered from";
        private const string SaidSuggestedAndNarrowed = "nothing offers them under a narrowed field";
        private const string SaidAlreadyKeyed = "rather than by words";
        private const string SaidKeyedTwice = "keyed both by";

        private const string DescriptionSuffix = "_Description";

        private const string NpcsKey = "npcs";
        private const string GeneralKey = "general";
        private const string IndividualKey = "individual";

        private const string NpcCatalog = "Npc";
        private const string LootTablesCatalog = "LootTables";

        private const int MinTier = 0;
        private const int MaxTier = 3;

        /// <summary>The ends of a range written as words: what markup carrying the right name and the
        /// wrong kind of thing under it holds.</summary>
        private const string TierFloor = "0";
        private const string TierCeiling = "3";

        private const int AuthoredLevel = 7;
        private const float NoScaling = 0f;

        private SchemaReflector _reflector = null!;

        [TestInitialize]
        public void CreateReflector() => _reflector = new SchemaReflector();

        /// <summary>The order of the fields is the order of a canonical file, so it is the order the type
        /// declares them in and nothing else.</summary>
        [TestMethod]
        public void Fields_StandInTheOrderTheTypeDeclaresThem()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));

            CollectionAssert.AreEqual(
                new[]
                {
                    IdField, NpcIdField, FractionField, AbilitiesField, BaseParametersField, ParametersField,
                    AuthoredField, ConditionsField, LevelField, ScalingField, AggressiveField, VersionField
                },
                Names(record));
            Assert.AreEqual(nameof(NpcDto), record.TypeName);
        }

        /// <summary>A member says its json name, or the serializer's naming strategy makes one of the name
        /// it has. Both are read here the way the game's own serializer reads them.</summary>
        [TestMethod]
        public void AJsonName_IsTheOneWrittenDown_OrTheCamelCaseOfTheMembersOwn()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));

            Assert.AreEqual(FieldKind.Integer, Field(record, LevelField).Kind, "the name written on the member wins");
            Assert.IsNotNull(Field(record, NpcIdField), "and a run of capitals comes out the way the strategy makes it");
        }

        [TestMethod]
        public void AMemberTheSerializerIgnores_IsNotInTheSchema() =>
            Assert.IsFalse(Names(_reflector.Record(typeof(NpcDto))).Contains(NoteField));

        /// <summary>What a string means is written on it: the catalog its id points into, the enum whose
        /// members it holds, the localization key it stands for.</summary>
        [TestMethod]
        public void WhatAStringHolds_IsReadFromTheMarkupOnIt()
        {
            RecordSchema npc = _reflector.Record(typeof(NpcDto));
            RecordSchema text = _reflector.Record(typeof(TextDto));

            FieldSchema fraction = Field(npc, FractionField);
            Assert.AreEqual(FieldKind.Enum, fraction.Kind);
            CollectionAssert.AreEqual(Enum.GetNames<TestFraction>(), fraction.EnumValues.ToArray());

            Assert.AreEqual(FieldKind.LocalizedKey, Field(text, TitleField).Kind);
            Assert.AreEqual(LocalizedKeyAttribute.NoSuffix, Field(text, TitleField).LocalizationSuffix);
            Assert.AreEqual(DescriptionSuffix, Field(text, SummaryField).LocalizationSuffix);
            Assert.AreEqual(FieldKind.String, Field(text, KeyField).Kind, "a refusal is not a reference");
        }

        /// <summary>A list of ids is a list, and the ids are what point into a catalog: markup that says what
        /// a value means belongs to the value, however deep it sits.</summary>
        [TestMethod]
        public void AListOfIds_CarriesTheCatalogOnItsElements()
        {
            FieldSchema abilities = Field(_reflector.Record(typeof(NpcDto)), AbilitiesField);

            Assert.AreEqual(FieldKind.Array, abilities.Kind);
            Assert.IsNotNull(abilities.Item);
            Assert.AreEqual(FieldKind.Reference, abilities.Item.Kind);
            Assert.AreEqual(FieldSchema.Unnamed, abilities.Item.JsonName);
            CollectionAssert.AreEqual(new[] { AbilitiesCatalog }, Catalogs(abilities.Item));
        }

        /// <summary>One field, several catalogs, and an empty value that names nothing on purpose.</summary>
        [TestMethod]
        public void AReference_NamesEveryCatalogItMayPointInto_AndWhetherEmptyIsLegal()
        {
            RecordSchema record = _reflector.Record(typeof(TextDto));
            FieldSchema buff = Field(record, BuffField);

            CollectionAssert.AreEquivalent(new[] { EquipItemsCatalog, ResourcesCatalog }, Catalogs(Field(record, IdField)));
            Assert.IsFalse(Field(record, IdField).AllowEmpty);
            Assert.IsTrue(buff.AllowEmpty);
            CollectionAssert.AreEqual(new[] { NpcBuffsCatalog }, Catalogs(buff));
        }

        /// <summary>A reference narrowed to one section of a catalog says so; one that names no section
        /// points into the whole of it. The two have to be told apart on the field: a catalog whose
        /// sections answer to nothing each other would otherwise offer the author ids the game drops.</summary>
        [TestMethod]
        public void AReferenceNarrowedToASection_CarriesTheSectionOntoTheField()
        {
            RecordSchema record = _reflector.Record(typeof(TextDto));

            Assert.AreEqual(
                new ReferenceTarget(ResourcesCatalog, CategoriesKey),
                Field(record, CategoryField).RefTargets[0]);
            Assert.IsTrue(Field(record, BuffField).RefTargets.All(target => target.Section is null),
                "a reference naming no section was narrowed to one");
            NothingSaid(nameof(TextDto), CategoryField);
        }

        /// <summary>A section named as a blank word narrows the field to a key no file is written under,
        /// which would report every id in it as broken. Read as the whole catalog, and said out loud: a
        /// narrowing that quietly does nothing looks exactly like a field nobody has narrowed.</summary>
        [TestMethod]
        public void AReferenceNarrowedToANamelessSection_PointsIntoTheWholeCatalog_AndIsSaidOutLoud()
        {
            FieldSchema field = Field(_reflector.Record(typeof(AwkwardDto)), SectionlessField);

            Assert.AreEqual(FieldKind.Reference, field.Kind);
            Assert.AreEqual(ReferenceTarget.Whole(AbilitiesCatalog), field.RefTargets[0]);
            Said(nameof(AwkwardDto), SectionlessField, AbilitiesCatalog);
        }

        /// <summary>A catalog named as a blank word is no catalog: the reference is dropped rather than
        /// answered from nothing, and said out loud on the same terms a nameless section is. The writing
        /// side refuses it where it is written, and the two halves of the contract have to agree even
        /// when one of them is markup this build has never seen.</summary>
        [TestMethod]
        public void AReferenceIntoANamelessCatalog_IsNotReadAtAll_AndIsSaidOutLoud()
        {
            FieldSchema field = Field(_reflector.Record(typeof(AwkwardDto)), CataloglessField);

            Assert.AreEqual(FieldKind.String, field.Kind, "a reference into no catalog was drawn as one");
            Assert.AreEqual(0, field.RefTargets.Count);
            Said(nameof(AwkwardDto), CataloglessField);
        }

        /// <summary>A refusal is a decision and is written down as one. A field that reads as unmarked is a
        /// field nothing can check for having been thought about at all.</summary>
        [TestMethod]
        public void ARefusalOfAReference_IsCarriedOntoTheField()
        {
            RecordSchema record = _reflector.Record(typeof(TextDto));
            FieldSchema key = Field(record, KeyField);

            Assert.AreEqual(FieldKind.String, key.Kind);
            Assert.IsTrue(key.RefusedAsReference);
            Assert.IsFalse(Field(record, TitleField).RefusedAsReference, "and nothing is refused where nothing was written");
        }

        /// <summary>A list of strings carries the refusal where it carries everything else said about them:
        /// on the elements, which are what an id would have been.</summary>
        [TestMethod]
        public void ARefusalOnAListOfStrings_IsCarriedOntoItsElements()
        {
            FieldSchema tags = Field(_reflector.Record(typeof(TextDto)), TagsField);

            Assert.AreEqual(FieldKind.Array, tags.Kind);
            Assert.IsFalse(tags.RefusedAsReference, "the list is not the thing that could have been an id");
            Assert.AreEqual(FieldKind.String, tags.Item?.Kind);
            Assert.IsTrue(tags.Item?.RefusedAsReference);
        }

        /// <summary>The words a field is answered from ride where everything else said about a string does:
        /// on the field, and on the elements of a list of them. They narrow nothing — the field stays plain
        /// text, because a word the list does not know is written and read exactly as any other.</summary>
        [TestMethod]
        public void TheWordsAFieldIsAnsweredFrom_AreCarriedOntoIt()
        {
            RecordSchema record = _reflector.Record(typeof(TextDto));
            FieldSchema fact = Field(record, FactField);
            FieldSchema facts = Field(record, FactsField);

            Assert.AreEqual(FieldKind.String, fact.Kind, "an open list of words narrows nothing");
            Assert.AreEqual(FactsSource, fact.Suggests);
            Assert.IsTrue(fact.RefusedAsReference, "a refusal and a list of words are two answers, not one");
            Assert.IsNull(Field(record, TitleField).Suggests, "and nothing is offered where nothing was written");

            Assert.IsNull(facts.Suggests, "the list is not the thing a word is written into");
            Assert.AreEqual(FactsSource, facts.Item?.Suggests);
            NothingSaid(nameof(TextDto), FactField);
        }

        /// <summary>Words offered where no word is written: markup that does nothing looks exactly like a
        /// field nobody has marked up yet, so it is named rather than passed over.</summary>
        [TestMethod]
        public void WordsOfferedForSomethingThatIsNotText_AreSaidOutLoud()
        {
            Assert.IsNull(Field(_reflector.Record(typeof(AwkwardDto)), PriceField).Suggests);
            Said(nameof(AwkwardDto), PriceField, SaidNotSuggestable);
        }

        /// <summary>And words offered under a field narrowed to something else: a reference is drawn as the
        /// ids of its catalogs, and nothing ever reads the open list written beside them. Said out loud for
        /// the same reason — the author wrote a list nobody offers and has no way of telling.</summary>
        [TestMethod]
        public void WordsOfferedUnderANarrowedField_AreSaidOutLoud()
        {
            Assert.AreEqual(FieldKind.Reference, Field(_reflector.Record(typeof(AwkwardDto)), NamedField).Kind);
            Said(nameof(AwkwardDto), NamedField, SaidSuggestedAndNarrowed);
        }

        /// <summary>A field named and refused at once keeps the refusal, and says so in the model as well as
        /// in the report: the tool asks the field, not the walk that built it.</summary>
        [TestMethod]
        public void AFieldRefusedAndNamedAtOnce_ReadsAsRefused() =>
            Assert.IsTrue(Field(_reflector.Record(typeof(AwkwardDto)), KeyField).RefusedAsReference);

        /// <summary>A map the author keys freely says so by having no key schema; one keyed by an enum says
        /// which members it takes, because an inspector that did not know would accept any word.</summary>
        [TestMethod]
        public void AMap_DescribesItsKeysAsWellAsItsValues()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));

            FieldSchema free = Field(record, BaseParametersField);
            Assert.AreEqual(FieldKind.Dictionary, free.Kind);
            Assert.IsNull(free.Key);
            Assert.AreEqual(FieldKind.Number, free.Item?.Kind);

            FieldSchema keyed = Field(record, ParametersField);
            Assert.AreEqual(FieldKind.Enum, keyed.Key?.Kind);
            CollectionAssert.AreEqual(Enum.GetNames<TestParameter>(), keyed.Key!.EnumValues.ToArray());
            Assert.AreEqual(FieldKind.Integer, keyed.Item?.Kind);
        }

        /// <summary>A map written with words for keys and meaning the members of an enum says which enum,
        /// and the tool offers the members instead of taking any word.</summary>
        [TestMethod]
        public void KeysMarkedAsAnEnum_AreOfferedAsItsMembers()
        {
            FieldSchema map = Field(_reflector.Record(typeof(KeyedDto)), BaseParametersField);

            Assert.AreEqual(FieldKind.Enum, map.Key?.Kind);
            Assert.AreEqual(FieldSchema.Unnamed, map.Key!.JsonName);
            CollectionAssert.AreEqual(Enum.GetNames<TestParameter>(), map.Key.EnumValues.ToArray());
            Assert.AreEqual(FieldKind.Number, map.Item?.Kind);
            NothingSaid(BaseParametersField);
        }

        /// <summary>Keys that name records name them out of every catalog written on the field, the way the
        /// value of a reference does.</summary>
        [TestMethod]
        public void KeysNamingRecords_PointIntoEveryCatalogWrittenOnThem()
        {
            FieldSchema map = Field(_reflector.Record(typeof(KeyedDto)), ItemsField);

            Assert.AreEqual(FieldKind.Reference, map.Key?.Kind);
            CollectionAssert.AreEqual(new[] { EquipItemsCatalog, ResourcesCatalog }, Catalogs(map.Key!));
        }

        /// <summary>What the keys hold and what the values hold are two questions, answered by two pieces of
        /// markup on one field. Neither answer is read as the other, and neither is complained about.</summary>
        [TestMethod]
        public void TheKeysOfAMap_AreDescribedApartFromItsValues()
        {
            FieldSchema map = Field(_reflector.Record(typeof(KeyedDto)), PassivesField);

            Assert.AreEqual(FieldKind.Enum, map.Key?.Kind);
            Assert.AreEqual(FieldKind.Reference, map.Item?.Kind);
            NothingSaid(PassivesField);
        }

        /// <summary>Keys described where the type has settled them already, and where there are no keys at
        /// all: the schema is still built, and markup that did nothing is named.</summary>
        [TestMethod]
        public void KeyMarkupNothingCanUse_IsSaidOutLoud()
        {
            RecordSchema record = _reflector.Record(typeof(KeyedDto));

            Assert.AreEqual(FieldKind.Enum, Field(record, ParametersField).Key?.Kind, "the type is what the file is parsed into");
            Assert.AreEqual(FieldKind.String, Field(record, NameField).Kind);
            Said(nameof(KeyedDto), ParametersField, SaidAlreadyKeyed);
            Said(nameof(KeyedDto), NameField, SaidNotAMap);
        }

        /// <summary>Keys said to be an enum and to name records at once: one of the two is what the author
        /// is offered, and which one it is has to be said rather than found out from a file.</summary>
        [TestMethod]
        public void KeysNarrowedTwoWaysAtOnce_KeepTheEnum_AndAreSaidOutLoud()
        {
            FieldSchema map = Field(_reflector.Record(typeof(KeyedDto)), WeightsField);

            Assert.AreEqual(FieldKind.Enum, map.Key?.Kind);
            Said(nameof(KeyedDto), WeightsField, SaidKeyedTwice);
        }

        /// <summary>Every kind an editor draws differently, read from the type that produces it.</summary>
        [TestMethod]
        public void EveryKind_ComesFromTheClrTypeThatHoldsIt()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));

            Assert.AreEqual(FieldKind.String, Field(record, IdField).Kind);
            Assert.AreEqual(FieldKind.Integer, Field(record, LevelField).Kind);
            Assert.AreEqual(FieldKind.Number, Field(record, ScalingField).Kind);
            Assert.AreEqual(FieldKind.Boolean, Field(record, AggressiveField).Kind);
            Assert.AreEqual(FieldKind.Object, Field(record, AuthoredField).Kind);
            Assert.AreEqual(FieldKind.Array, Field(record, AbilitiesField).Kind);
            Assert.AreEqual(FieldKind.Dictionary, Field(record, BaseParametersField).Kind);
            Assert.AreEqual(FieldKind.Any, Field(record, ConditionsField).Kind, "a free structure is kept verbatim");
            Assert.IsNull(Field(record, IdField).Documentation, "xml docs are not in the assembly at runtime");
        }

        [TestMethod]
        public void ANestedObject_CarriesTheRecordOfItsOwnType()
        {
            RecordSchema authored = Field(_reflector.Record(typeof(NpcDto)), AuthoredField).Record!;

            Assert.AreEqual(nameof(AuthoredDto), authored.TypeName);
            CollectionAssert.AreEqual(new[] { AuthoredLevelField, NameField }, Names(authored));
        }

        /// <summary>Required is what the file has to state, and the type says it by what it allows to be
        /// absent: a reference not written "?" is a value the reader is owed, a value type says the same
        /// by having neither a "?" nor a value of its own.</summary>
        [TestMethod]
        public void WhatIsRequired_IsWhatTheTypeDoesNotAllowToBeAbsent()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));
            RecordSchema authored = _reflector.Record(typeof(AuthoredDto));

            Assert.IsTrue(Field(record, ScalingField).Required, "a number with nothing written for it");
            Assert.IsTrue(Field(record, IdField).Required, "a string nothing allows to be absent");
            Assert.IsTrue(Field(record, AbilitiesField).Required, "a list is a key like any other");
            Assert.IsTrue(Field(record, BaseParametersField).Required, "and so is a map");
            Assert.IsFalse(Field(record, LevelField).Required, "a number that starts at one");
            Assert.IsFalse(Field(record, AggressiveField).Required, "a flag that starts true");
            Assert.IsFalse(Field(record, AuthoredField).Required, "a section that may be absent");
            Assert.IsFalse(Field(record, ConditionsField).Required, "and a free structure that may be");
            Assert.IsFalse(Field(authored, AuthoredLevelField).Required, "a number written '?'");
            Assert.IsFalse(Field(authored, NameField).Required, "and text written '?'");
            Assert.IsTrue(Field(_reflector.Record(typeof(StrictDto)), IdField).Required, "and what the type demands");
        }

        /// <summary>The value a reference starts out holding is the parser's guard against null and not the
        /// author's leave to leave the key out: the same string with an initializer and without one makes
        /// the same demand of the file. A key the game reads a default for is a key the game DECIDED could
        /// be absent, and only a value type and a "?" say that.</summary>
        [TestMethod]
        public void AnInitializerOnAReference_DoesNotMakeTheKeyTheAuthorsToLeaveOut()
        {
            RecordSchema record = _reflector.Record(typeof(GuardedDto));

            Assert.IsTrue(Field(record, IdField).Required, "text that starts empty is text the file writes");
            Assert.IsTrue(Field(record, TagsField).Required, "a list that starts empty is a list the file writes");
            Assert.IsTrue(Field(record, AuthoredField).Required, "and a record that stands whole from the start");
            Assert.IsFalse(Field(record, NameField).Required);
            Assert.IsFalse(Field(record, AmountField).Required);
        }

        /// <summary>A type compiled where nullability is not stated answers neither way, and the key is
        /// read as one the file has to write. Said out loud: a record whose keys cannot be removed for a
        /// reason nobody can see is a contract the author has no way of reading back.</summary>
        [TestMethod]
        public void ATypeThatStatesNoNullability_IsReadAsRequired_AndSaidOutLoud()
        {
            Assert.IsTrue(Field(_reflector.Record(typeof(UnannotatedDto)), IdField).Required);
            Said(nameof(UnannotatedDto), IdField);
        }

        /// <summary>The default is what the game reads when the key is absent, which is the value a fresh
        /// instance holds. Shapes have none: an absent list is a list the editor builds, not a value.</summary>
        [TestMethod]
        public void ADefault_IsWhatAFreshInstanceHolds()
        {
            RecordSchema record = _reflector.Record(typeof(NpcDto));

            Assert.AreEqual(1, Field(record, LevelField).Default);
            Assert.AreEqual(true, Field(record, AggressiveField).Default);
            Assert.AreEqual(string.Empty, Field(record, IdField).Default);
            Assert.AreEqual(NoScaling, Field(record, ScalingField).Default);
            Assert.IsNull(Field(record, AbilitiesField).Default);
            Assert.IsNull(Field(record, AuthoredField).Default);
        }

        /// <summary>A name is what the file holds for an enum, so a name is what the default is written as.</summary>
        [TestMethod]
        public void ADefaultOfAnEnum_IsTheNameTheFileWrites() =>
            Assert.AreEqual(nameof(TestParameter.Health), Field(_reflector.Record(typeof(AwkwardDto)), NameField).Default);

        /// <summary>Nothing builds a positional record without arguments, so nothing can say what it starts
        /// with — and everything it declares has to be written down.</summary>
        [TestMethod]
        public void ATypeThatCannotBeBuiltWithoutArguments_StatesNoDefaults()
        {
            RecordSchema record = _reflector.Record(typeof(PositionalDto));

            Assert.IsNull(Field(record, IdField).Default);
            Assert.IsNull(Field(record, PriceField).Default);
            Assert.IsTrue(Field(record, IdField).Required);
            Assert.IsFalse(Field(record, AugmentsField).Required, "except what may be absent");
            Said(nameof(PositionalDto), SaidNoArguments);
        }

        /// <summary>Machinery stays in the file and out of the inspector; a range narrows the numbers a field
        /// takes, on the number itself however it is held.</summary>
        [TestMethod]
        public void MarkupThatNarrowsAField_IsCarriedOntoIt()
        {
            RecordSchema npc = _reflector.Record(typeof(NpcDto));
            RecordSchema tier = _reflector.Record(typeof(LootTierDto));

            Assert.IsTrue(Field(npc, VersionField).Hidden);
            Assert.IsFalse(Field(npc, IdField).Hidden);
            Assert.AreEqual(new NumericRange(MinTier, MaxTier), Field(tier, TierField).Range);
            Assert.AreEqual(new NumericRange(MinTier, MaxTier), Field(tier, WeightsField).Item?.Range, "a list of numbers is narrowed on its numbers");
        }

        /// <summary>The polymorphic thing in a loot table is the position, and a position sits two arrays
        /// below the table. Registered against the type, the shapes have to reach it there.</summary>
        [TestMethod]
        public void RegisteredShapes_ReachTheRecordWhereverItSits()
        {
            _reflector.Polymorphic(typeof(LootPositionDto), Positions());

            RecordSchema table = _reflector.Record(typeof(LootTableDto));
            RecordSchema position = Nested(Nested(table, TiersField), ItemsField);

            Assert.IsNotNull(position.Variants);
            CollectionAssert.AreEqual(
                new[] { IdField, AugmentsField },
                position.Variants.Variants.Select(variant => variant.DiscriminatorValue).ToArray());
            Assert.AreEqual(nameof(LootPositionByIdDto), position.Variants.Variants[0].Record.TypeName);
            Assert.IsNull(Nested(table, TiersField).Variants, "and only the record they were registered against");
        }

        [TestMethod]
        public void RegisteredShapes_HangOnTheRecordAskedForDirectly()
        {
            _reflector.Polymorphic(typeof(LootPositionDto), Positions());

            Assert.IsNotNull(_reflector.Record(typeof(LootPositionDto)).Variants);
        }

        /// <summary>Shapes are put on a record where it is BUILT, and a record is a value nobody keeps a
        /// list of copies of: registered afterwards, they reach nothing already handed out. The order is
        /// the descriptor's to get right, and getting it wrong is not something to find out from a file.</summary>
        [TestMethod]
        public void ShapesRegisteredAfterTheRecordsWereRead_ReachNothing_AndAreNamed()
        {
            CatalogSchemaBuilder early = new();
            CatalogSchemaBuilder late = new();

            CatalogSchema first = early.Build(new LootTablesDescriptor());
            CatalogSchema second = late.Build(new LateShapesDescriptor());

            Assert.AreNotEqual(first, second);
            Assert.IsNotNull(Positions(first).Variants);
            Assert.IsNull(Positions(second).Variants, "the record was built before the shapes were named");
            Assert.IsTrue(early.Reflection.IsClean, early.Reflection.ToString());
            StringAssert.Contains(late.Reflection.ToString(), nameof(LootPositionDto));
        }

        /// <summary>Shapes are registered against a type by name, and a misspelt name registers shapes
        /// nothing will ever wear. The schema comes out with one record where the file has several, and
        /// nothing about it looks wrong.</summary>
        [TestMethod]
        public void ShapesRegisteredForATypeTheCatalogNeverHolds_AreSaidOutLoud()
        {
            CatalogSchemaBuilder builder = new();

            builder.Build(new StrayShapesDescriptor());

            Assert.IsFalse(builder.Checks.IsClean);
            StringAssert.Contains(builder.Checks.ToString(), nameof(TreeNodeDto));
        }

        [TestMethod]
        public void ShapesRegisteredTwiceForOneType_AreSaidOutLoud()
        {
            _reflector.Polymorphic(typeof(LootPositionDto), Positions());
            _reflector.Polymorphic(typeof(LootPositionDto), Positions());

            Said(nameof(LootPositionDto), SaidTwice);
        }

        /// <summary>A record holding a record of its own type would be read for as long as the machine
        /// stands. The field keeps its kind so the editor still knows an object goes there.</summary>
        [TestMethod]
        public void ATypeThatLeadsBackToItself_IsReadOnce_AndSaidSo()
        {
            RecordSchema record = _reflector.Record(typeof(TreeNodeDto));

            Assert.AreEqual(FieldKind.Object, Field(record, ParentField).Kind);
            Assert.IsNull(Field(record, ParentField).Record);
            Assert.AreEqual(FieldKind.Object, Field(record, ChildrenField).Item?.Kind);
            Assert.IsNull(Field(record, ChildrenField).Item?.Record);
            Said(nameof(TreeNodeDto), ChildrenField);
        }

        /// <summary>Base first, then what the derived record adds: the file was written that way and a
        /// canonical save must not move a line the author did not touch.</summary>
        [TestMethod]
        public void WhatARecordInherits_StandsBeforeWhatItAdds() =>
            CollectionAssert.AreEqual(new[] { IdField, WeightsField, ScaleField }, Names(_reflector.Record(typeof(ScaledDto))));

        /// <summary>An abstract record and an interface describe a shape nothing in the file ever is. Read
        /// as far as they go, and said out loud, because the shapes were meant to be registered.</summary>
        [TestMethod]
        public void ATypeNothingIsEverExactly_IsSaidOutLoud()
        {
            _reflector.Record(typeof(ModifierBase));

            Said(nameof(ModifierBase));
        }

        [TestMethod]
        public void ATypeWhoseShapesAreRegistered_IsNotComplainedAbout()
        {
            _reflector.Polymorphic(typeof(ModifierBase), new VariantSet([Variant(ScaleField, _reflector.Record(typeof(ScaledDto)))], IdField));
            _reflector.Record(typeof(ModifierBase));

            Assert.IsTrue(_reflector.Report.IsClean, _reflector.Report.ToString());
        }

        /// <summary>Everything the walk could not read is named. A field quietly dropped looks exactly like
        /// a field the game never had.</summary>
        [TestMethod]
        public void WhatCannotBeRead_IsLeftOutAndNamed()
        {
            RecordSchema record = _reflector.Record(typeof(AwkwardDto));

            CollectionAssert.DoesNotContain(Names(record), SummaryField);
            Said(nameof(AwkwardDto), nameof(AwkwardDto.Summary));
            Said(nameof(AwkwardDto), nameof(AwkwardDto.Fragile), SaidThrew);
            Assert.IsNull(Field(record, FragileField).Default, "a value that cannot be read has no default");
        }

        /// <summary>A property nothing can be written to is worked out from the others and stands in no
        /// file: required, it would demand a key no author can write. A get-only LIST is another matter —
        /// the deserializer fills it where it stands, so the file does write it.</summary>
        [TestMethod]
        public void AValueWorkedOutFromTheOthers_IsNotInTheSchema()
        {
            RecordSchema record = _reflector.Record(typeof(AwkwardDto));

            CollectionAssert.DoesNotContain(Names(record), NamesADropField);
            CollectionAssert.DoesNotContain(Names(record), BrokenField);
            CollectionAssert.Contains(Names(record), TagsField);
            Assert.AreEqual(FieldKind.Array, Field(record, TagsField).Kind);
            Said(nameof(AwkwardDto), nameof(AwkwardDto.NamesADrop), SaidComputed);
        }

        /// <summary>The metadata puts every field of a type before every property of it, so a schema holding
        /// both would state an order no file was ever written in.</summary>
        [TestMethod]
        public void APublicField_IsNotInTheSchema_AndIsNamed()
        {
            RecordSchema record = _reflector.Record(typeof(FieldedDto));

            CollectionAssert.AreEqual(new[] { IdField }, Names(record));
            Said(nameof(FieldedDto), nameof(FieldedDto.Count));
        }

        /// <summary>A converter writes whatever it likes: the game reads a loot position as "an id OR a
        /// group" and a range as "a number OR a pair", neither of which is the shape of the type. The
        /// reflector cannot know what it writes, and a shape stated with confidence it does not have is
        /// worse than one the report has flagged.</summary>
        [TestMethod]
        public void AFieldReadThroughAConverter_IsFlagged()
        {
            _reflector.Record(typeof(ConvertedDto));

            Said(nameof(ConvertedDto), ItemsField, nameof(RangeConverter));
            Said(nameof(ConvertedDto), ValueField, nameof(RangeConverter), SaidReadThrough);
        }

        /// <summary>An enum written as its name is the one converter that changes nothing: a name is what
        /// the schema says the file holds already.</summary>
        [TestMethod]
        public void AnEnumWrittenAsItsName_IsNotFlagged()
        {
            _reflector.Record(typeof(ConvertedDto));

            NothingSaid(RarityField);
        }

        /// <summary>Two properties of one type are two readings of it. A schema that handed the second one
        /// nothing — a cache keyed by type, a walk that remembers too much — would lose half a file.</summary>
        [TestMethod]
        public void OneTypeStandingInTwoPlaces_IsReadForBothOfThem()
        {
            RecordSchema record = _reflector.Record(typeof(PairDto));

            Assert.IsNotNull(Field(record, PrimaryField).Record);
            Assert.IsNotNull(Field(record, FallbackField).Record);
            Assert.AreEqual(nameof(AuthoredDto), Field(record, FallbackField).Record!.TypeName);
            Assert.IsTrue(_reflector.Report.IsClean, _reflector.Report.ToString());
        }

        /// <summary>Markup on a map describes what the map HOLDS. An author who wrote it meaning the keys
        /// gets a word about it, instead of a complaint that the numbers in it are not text.</summary>
        [TestMethod]
        public void MarkupOnAMap_IsReadAsDescribingItsValues()
        {
            FieldSchema map = Field(_reflector.Record(typeof(MappedDto)), BaseParametersField);

            Assert.IsNull(map.Key);
            Said(nameof(MappedDto), BaseParametersField, SaidMap);
        }

        [TestMethod]
        public void ASequenceThatDoesNotSayWhatItHolds_IsKeptVerbatim()
        {
            RecordSchema record = _reflector.Record(typeof(AwkwardDto));

            Assert.AreEqual(FieldKind.Any, Field(record, LooseField).Kind);
            Assert.AreEqual(FieldKind.Any, Field(record, AnythingField).Kind);
            Said(nameof(AwkwardDto), LooseField);
        }

        /// <summary>Json keys by words. A map keyed by anything else is left free rather than described as
        /// something the file cannot hold.</summary>
        [TestMethod]
        public void AMapKeyedByWhatJsonCannotWrite_IsLeftFree_AndNamed()
        {
            FieldSchema map = Field(_reflector.Record(typeof(AwkwardDto)), WeightsField);

            Assert.AreEqual(FieldKind.Dictionary, map.Kind);
            Assert.IsNull(map.Key);
            Said(nameof(AwkwardDto), WeightsField);
        }

        /// <summary>Markup on a value that cannot use it does nothing, and a field that is marked and reads
        /// as unmarked is the one thing the markup exists to prevent.</summary>
        [TestMethod]
        public void MarkupAValueCannotUse_IsSaidOutLoud()
        {
            RecordSchema record = _reflector.Record(typeof(AwkwardDto));

            Assert.AreEqual(FieldKind.Integer, Field(record, PriceField).Kind);
            Assert.AreEqual(0, Field(record, PriceField).RefTargets.Count);
            Assert.IsNull(Field(record, IdField).Range);
            Said(nameof(AwkwardDto), PriceField, SaidNotText);
            Said(nameof(AwkwardDto), IdField, SaidNotANumber);
        }

        /// <summary>A field marked a reference and marked not one at once: the refusal is taken, because a
        /// validator told to check ids it should not would report every value in the file as broken.</summary>
        [TestMethod]
        public void AFieldRefusedAndNamedAtOnce_KeepsTheRefusal()
        {
            FieldSchema field = Field(_reflector.Record(typeof(AwkwardDto)), KeyField);

            Assert.AreEqual(FieldKind.String, field.Kind);
            Assert.AreEqual(0, field.RefTargets.Count);
            Said(nameof(AwkwardDto), KeyField);
        }

        /// <summary>Markup the tool knows by name and that answers to nothing behind it — a name carried
        /// by nothing, and a name carrying something of another kind entirely. The field is read as
        /// unmarked and the break is named: markup that quietly does nothing looks exactly like a field
        /// nobody has marked up yet.</summary>
        [TestMethod]
        public void MarkupNotAnsweringToTheNamesItIsReadBy_IsSaidOutLoud()
        {
            RecordSchema record = _reflector.Record(typeof(BrokenMarkupDto));

            Assert.IsNull(Field(record, TierField).Range);
            Said(nameof(BrokenMarkupDto), TierField, MarkupNames.Min);
            Said(nameof(BrokenMarkupDto), ModifiersField, MarkupNames.Field);
        }

        [TestMethod]
        public void TwoMembersUnderOneJsonName_LeaveOneFieldAndOneWord()
        {
            RecordSchema record = _reflector.Record(typeof(TwiceNamedDto));

            Assert.AreEqual(1, record.Fields.Count);
            Said(nameof(TwiceNamedDto), KeyField);
        }

        /// <summary>A field naming what tells the shapes apart, and shapes told apart by something else, is
        /// two decisions about one file. Only one of them can be the one the editor draws.</summary>
        [TestMethod]
        public void AFieldNamingAnotherDiscriminatorThanTheShapesUse_IsSaidOutLoud()
        {
            _reflector.Polymorphic(typeof(ModifierBase), new VariantSet([Variant(ScaleField, _reflector.Record(typeof(ScaledDto)))], IdField));
            _reflector.Record(typeof(SectionDto));

            Said(nameof(SectionDto), ModifiersField, KeyField);
        }

        /// <summary>A type with nothing filled in has no fields to read, and a schema of a record that says
        /// nothing is worse than a word about it.</summary>
        [TestMethod]
        public void AnOpenGenericType_IsReadAsNothing_AndNamed()
        {
            RecordSchema record = _reflector.Record(typeof(HolderDto<>));

            Assert.AreEqual(0, record.Fields.Count);
            Said(nameof(HolderDto<object>));
        }

        [TestMethod]
        public void ATypeThatThrowsWhenBuilt_LosesItsDefaultsAndNothingElse()
        {
            RecordSchema record = _reflector.Record(typeof(ThrowingDto));

            Assert.AreEqual(1, record.Fields.Count);
            Assert.IsNull(Field(record, IdField).Default);
            Said(nameof(ThrowingDto));
        }

        [TestMethod]
        public void APlainRecord_LeavesNothingToSay()
        {
            _reflector.Record(typeof(NpcDto));

            Assert.IsTrue(_reflector.Report.IsClean, _reflector.Report.ToString());
        }

        [TestMethod]
        public void TheReflector_RefusesNothingWhereSomethingIsRequired()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _reflector.Record(null!));
            Assert.ThrowsException<ArgumentNullException>(() => _reflector.Polymorphic(null!, Positions()));
            Assert.ThrowsException<ArgumentNullException>(() => _reflector.Polymorphic(typeof(LootPositionDto), null!));
        }

        /// <summary>The builder hands back what the descriptor said, and says what the descriptor and the
        /// types it named do not agree about.</summary>
        [TestMethod]
        public void ADescribedCatalog_IsBuiltAsItWasDescribed()
        {
            CatalogSchemaBuilder builder = new();

            CatalogSchema schema = builder.Build(new LootTablesDescriptor());

            Assert.AreEqual(RootShape.SectionsOfArrays, schema.Shape);
            Assert.AreEqual(KeyField, schema.Sections[0].Record.IdField);
            Assert.IsTrue(builder.Checks.IsClean, builder.Checks.ToString());
            Assert.IsTrue(builder.Reflection.IsClean, builder.Reflection.ToString());
        }

        /// <summary>A record whose id is written under a key it does not have leaves the tool with no way to
        /// name a record, and the model cannot see it: the id is named by the descriptor, the fields by the
        /// type.</summary>
        [TestMethod]
        public void AnIdWrittenUnderAKeyTheRecordDoesNotHave_IsSaidOutLoud()
        {
            CatalogSchemaBuilder builder = new();

            builder.Build(new MisnamedDescriptor());

            Assert.IsFalse(builder.Checks.IsClean);
            StringAssert.Contains(builder.Checks.ToString(), NameField);
        }

        /// <summary>Nine files, one per slot — and a slot no record writes leaves every record with no file
        /// to go to.</summary>
        [TestMethod]
        public void ACatalogSplitByAFieldNoRecordWrites_IsSaidOutLoud()
        {
            CatalogSchemaBuilder builder = new();

            builder.Build(new SplitDescriptor());

            Assert.IsFalse(builder.Checks.IsClean);
            StringAssert.Contains(builder.Checks.ToString(), ScaleField);
        }

        [TestMethod]
        public void TheBuilder_RefusesNothingWhereSomethingIsRequired()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CatalogSchemaBuilder().Build(null!));
            Assert.ThrowsException<ArgumentNullException>(() => new CatalogSchemaBuilder(null!));
        }

        internal static VariantSchema Variant(string value, RecordSchema record) =>
            new() { DiscriminatorValue = value, Record = record };

        internal static SectionSchema Section(string key, RecordSchema record) => new() { Key = key, Record = record };

        internal static FieldSchema Field(RecordSchema record, string jsonName) =>
            record.Fields.FirstOrDefault(field => field.JsonName == jsonName)
            ?? throw new AssertFailedException($"{record.TypeName} has no field '{jsonName}'.");

        internal static RecordSchema Nested(RecordSchema record, string jsonName) =>
            Field(record, jsonName).Item?.Record
            ?? throw new AssertFailedException($"{record.TypeName}.{jsonName} holds no records.");

        private static string[] Names(RecordSchema record) => [.. record.Fields.Select(field => field.JsonName)];

        private VariantSet Positions() => new(
        [
            Variant(IdField, _reflector.Record(typeof(LootPositionByIdDto))),
            Variant(AugmentsField, _reflector.Record(typeof(LootPositionByAugmentsDto)))
        ]);

        /// <summary>The position record of a built loot catalog, two arrays below its first section.</summary>
        private static RecordSchema Positions(CatalogSchema schema) =>
            Nested(Nested(schema.Sections[0].Record, TiersField), ItemsField);

        /// <summary>The catalogs a reference points into, whichever sections of them it names.</summary>
        private static string[] Catalogs(FieldSchema field) => [.. field.RefTargets.Select(target => target.Catalog)];

        /// <summary>One note naming all of these, in whatever words it uses.</summary>
        private void Said(params string[] parts)
        {
            Assert.IsTrue(Told(parts), $"nothing was said about {string.Join(", ", parts)}: {_reflector.Report}");
        }

        /// <summary>No note about it at all — which is the whole of what a rule that lets something through
        /// can be checked by.</summary>
        private void NothingSaid(params string[] parts)
        {
            Assert.IsFalse(Told(parts), $"something was said about {string.Join(", ", parts)}: {_reflector.Report}");
        }

        private bool Told(string[] parts) =>
            _reflector.Report.Notes.Any(note => parts.All(part => note.Contains(part, StringComparison.Ordinal)));

        internal enum TestFraction
        {
            Human,
            Undead,
            Animal
        }

        internal enum TestParameter
        {
            Health,
            Damage
        }

        internal enum TestRarity
        {
            Legendary,
            Common
        }

        internal sealed record NpcDto
        {
            public string Id { get; init; } = string.Empty;

            public string NPCId { get; init; } = string.Empty;

            [EnumOf(typeof(TestFraction))] public string Fraction { get; init; } = string.Empty;

            [CatalogRef(AbilitiesCatalog)] public List<string> Abilities { get; init; } = [];

            public Dictionary<string, float> BaseParameters { get; init; } = [];

            public Dictionary<TestParameter, int> Parameters { get; init; } = [];

            public AuthoredDto? Authored { get; init; }

            public JToken? AcceptConditions { get; init; }

            [JsonProperty(LevelField)] public int LevelMin { get; init; } = 1;

            public float LevelScaling { get; init; }

            public bool Aggressive { get; init; } = true;

            [Hidden] public int Version { get; init; }

            [JsonIgnore] public string Note { get; init; } = string.Empty;
        }

        internal sealed record AuthoredDto
        {
            public int? Level { get; init; } = AuthoredLevel;

            public string? Name { get; init; }
        }

        internal sealed record TextDto
        {
            [CatalogRef(EquipItemsCatalog)]
            [CatalogRef(ResourcesCatalog)]
            public string Id { get; init; } = string.Empty;

            [CatalogRef(NpcBuffsCatalog, AllowEmpty = true)] public string NpcBuffId { get; init; } = string.Empty;

            [CatalogRef(ResourcesCatalog, Section = CategoriesKey)] public string CategoryId { get; init; } = string.Empty;

            [NotARef] public string Key { get; init; } = string.Empty;

            [NotARef] public List<string> Tags { get; init; } = [];

            /// <summary>Answered from an open list of words: not a reference, not an enum, and a word the
            /// list does not know is written all the same.</summary>
            [NotARef][Suggests(FactsSource)] public string Fact { get; init; } = string.Empty;

            [Suggests(FactsSource)] public List<string> Facts { get; init; } = [];

            [LocalizedKey] public string Title { get; init; } = string.Empty;

            [LocalizedKey(DescriptionSuffix)] public string Summary { get; init; } = string.Empty;
        }

        /// <summary>The maps of an NPC record, whose keys are as much a decision as their values: parameters
        /// named by an enum, properties named by the factory that reads them, and the markup that misses.</summary>
        internal sealed record KeyedDto
        {
            [DictionaryKey(typeof(TestParameter))] public Dictionary<string, float> BaseParameters { get; init; } = [];

            [DictionaryKey(EquipItemsCatalog)]
            [DictionaryKey(ResourcesCatalog)]
            public Dictionary<string, int> Items { get; init; } = [];

            [DictionaryKey(typeof(TestParameter))]
            [CatalogRef(AbilitiesCatalog)]
            public Dictionary<string, string> Passives { get; init; } = [];

            [DictionaryKey(typeof(TestParameter))]
            [DictionaryKey(EquipItemsCatalog)]
            public Dictionary<string, float> Weights { get; init; } = [];

            /// <summary>Keys said to be an enum where the type has already said so.</summary>
            [DictionaryKey(typeof(TestParameter))] public Dictionary<TestParameter, int> Parameters { get; init; } = [];

            /// <summary>Keys described where there are no keys.</summary>
            [DictionaryKey(typeof(TestParameter))] public string Name { get; init; } = string.Empty;
        }

        internal sealed record StrictDto
        {
            public required string Id { get; init; }
        }

        /// <summary>The shape the game's own records take: text, a list and a record that start out empty
        /// rather than null, beside the two ways a key really is the author's to leave out.</summary>
        internal sealed record GuardedDto
        {
            public string Id { get; init; } = string.Empty;

            public List<string> Tags { get; init; } = [];

            public AuthoredDto Authored { get; init; } = new();

            public string? Name { get; init; }

            public int Amount { get; init; } = 1;
        }

        internal sealed record LootTableDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            public List<LootTierDto> Tiers { get; init; } = [];
        }

        internal sealed record LootTierDto
        {
            [Range(MinTier, MaxTier)] public int Tier { get; init; }

            [Range(MinTier, MaxTier)] public List<int> Weights { get; init; } = [];

            public List<LootPositionDto> Items { get; init; } = [];
        }

        internal sealed record LootPositionDto
        {
            public float Price { get; init; }
        }

        internal sealed record LootPositionByIdDto
        {
            [CatalogRef(EquipItemsCatalog)] public string Id { get; init; } = string.Empty;

            public float Price { get; init; }
        }

        internal sealed record LootPositionByAugmentsDto
        {
            public AugmentGroupDto? Augments { get; init; }

            public float Price { get; init; }
        }

        internal sealed record AugmentGroupDto
        {
            [Range(MinTier, MaxTier)] public int Tier { get; init; }

            [EnumOf(typeof(TestRarity))] public string Rarity { get; init; } = string.Empty;
        }

        /// <summary>A position named by a constructor: nothing builds it without arguments.</summary>
        internal sealed record PositionalDto(string Id, float Price, AugmentGroupDto? Augments = null);

        internal abstract record ModifierBase
        {
            public string Id { get; init; } = string.Empty;

            public List<float> Weights { get; init; } = [];
        }

        internal sealed record ScaledDto : ModifierBase
        {
            public float Scale { get; init; }
        }

        internal sealed record SectionDto
        {
            [NotARef] public string Key { get; init; } = string.Empty;

            [Discriminator(KeyField)] public List<ModifierBase> Modifiers { get; init; } = [];
        }

        /// <summary>Markup wearing the names the tool reads by and answering to neither of them.</summary>
        internal sealed record BrokenMarkupDto
        {
            [Broken.Range(TierFloor, TierCeiling)] public int Tier { get; init; }

            [Broken.Discriminator] public List<ModifierBase> Modifiers { get; init; } = [];
        }

        internal sealed record TreeNodeDto
        {
            public string Id { get; init; } = string.Empty;

            public TreeNodeDto? Parent { get; init; }

            public List<TreeNodeDto> Children { get; init; } = [];
        }

        internal sealed record TwiceNamedDto
        {
            public string Key { get; init; } = string.Empty;

            [JsonProperty(KeyField)] public string Other { get; init; } = string.Empty;
        }

        internal sealed record HolderDto<T>
        {
            public T? Held { get; init; }
        }

        internal sealed record ThrowingDto
        {
            public ThrowingDto() => throw new InvalidOperationException(nameof(ThrowingDto));

            public string Id { get; init; } = string.Empty;
        }

        /// <summary>Every shape the walk cannot take at face value, in one record.</summary>
        internal sealed record AwkwardDto
        {
            /// <summary>A list nothing can be assigned to is still filled where it stands, so the file does
            /// write it — the one get-only shape that belongs in a schema.</summary>
            public List<string> Tags { get; } = [];

            /// <summary>Worked out from the fields around it, the way a loot position answers whether it
            /// names a drop. Nothing in the file corresponds to it.</summary>
            public bool NamesADrop => Tags.Count > 0;

            public string Broken => throw new InvalidOperationException(nameof(Broken));

            /// <summary>Written to, so it belongs in the schema; unreadable, so it has no default.</summary>
            public string Fragile
            {
                get => throw new InvalidOperationException(nameof(Fragile));
                init => _ = value;
            }

            [Range(MinTier, MaxTier)] public string Id { get; init; } = string.Empty;

            [CatalogRef(EquipItemsCatalog)]
            [NotARef]
            public string Key { get; init; } = string.Empty;

            /// <summary>Named as a reference and answered from a list of words, and it is a number: both
            /// pieces of markup have nothing to apply themselves to.</summary>
            [CatalogRef(EquipItemsCatalog)][Suggests(FactsSource)] public int Price { get; init; }

            /// <summary>Text pointed into a catalog and answered from a list of words at once: the field is
            /// drawn as the reference it was narrowed to, and the words are offered by nobody.</summary>
            [CatalogRef(EquipItemsCatalog)][Suggests(FactsSource)] public string Named { get; init; } = string.Empty;

            /// <summary>Narrowed to a section with no name, which no file writes anything under.</summary>
            [CatalogRef(AbilitiesCatalog, Section = " ")] public string Sectionless { get; init; } = string.Empty;

            /// <summary>Pointed into a catalog with no name, which is no catalog. The game's own markup
            /// refuses this where it is written; markup from anywhere else reaches the walk.</summary>
            [CatalogRef(" ")] public string Catalogless { get; init; } = string.Empty;

            public TestParameter Name { get; init; } = TestParameter.Health;

            public Dictionary<int, float> Weights { get; init; } = [];

            public ArrayList Loose { get; init; } = [];

            public object? Anything { get; init; }

            public string this[int index] => string.Empty;

            public string Summary
            {
                init => _ = value;
            }
        }

        /// <summary>Two properties of one type, side by side: the walk reads the type twice and neither
        /// reading is the other's leftovers.</summary>
        internal sealed record PairDto
        {
            public AuthoredDto? Primary { get; init; }

            public AuthoredDto? Fallback { get; init; }
        }

#nullable disable

        /// <summary>A type compiled where nullability is not stated: its text answers neither "may be
        /// absent" nor "may not", which is the one case the walk has to decide for itself.</summary>
        internal sealed record UnannotatedDto
        {
            public string Id { get; init; } = string.Empty;
        }

#nullable restore

        internal sealed record FieldedDto
        {
            public string Id { get; init; } = string.Empty;

            public int Count = 1;
        }

        /// <summary>What the game reads through converters: a list whose entries take a shape of their own,
        /// an enum written as its name, and a type that is a number in the file as often as a pair.</summary>
        internal sealed record ConvertedDto
        {
            [JsonConverter(typeof(RangeConverter))] public List<LootPositionDto> Items { get; init; } = [];

            [JsonConverter(typeof(StringEnumConverter))] public TestRarity Rarity { get; init; }

            public RangeDto Value { get; init; } = new();
        }

        [JsonConverter(typeof(RangeConverter))]
        internal sealed record RangeDto
        {
            public float Min { get; init; }

            public float Max { get; init; }
        }

        internal sealed record MappedDto
        {
            [EnumOf(typeof(TestParameter))] public Dictionary<string, float> BaseParameters { get; init; } = [];
        }

        /// <summary>Stands for the converters the game reads its ranges and its loot positions through. What
        /// it does is nobody's business here — only that it is written down.</summary>
        private sealed class RangeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => true;

            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => null;

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
            }
        }

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

                RecordSchema table = builder.Record(typeof(LootTableDto)) with { IdField = KeyField };

                return new CatalogSchema(
                    RootShape.SectionsOfArrays,
                    [Section(GeneralKey, table), Section(IndividualKey, table)],
                    [],
                    new SingleFilePlacement { FileName = LootTablesCatalog });
            }
        }

        /// <summary>The same catalog written the wrong way round: the table is read before the shapes of its
        /// positions are named.</summary>
        private sealed class LateShapesDescriptor : ICatalogDescriptor
        {
            public string Catalog => LootTablesCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                RecordSchema table = builder.Record(typeof(LootTableDto)) with { IdField = KeyField };

                builder.Polymorphic(typeof(LootPositionDto), new VariantSet(
                [
                    Variant(IdField, builder.Record(typeof(LootPositionByIdDto))),
                    Variant(AugmentsField, builder.Record(typeof(LootPositionByAugmentsDto)))
                ]));

                return new CatalogSchema(
                    RootShape.SectionsOfArrays,
                    [Section(GeneralKey, table), Section(IndividualKey, table)],
                    [],
                    new SingleFilePlacement { FileName = LootTablesCatalog });
            }
        }

        /// <summary>Shapes named for a type this catalog holds nothing of — a name misspelt in a typeof.</summary>
        private sealed class StrayShapesDescriptor : ICatalogDescriptor
        {
            public string Catalog => NpcCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                builder.Polymorphic(
                    typeof(TreeNodeDto),
                    new VariantSet([Variant(AuthoredLevelField, builder.Record(typeof(AuthoredDto)))]));

                return new CatalogSchema(
                    RootShape.ArrayUnderKey,
                    [Section(NpcsKey, builder.Record(typeof(NpcDto)))],
                    [],
                    new FreeFilePlacement());
            }
        }

        private sealed class MisnamedDescriptor : ICatalogDescriptor
        {
            public string Catalog => NpcCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(NpcsKey, builder.Record(typeof(NpcDto)) with { IdField = NameField })],
                [],
                new FreeFilePlacement());
        }

        private sealed class SplitDescriptor : ICatalogDescriptor
        {
            public string Catalog => EquipItemsCatalog;

            public CatalogSchema Describe(ISchemaBuilder builder) => new(
                RootShape.ArrayUnderKey,
                [Section(ItemsField, builder.Record(typeof(NpcDto)))],
                [],
                new FieldFilePlacement { FieldName = ScaleField });
        }
    }
}
