namespace LastBreathTest.Narrative
{
    using System.Reflection;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
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

    /// <summary>
    /// Holds the vocabulary an authoring tool reads without a running game against the factories the game
    /// actually registers: the registry has to name the very specification each factory hands out, and no
    /// factory living in Core may be left out of it. A word missing from the registry is an entry the
    /// editor never offers; a word the registry states differently is an entry the parser refuses.
    /// </summary>
    [TestClass]
    public class NarrativeVocabularyTests
    {
        /// <summary>How a factory declares its specification: one field, or one method per member of the
        /// enum a parameterised factory is built for.</summary>
        private const string SpecFieldName = "Spec";

        private const string SpecFactoryName = "SpecFor";

        private List<INarrativeConditionFactory> _conditions = null!;
        private List<INarrativeActionFactory> _actions = null!;

        [TestInitialize]
        public void Setup()
        {
            _conditions = NarrativeFactories.Conditions(
                Mock.Of<IInventory>(),
                Mock.Of<IWorldFactsService>(),
                Mock.Of<IFactionRelationService>(),
                Mock.Of<IPersonalReputationService>(),
                Mock.Of<IPlayerAccessor>(),
                Mock.Of<IInfluenceMastery>(),
                Mock.Of<IWorldClock>(),
                new DefaultRandomNumberGenerator(seed: 44),
                Mock.Of<IQuestLogService>,
                Mock.Of<IQuestProvider>);

            _actions = NarrativeFactories.Actions(
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
        public void TheConditionVocabulary_IsWhatTheRegisteredFactoriesDeclare() =>
            AssertVocabularyMatches(NarrativeVocabulary.Conditions, [.. _conditions.Select(factory => factory.Parameters)], "condition");

        [TestMethod]
        public void TheActionVocabulary_IsWhatTheRegisteredFactoriesDeclare() =>
            AssertVocabularyMatches(NarrativeVocabulary.Actions, [.. _actions.Select(factory => factory.Parameters)], "action");

        /// <summary>The registry is written out by hand, so nothing but this holds it level with Core: a
        /// factory added there and forgotten here would be a word the editor cannot write.</summary>
        [TestMethod]
        public void EveryConditionFactoryInCore_IsNamedByTheVocabulary() =>
            AssertEveryFactoryIsNamed<INarrativeConditionFactory>(NarrativeVocabulary.Conditions, "condition");

        [TestMethod]
        public void EveryActionFactoryInCore_IsNamedByTheVocabulary() =>
            AssertEveryFactoryIsNamed<INarrativeActionFactory>(NarrativeVocabulary.Actions, "action");

        /// <summary>The tool may ask either half — the running registry or the vocabulary standing on its
        /// own — and is handed the same schemas.</summary>
        [TestMethod]
        public void TheSchemasReadWithoutAGame_AreTheSchemasOfTheFactories()
        {
            CollectionAssert.AreEquivalent(
                NarrativeSchemas.Conditions(_conditions).ToList(),
                NarrativeSchemas.Conditions().ToList(),
                "the condition schemas read from the vocabulary are not the ones the registered factories give");

            CollectionAssert.AreEquivalent(
                NarrativeSchemas.Actions(_actions).ToList(),
                NarrativeSchemas.Actions().ToList(),
                "the action schemas read from the vocabulary are not the ones the registered factories give");
        }

        /// <summary>Names first and in order, so a missing, surplus or displaced word is reported as
        /// itself; identity second, because a registry stating the same shape twice would let the two
        /// drift apart. The order is the one the game registers the factories in — a tool building the
        /// same list in another order is building a different list.</summary>
        private static void AssertVocabularyMatches(
            IReadOnlyList<NarrativeRecordSpec> vocabulary, List<NarrativeRecordSpec> declared, string what)
        {
            CollectionAssert.AreEqual(
                declared.Select(spec => spec.TypeName).ToList(),
                vocabulary.Select(spec => spec.TypeName).ToList(),
                $"the {what} vocabulary is not the registered {what}s, in the order they are registered");

            foreach (var spec in declared)
                Assert.AreSame(spec, vocabulary.Single(entry => entry.TypeName == spec.TypeName),
                    $"the {what} vocabulary states '{spec.TypeName}' as a specification of its own instead of the one its factory hands out");
        }

        private static void AssertEveryFactoryIsNamed<TFactory>(IReadOnlyList<NarrativeRecordSpec> vocabulary, string what)
        {
            foreach (var type in Factories<TFactory>())
            {
                var declared = DeclaredSpecs(type).ToList();
                Assert.IsTrue(declared.Count > 0,
                    $"'{type.Name}' declares no static '{SpecFieldName}' and no '{SpecFactoryName}', so nothing can name it without building it");

                foreach (var spec in declared)
                    Assert.IsTrue(vocabulary.Any(entry => ReferenceEquals(entry, spec)),
                        $"the {what} vocabulary does not name '{spec.TypeName}', declared by '{type.Name}'");
            }
        }

        private static IEnumerable<Type> Factories<TFactory>() =>
            typeof(TFactory).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(TFactory).IsAssignableFrom(type));

        /// <summary>Every specification a factory type states statically: the one it declares outright, or
        /// one per member of the enum it is parameterised by (the quest verbs).</summary>
        private static IEnumerable<NarrativeRecordSpec> DeclaredSpecs(Type factory)
        {
            const BindingFlags Static = BindingFlags.Public | BindingFlags.Static;

            if (factory.GetField(SpecFieldName, Static) is { } field && field.FieldType == typeof(NarrativeRecordSpec))
            {
                yield return (NarrativeRecordSpec)field.GetValue(null)!;
                yield break;
            }

            if (factory.GetMethod(SpecFactoryName, Static) is not { } method
                || method.ReturnType != typeof(NarrativeRecordSpec)
                || method.GetParameters() is not [{ ParameterType.IsEnum: true } parameter])
                yield break;

            foreach (object kind in Enum.GetValues(parameter.ParameterType))
                yield return (NarrativeRecordSpec)method.Invoke(null, [kind])!;
        }
    }
}
