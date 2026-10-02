namespace LastBreathTest.Loot
{
    using System.Collections.Generic;
    using System.Reflection;
    using Ability;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using LootGeneration.Internal;
    using Moq;

    /// <summary>
    /// Where the randomness the sandbox spawner rolls on comes from, and who decides it — the same question
    /// the cast and delivery seats answer for combat (see <see cref="CombatGeneratorCompositionTests"/>),
    /// asked of the one project outside Battle that also built the engine generator itself. The spawner
    /// held it in a field initializer, the worst shape of the mine: there is nothing to execute and nothing
    /// to catch, so merely CREATING a spawner took the whole process down (0xC0000005) and every remaining
    /// test with it. The test host references this project, so the mine was one <c>new Spawner()</c> away
    /// the entire time and held only because nothing walked over it. These tests walk over it.
    /// </summary>
    [TestClass]
    public class SpawnerGeneratorCompositionTests
    {
        [TestMethod]
        public void CreatingTheSpawnerIsSurvivable()
        {
            // Reaching the assertion at all is the proof: a native generator in a field initializer kills
            // the host before any assertion can report it.
            var spawner = new Spawner();

            Assert.IsNotNull(spawner, "the spawner refused to be built at all");
        }

        [TestMethod]
        public void TheSpawnerDefaultsToAStreamTheEngineNeverBuilt()
        {
            // Nobody has seated anything yet: a fresh spawner must already be able to roll, because that
            // is what a host without Godot gets.
            var spawner = new Spawner();

            Assert.IsInstanceOfType<DefaultRandomNumberGenerator>(Seated(spawner),
                "a spawner outside the engine must roll on the pure-C# stream — anything native there dies on construction");
        }

        [TestMethod]
        public void EverySpawnDecisionRollsOnTheSeatedStream()
        {
            // Creation being survivable is half of it: every decision the spawner was built to roll has to
            // land on the seated stream, or the generator simply moved somewhere the composition cannot reach.
            var rolls = new CountingRandom();
            var spawner = new Spawner();
            spawner.SetRandomNumberGenerator(rolls);
            spawner.SetNpcModifierProvider(ModifierProvider());

            foreach ((string decision, object[] arguments) in Decisions())
            {
                int before = rolls.Count;
                Roll(spawner, decision, arguments);
                Assert.IsTrue(rolls.Count > before,
                    $"{decision} decided without asking the seated stream for anything — its roll is somewhere the seat does not reach");
            }
        }

        [TestMethod]
        public void TheSceneThatBootsTheSandboxSeatsTheEngineGenerator() =>
            // The other half of the chain: the seat is worth exactly as much as the scene that fills it.
            GodotBootstraps.AssertInstalls(
                Path.Combine("LootGeneration", "Internal", "Main.cs"),
                nameof(Spawner.SetRandomNumberGenerator));

        /// <summary>Every spawn decision that owns a roll, with what it takes to ask for one.</summary>
        private static IEnumerable<(string Decision, object[] Arguments)> Decisions() =>
        [
            ("GetRandomRarity", []),
            ("GetRandomFraction", []),
            ("GetRandomLevel", [Rarity.Legendary]),
            ("GetRandomEntityType", []),
            ("GetAmountNpcModifiers", [EntityType.Boss, Rarity.Mythic]),
            ("GetRandomNpcModifiers", [1]),
            ("GetRandomPosition", []),
        ];

        /// <summary>Drives one spawn decision. The decisions are the spawner's own business — the public
        /// entry points around them instantiate a Godot scene, which no host without the engine survives.</summary>
        private static void Roll(Spawner spawner, string decision, object[] arguments)
        {
            MethodInfo? method = typeof(Spawner).GetMethod(decision, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, $"{nameof(Spawner)} no longer decides {decision}, so nothing here says what it rolls on");
            method.Invoke(spawner, arguments);
        }

        /// <summary>A catalog with something pickable in it: an empty one never reaches the weighted roll.</summary>
        private static INpcModifierProvider ModifierProvider()
        {
            var modifier = new Mock<INpcModifier>();
            modifier.SetupGet(entry => entry.Weight).Returns(1f);
            modifier.Setup(entry => entry.Copy()).Returns(() => modifier.Object);

            var provider = new Mock<INpcModifierProvider>();
            provider.Setup(catalog => catalog.GetAllModifiers()).Returns([modifier.Object]);

            return provider.Object;
        }

        /// <summary>What the spawner currently rolls on.</summary>
        private static IRandomNumberGenerator? Seated(Spawner spawner)
        {
            FieldInfo? seat = typeof(Spawner).GetField("_rnd", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(seat, $"{nameof(Spawner)} no longer holds a stream to roll on");
            return seat.GetValue(spawner) as IRandomNumberGenerator;
        }
    }
}
