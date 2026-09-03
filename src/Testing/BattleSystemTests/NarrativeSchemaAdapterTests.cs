namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;
    using LastBreath.Descriptors;
    using Moq;
    using Tooling.Schema.Model;

    /// <summary>
    /// Holds the adapter that hands the narrative vocabulary to an authoring tool against the vocabulary
    /// itself: the game states what each condition and action is written with, the adapter says how a tool
    /// sees it, and everything the game stated has to survive the crossing. A parameter kind the adapter
    /// has no arm for is caught here rather than at the editor's first entry carrying it.
    /// </summary>
    [TestClass]
    public class NarrativeSchemaAdapterTests
    {
        private const string SampleKey = "key";
        private const string SampleMember = "Member";
        private const string SampleCatalog = "Catalog";
        private const string SampleSection = "section";

        private List<INarrativeConditionFactory> _conditions = null!;
        private List<INarrativeActionFactory> _actions = null!;

        [TestInitialize]
        public void Setup()
        {
            _conditions = NarrativeTestFactories.Conditions(
                Mock.Of<IInventory>(),
                Mock.Of<IWorldFactsService>(),
                Mock.Of<IFactionRelationService>(),
                Mock.Of<IPersonalReputationService>(),
                Mock.Of<IPlayerAccessor>(),
                Mock.Of<IInfluenceMastery>(),
                Mock.Of<IWorldClock>(),
                Mock.Of<IQuestLogService>,
                Mock.Of<IQuestProvider>);

            _actions = NarrativeTestFactories.Actions(
                Mock.Of<IWorldFactsService>(),
                Mock.Of<IInventory>(),
                Mock.Of<IItemMinter>(),
                Mock.Of<IGameEventBus>(),
                Mock.Of<IPlayerAccessor>(),
                Mock.Of<IFactionRelationService>(),
                Mock.Of<IInfluenceMastery>(),
                Mock.Of<IMartialArtMastery>(),
                Mock.Of<IGameMessageBus>(),
                Mock.Of<INpcProvider>(),
                Mock.Of<INpcModifierProvider>(),
                Mock.Of<INpcWorldSpawner>(),
                Mock.Of<INpcPopulationService>(),
                Mock.Of<ISpawnPointRegistry>(),
                Mock.Of<IQuestLogService>);
        }

        [TestMethod]
        public void EveryConditionSpecification_CrossesWhole()
        {
            foreach (var factory in _conditions)
                AssertConverted(factory.Parameters);
        }

        [TestMethod]
        public void EveryActionSpecification_CrossesWhole()
        {
            foreach (var factory in _actions)
                AssertConverted(factory.Parameters);
        }

        /// <summary>What the editor is handed: one schema per registered factory, in the order the
        /// vocabulary was registered in.</summary>
        [TestMethod]
        public void TheVocabulary_IsHandedOverWhole()
        {
            CollectionAssert.AreEqual(
                _conditions.Select(factory => factory.Type).ToList(),
                NarrativeSchemas.Conditions(_conditions).Select(schema => schema.TypeName).ToList(),
                "the conditions handed to the editor are not the conditions the vocabulary registered");

            CollectionAssert.AreEqual(
                _actions.Select(factory => factory.Type).ToList(),
                NarrativeSchemas.Actions(_actions).Select(schema => schema.TypeName).ToList(),
                "the actions handed to the editor are not the actions the vocabulary registered");
        }

        /// <summary>A kind added to the vocabulary and forgotten in the adapter would reach the editor as a
        /// throw on the first entry carrying it.</summary>
        [TestMethod]
        public void EveryParameterKind_IsMapped()
        {
            foreach (var kind in Enum.GetValues<NarrativeParameterKind>())
            {
                var parameter = new NarrativeParameterSpec
                {
                    JsonName = SampleKey,
                    Kind = kind,
                    Choices = [SampleMember],
                    Targets = [NarrativeReferenceTarget.Whole(SampleCatalog)]
                };

                try
                {
                    Assert.IsNotNull(NarrativeSchemas.ToSchema(parameter));
                }
                catch (ArgumentOutOfRangeException)
                {
                    Assert.Fail($"'{kind}' is a narrative parameter kind the adapter has no schema for");
                }
            }
        }

        /// <summary>A target narrowed to one section of a catalog crosses narrowed, whether it stands alone
        /// or in a list. A section lost on the way would offer the author every id of the catalog, including
        /// the ones the game never resolves — and nothing would call the file broken for picking one.</summary>
        [TestMethod]
        public void ATargetNarrowedToASection_CrossesNarrowed()
        {
            var parameter = new NarrativeParameterSpec
            {
                JsonName = SampleKey,
                Kind = NarrativeParameterKind.Reference,
                Required = true,
                Targets = [NarrativeReferenceTarget.Whole(SampleCatalog), new NarrativeReferenceTarget(SampleCatalog, SampleSection)]
            };

            List<ReferenceTarget> expected = [ReferenceTarget.Whole(SampleCatalog), new(SampleCatalog, SampleSection)];

            CollectionAssert.AreEqual(expected, NarrativeSchemas.ToSchema(parameter).RefTargets.ToList(),
                "a reference crossed pointing somewhere other than where the vocabulary sent it");
            CollectionAssert.AreEqual(expected,
                NarrativeSchemas.ToSchema(parameter with { Kind = NarrativeParameterKind.References }).Item!.RefTargets.ToList(),
                "a list of references crossed pointing somewhere other than where the vocabulary sent it");
        }

        private static void AssertConverted(NarrativeRecordSpec spec)
        {
            var schema = NarrativeSchemas.ToSchema(spec);

            Assert.AreEqual(spec.TypeName, schema.TypeName, "the record crossed under another type");
            CollectionAssert.AreEqual(
                spec.Parameters.Select(parameter => parameter.JsonName).ToList(),
                schema.Fields.Select(field => field.JsonName).ToList(),
                $"'{spec.TypeName}' crossed with other keys than it declares");

            for (int index = 0; index < spec.Parameters.Count; index++)
                AssertConverted(spec.TypeName, spec.Parameters[index], schema.Fields[index]);
        }

        private static void AssertConverted(string type, NarrativeParameterSpec parameter, FieldSchema field)
        {
            string address = $"{type}.{parameter.JsonName}";

            Assert.AreEqual(parameter.Required, field.Required, $"'{address}' crossed with another answer on whether it must be written");
            Assert.AreEqual(parameter.Default, field.Default, $"'{address}' crossed with another fallback");
            Assert.AreEqual(parameter.RefusedAsReference, field.RefusedAsReference, $"'{address}' crossed with another answer on being a reference");
            Assert.AreEqual(parameter.Documentation, field.Documentation, $"'{address}' crossed without what it means");

            switch (parameter.Kind)
            {
                case NarrativeParameterKind.Choice:
                    Assert.AreEqual(FieldKind.Enum, field.Kind, $"'{address}' offers members and did not cross as a choice");
                    CollectionAssert.AreEqual(parameter.Choices.ToList(), field.EnumValues.ToList(), $"'{address}' crossed with other members");
                    break;

                case NarrativeParameterKind.Reference:
                    Assert.AreEqual(FieldKind.Reference, field.Kind, $"'{address}' names catalogs and did not cross as a reference");
                    CollectionAssert.AreEqual(Targets(parameter), field.RefTargets.ToList(), $"'{address}' crossed with other catalogs");
                    break;

                case NarrativeParameterKind.References:
                    Assert.AreEqual(FieldKind.Array, field.Kind, $"'{address}' holds a list and did not cross as one");
                    Assert.AreEqual(FieldKind.Reference, field.Item?.Kind, $"'{address}' crossed as a list of something other than references");
                    CollectionAssert.AreEqual(Targets(parameter), field.Item!.RefTargets.ToList(), $"'{address}' crossed with other catalogs");
                    break;

                case NarrativeParameterKind.NestedCondition:
                    Assert.AreEqual(FieldKind.Object, field.Kind, $"'{address}' holds a condition and did not cross as a record");
                    Assert.AreEqual(NarrativeParameterSchema.NestedConditionRecord, field.Record?.TypeName,
                        $"'{address}' crossed as a record the editor cannot resolve against the vocabulary");
                    break;

                case NarrativeParameterKind.NestedConditions:
                    Assert.AreEqual(FieldKind.Array, field.Kind, $"'{address}' holds a list and did not cross as one");
                    Assert.AreEqual(NarrativeParameterSchema.NestedConditionRecord, field.Item?.Record?.TypeName,
                        $"'{address}' crossed as a list of something other than condition entries");
                    break;

                default:
                    Assert.AreEqual(Leaf(parameter.Kind), field.Kind, $"'{address}' crossed as another kind of value");
                    break;
            }
        }

        /// <summary>Where the parameter points, as the adapter states them: the same catalog, and the same
        /// section wherever the vocabulary narrowed one.</summary>
        private static List<ReferenceTarget> Targets(NarrativeParameterSpec parameter) =>
            [.. parameter.Targets.Select(target => new ReferenceTarget(target.Catalog, target.Section))];

        /// <summary>The kinds carrying nothing but a value of their own.</summary>
        private static FieldKind Leaf(NarrativeParameterKind kind) => kind switch
        {
            NarrativeParameterKind.Text => FieldKind.String,
            NarrativeParameterKind.Integer => FieldKind.Integer,
            NarrativeParameterKind.Number => FieldKind.Number,
            NarrativeParameterKind.Boolean => FieldKind.Boolean,
            _ => throw new AssertFailedException($"'{kind}' is not a plain value and needs a case of its own above"),
        };
    }
}
