namespace LastBreathTest.BattleSystemTests
{
    using System.Reflection;
    using Core.Ai.World.Time;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Inventory;
    using Core.Narrative;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;
    using LastBreath.Descriptors;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Holds every condition factory's declared parameters against the parser that actually reads them:
    /// a field called required the parser lives without, a default it does not read, or a key it reads
    /// and the schema never names, fails here. The entries are built FROM the schema, so a schema and a
    /// parser can only agree honestly.
    /// </summary>
    [TestClass]
    public class NarrativeConditionSchemaTests
    {
        private const string StubText = "x";
        private const string StubReference = "Some_Id";
        private const int StubNumber = 1;
        private const string UnknownKey = "authoringNote";

        /// <summary>What a factory's json keys are called: a constant reads the file, so a constant is
        /// what the schema can be held against. A key written as a literal inside Create is beyond this
        /// pin — the forward checks below are what catch that.</summary>
        private const string KeySuffix = "Key";

        /// <summary>Defaults nothing here can watch being read. The offer roll reaches the line writing its
        /// cooldown only past a Godot RandomNumberGenerator, which cannot exist outside the engine.</summary>
        private static readonly string[] s_unwatchableDefaults = ["QuestOfferRoll.cooldownHours"];

        private WorldFactsService _facts = null!;
        private INarrativeConditionFactory _fact = null!;
        private List<INarrativeConditionFactory> _factories = null!;
        private NarrativeConditionParser _parser = null!;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();

            // The probes below ask each condition for one step above its declared default, so the world is
            // set exactly at that default: "at least the default" is met, one more is not.
            _facts.SetCount(StubText, StubNumber);

            var inventory = new Mock<IInventory>();
            inventory.Setup(bag => bag.GetTotalItemAmount(It.IsAny<string>())).Returns(StubNumber);

            _factories = NarrativeFactories.Conditions(
                inventory.Object,
                _facts,
                Mock.Of<IFactionRelationService>(),
                Mock.Of<IPersonalReputationService>(),
                PlayerAtZero(),
                Mock.Of<IInfluenceMastery>(),
                Mock.Of<IWorldClock>(),
                new DefaultRandomNumberGenerator(seed: 11),
                Mock.Of<IQuestLogService>,
                Mock.Of<IQuestProvider>);

            _fact = _factories.OfType<FactConditionFactory>().Single();
            _parser = new NarrativeConditionParser(_factories);
        }

        /// <summary>The list above stands in for the DI registry, which lives in a project the tests do not
        /// reference; reflection is what keeps it from falling behind Core.</summary>
        [TestMethod]
        public void EveryConditionFactoryInCore_IsUnderTest()
        {
            var declared = typeof(INarrativeConditionFactory).Assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(INarrativeConditionFactory).IsAssignableFrom(type))
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            var covered = _factories.Select(factory => factory.GetType().Name).OrderBy(name => name, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(declared, covered,
                $"a condition factory is missing from this test (and probably from the DI registry): [{string.Join(", ", declared.Except(covered))}]");
        }

        [TestMethod]
        public void EveryFactory_DeclaresParametersUnderItsOwnType()
        {
            foreach (var factory in _factories)
            {
                var schema = factory.Parameters;
                Assert.AreEqual(factory.Type, schema.TypeName, $"'{factory.GetType().Name}' declares its parameters under another type");

                var names = schema.Parameters.Select(field => field.JsonName).ToList();
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
                var declared = factory.Parameters.Parameters.Select(field => field.JsonName).ToList();

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
                foreach (var field in factory.Parameters.Parameters.Where(field => !field.Required))
                {
                    string address = $"{factory.Type}.{field.JsonName}";
                    Assert.IsNotNull(field.Default, $"'{address}' is optional and must name what the parser reads in its place");
                    Assert.IsInstanceOfType<int>(field.Default, $"'{address}' defaults to something this probe cannot write: {field.Default}");

                    if (s_unwatchableDefaults.Contains(address)) continue;

                    int stated = (int)field.Default;
                    bool absent = Met(factory, field.JsonName, written: null);

                    Assert.AreEqual(Met(factory, field.JsonName, stated), absent,
                        $"'{address}' is declared to default to {stated}, and the parser reads something else when the key is absent");
                    Assert.AreNotEqual(Met(factory, field.JsonName, stated + 1), absent,
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

        private static IEnumerable<NarrativeParameterSpec> Required(INarrativeConditionFactory factory) =>
            factory.Parameters.Parameters.Where(field => field.Required);

        /// <summary>A factory's own json-key constants, its base classes' included: a composite reads the
        /// key its base declares, and FlattenHierarchy does not reach a private one.</summary>
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

        private static IPlayerAccessor PlayerAtZero()
        {
            var attribute = new Mock<IEntityAttribute>();
            attribute.Setup(value => value.Total).Returns(0);

            var player = new Mock<IPlayer>();
            player.Setup(hero => hero.Strength).Returns(attribute.Object);
            player.Setup(hero => hero.Dexterity).Returns(attribute.Object);
            player.Setup(hero => hero.Intelligence).Returns(attribute.Object);

            var accessor = new Mock<IPlayerAccessor>();
            accessor.Setup(holder => holder.Player).Returns(player.Object);

            return accessor.Object;
        }

        private bool Met(INarrativeConditionFactory factory, string jsonName, int? written)
        {
            var json = Minimal(factory);
            if (written is { } value) json[jsonName] = value;

            var condition = _parser.Parse(json);
            Assert.IsNotNull(condition, $"'{factory.Type}' rejected the entry probing '{jsonName}'");

            return condition.IsMet(NarrativeContext.Empty);
        }

        /// <summary>The smallest entry the schema allows: every required field filled with a stand-in of the
        /// kind it asks for, and nothing else.</summary>
        private JObject Minimal(INarrativeConditionFactory factory)
        {
            var json = new JObject { [NarrativeParameterSchema.TypeKey] = factory.Parameters.TypeName };
            foreach (var field in Required(factory))
                json[field.JsonName] = Stub(field);

            return json;
        }

        private JToken Stub(NarrativeParameterSpec field) => field.Kind switch
        {
            NarrativeParameterKind.Text => StubText,
            NarrativeParameterKind.Reference => StubReference,
            NarrativeParameterKind.Integer or NarrativeParameterKind.Number => StubNumber,
            NarrativeParameterKind.Boolean => true,
            NarrativeParameterKind.Choice => First(field),
            // A nested condition names the vocabulary rather than a record of its own; the fact counter is
            // the simplest entry that vocabulary can build.
            NarrativeParameterKind.NestedCondition => Minimal(_fact),
            NarrativeParameterKind.NestedConditions => new JArray(Minimal(_fact)),
            _ => throw Unstubbable(field, $"a {field.Kind} this probe has no stand-in for"),
        };

        private static JToken First(NarrativeParameterSpec field) =>
            field.Choices.Count > 0 ? field.Choices[0] : throw Unstubbable(field, "a choice offering no members");

        private static AssertFailedException Unstubbable(NarrativeParameterSpec field, string what) =>
            new($"'{field.JsonName}' is {what}");
    }
}
