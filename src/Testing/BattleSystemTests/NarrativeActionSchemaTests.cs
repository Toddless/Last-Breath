namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;
    using Moq;
    using Newtonsoft.Json.Linq;
    using Tooling.Schema.Model;

    /// <summary>
    /// Holds every action factory's declared parameters against the parser that actually reads them: a
    /// field called required the parser lives without, a default it does not read, or a key it reads and
    /// the schema never names, fails here. The entries are built FROM the schema, so a schema and a
    /// parser can only agree honestly. An action is executed rather than asked, so a default is proven by
    /// what the world got: a fact counted, an amount handed over, a reason recorded.
    /// </summary>
    [TestClass]
    public class NarrativeActionSchemaTests
    {
        private const string StubText = "x";
        private const string StubReference = "Some_Id";
        private const int StubNumber = 1;
        private const string UnknownKey = "authoringNote";

        /// <summary>What a factory's json keys are called: a constant reads the file, so a constant is
        /// what the schema can be held against. A key written as a literal inside Create is beyond this
        /// pin — the forward checks below are what catch that.</summary>
        private const string KeySuffix = "Key";

        /// <summary>Optional fields no probe can hold to a default. SpawnNpc's modifier list has none to
        /// hold it to: an absent list leaves the NPC record's own roll standing, which happens inside a
        /// Godot spawn, while a written one names the exact set — two different answers, not a value and
        /// its fallback.</summary>
        private static readonly string[] s_unwatchableDefaults = ["SpawnNpc.modifiers"];

        private WorldFactsService _facts = null!;
        private int _givenAmount;
        private int _takenAmount;
        private string? _recordedReason;
        private List<INarrativeActionFactory> _factories = null!;
        private Dictionary<INarrativeActionFactory, Func<object?>> _watchers = null!;
        private NarrativeActionParser _parser = null!;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();

            var inventory = new Mock<IInventory>();
            inventory.Setup(bag => bag.TryAddItem(It.IsAny<IItem>(), It.IsAny<int>()))
                .Callback<IItem, int>((_, amount) => _givenAmount = amount);
            inventory.Setup(bag => bag.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()))
                .Callback<string, int>((_, amount) => _takenAmount = amount);

            var relations = new Mock<IFactionRelationService>();
            relations.Setup(service => service.AddReputation(It.IsAny<Fractions>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<Fractions, int, string>((_, _, reason) => _recordedReason = reason);

            _factories = NarrativeTestFactories.Actions(
                _facts,
                inventory.Object,
                Mock.Of<IItemMinter>(),
                Mock.Of<IGameEventBus>(),
                Mock.Of<IPlayerAccessor>(),
                relations.Object,
                Mock.Of<IInfluenceMastery>(),
                Mock.Of<IMartialArtMastery>(),
                Mock.Of<IGameMessageBus>(),
                Mock.Of<INpcProvider>(),
                Mock.Of<INpcModifierProvider>(),
                Mock.Of<INpcWorldSpawner>(),
                Mock.Of<INpcPopulationService>(),
                Mock.Of<ISpawnPointRegistry>(),
                Mock.Of<IQuestLogService>);

            // What each action with an optional field is seen doing: the world the probe below reads back.
            _watchers = new Dictionary<INarrativeActionFactory, Func<object?>>
            {
                [Single<SetFactActionFactory>()] = () => _facts.GetCount(StubText),
                [Single<GiveItemActionFactory>()] = () => _givenAmount,
                [Single<TakeItemActionFactory>()] = () => _takenAmount,
                [Single<AddReputationActionFactory>()] = () => _recordedReason,
            };

            _parser = new NarrativeActionParser(_factories);
        }

        /// <summary>The list above stands in for the DI registry, which lives in a project the tests do not
        /// reference; reflection is what keeps it from falling behind Core.</summary>
        [TestMethod]
        public void EveryActionFactoryInCore_IsUnderTest()
        {
            var declared = typeof(INarrativeActionFactory).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(INarrativeActionFactory).IsAssignableFrom(type))
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            // One class answers to four types, so the class list is compared without its repeats and the
            // instances are counted separately.
            var covered = _factories.Select(factory => factory.GetType().Name).Distinct()
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(declared, covered,
                $"an action factory is missing from this test (and probably from the DI registry): [{string.Join(", ", declared.Except(covered))}]");

            Assert.AreEqual(Enum.GetValues<QuestActionKind>().Length, _factories.OfType<QuestActionFactory>().Count(),
                "the quest vocabulary registers one factory per kind");
        }

        [TestMethod]
        public void EveryFactory_DeclaresParametersUnderItsOwnType()
        {
            CollectionAssert.AllItemsAreUnique(_factories.Select(factory => factory.Type).ToList(),
                "two factories answer to one type — the parser builds its registry by that name and would refuse the pair");

            foreach (var factory in _factories)
            {
                var schema = factory.Parameters;
                Assert.AreEqual(factory.Type, schema.TypeName, $"'{factory.GetType().Name}' declares its parameters under another type");

                var names = schema.Fields.Select(field => field.JsonName).ToList();
                Assert.IsFalse(names.Any(string.IsNullOrWhiteSpace), $"'{factory.Type}' declares a nameless parameter");
                CollectionAssert.AllItemsAreUnique(names, $"'{factory.Type}' declares one json key twice");
            }
        }

        /// <summary>The other direction: a key the factory reads and the schema forgot would leave the
        /// editor writing entries the game silently ignores.</summary>
        [TestMethod]
        public void EveryJsonKeyAFactoryReads_IsDeclared()
        {
            foreach (var factory in _factories)
            {
                var declared = factory.Parameters.Fields.Select(field => field.JsonName).ToList();

                foreach (var constant in JsonKeyConstants(factory.GetType()))
                    CollectionAssert.Contains(declared, (string)constant.GetRawConstantValue()!,
                        $"'{factory.GetType().Name}.{constant.Name}' names a json key the schema of '{factory.Type}' does not declare");
            }
        }

        [TestMethod]
        public void AnEntryBuiltFromTheSchema_Parses()
        {
            foreach (var factory in _factories)
                Assert.IsNotNull(_parser.Parse(Minimal(factory)),
                    $"'{factory.Type}': an entry holding every field the schema calls required was rejected by the parser");
        }

        [TestMethod]
        public void DroppingARequiredField_BreaksTheParse()
        {
            foreach (var factory in _factories)
                foreach (var field in Required(factory))
                {
                    var json = Minimal(factory);
                    json.Remove(field.JsonName);

                    Assert.IsNull(_parser.Parse(json),
                        $"'{factory.Type}.{field.JsonName}' is declared required, but the parser reads the entry without it");
                }
        }

        [TestMethod]
        public void AnAbsentOptionalField_ReadsAsItsDeclaredDefault()
        {
            foreach (var factory in _factories)
                foreach (var field in factory.Parameters.Fields.Where(field => !field.Required))
                {
                    string address = $"{factory.Type}.{field.JsonName}";
                    if (s_unwatchableDefaults.Contains(address)) continue;

                    Assert.IsNotNull(field.Default, $"'{address}' is optional and must name what the parser reads in its place");

                    object? absent = Did(factory, field.JsonName, written: null);

                    Assert.AreEqual(Did(factory, field.JsonName, field.Default), absent,
                        $"'{address}' is declared to default to {field.Default}, and the parser reads something else when the key is absent");
                    Assert.AreNotEqual(Did(factory, field.JsonName, Another(field.Default, address)), absent,
                        $"the probe cannot tell '{address}' values apart, so the comparison above proves nothing");
                }
        }

        /// <summary>What the editor may do with a key the game does not read: the parser drops it without a
        /// word, so nothing but the file itself remembers it.</summary>
        [TestMethod]
        public void AKeyNoFactoryReads_IsIgnored()
        {
            foreach (var factory in _factories)
            {
                var json = Minimal(factory);
                json[UnknownKey] = "written by an editor, read by nobody";

                Assert.IsNotNull(_parser.Parse(json), $"'{factory.Type}' refused an entry carrying an unknown key");
            }
        }

        private static IEnumerable<FieldSchema> Required(INarrativeActionFactory factory) =>
            factory.Parameters.Fields.Where(field => field.Required);

        /// <summary>A factory's own json-key constants, its base classes' included.</summary>
        private static IEnumerable<FieldInfo> JsonKeyConstants(Type factory)
        {
            const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

            for (Type? level = factory; level != null && level != typeof(object); level = level.BaseType)
                foreach (var constant in level.GetFields(Declared))
                    if (constant is { IsLiteral: true, IsInitOnly: false }
                        && constant.FieldType == typeof(string)
                        && constant.Name.EndsWith(KeySuffix, StringComparison.Ordinal))
                        yield return constant;
        }

        /// <summary>A value of the same kind the default is written in, and a different one: what the probe
        /// writes to prove it can tell the declared default from anything else.</summary>
        private static object Another(object? declared, string address) => declared switch
        {
            int number => number + 1,
            string text => text + StubText,
            _ => throw Unstubbable(address, $"a default this probe cannot vary: {declared}"),
        };

        /// <summary>What the world got when the entry was parsed and executed — with the field under probe
        /// written out, or left absent so the parser reaches for its fallback.</summary>
        private object? Did(INarrativeActionFactory factory, string jsonName, object? written)
        {
            Assert.IsTrue(_watchers.TryGetValue(factory, out var watched),
                $"'{factory.Type}' declares an optional field and nothing here watches what the parser reads in its place");

            var json = Minimal(factory);
            if (written != null) json[jsonName] = JToken.FromObject(written);

            var action = _parser.Parse(json);
            Assert.IsNotNull(action, $"'{factory.Type}' rejected the entry probing '{jsonName}'");

            Forget();
            action.Execute(NarrativeContext.Empty);

            return watched();
        }

        /// <summary>Every probe starts from a world that did nothing yet: facts count up, so a second run
        /// against the same registry would read the first one's answer as well.</summary>
        private void Forget()
        {
            _facts.SetCount(StubText, 0);
            _givenAmount = 0;
            _takenAmount = 0;
            _recordedReason = null;
        }

        private TFactory Single<TFactory>()
            where TFactory : INarrativeActionFactory => _factories.OfType<TFactory>().Single();

        /// <summary>The smallest entry the schema allows: every required field filled with a stand-in of the
        /// kind it asks for, and nothing else.</summary>
        private JObject Minimal(INarrativeActionFactory factory)
        {
            var json = new JObject { [NarrativeParameterSchema.TypeKey] = factory.Parameters.TypeName };
            foreach (var field in Required(factory))
                json[field.JsonName] = Stub(field);

            return json;
        }

        private static JToken Stub(FieldSchema field) => field.Kind switch
        {
            FieldKind.String => StubText,
            FieldKind.Reference => StubReference,
            FieldKind.Integer or FieldKind.Number => StubNumber,
            FieldKind.Boolean => true,
            FieldKind.Enum => First(field),
            _ => throw Unstubbable(field.JsonName, $"a {field.Kind} this probe has no stand-in for"),
        };

        private static JToken First(FieldSchema field) =>
            field.EnumValues.Count > 0 ? field.EnumValues[0] : throw Unstubbable(field.JsonName, "a choice offering no members");

        private static AssertFailedException Unstubbable(string address, string what) => new($"'{address}' is {what}");
    }
}
