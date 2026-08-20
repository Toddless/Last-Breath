namespace LastBreathTest.BattleSystemTests
{
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Battle.Source.Abilities.Activation;
    using Battle.Source.CombatRules;
    using Core.Battle;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Services;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// What the intelligence stance rolls on. The stage chances and their caps are the multiplier on the
    /// output of eight abilities, and they used to be a pair of dictionaries in the activation itself —
    /// a balance pass on them meant a rebuild. They are read from the combat rules now, so the walks
    /// below take the figures out of data and show the roll following them, rather than restating the
    /// shipped ladder as a constant of its own.
    /// </summary>
    [TestClass]
    public class MulticastStageRulesTests
    {
        /// <summary>The stage every cast lands on when no rolled stage comes up.</summary>
        private const int BaseStage = 1;

        /// <summary>A draw between the stage chances the walks author, so which side of it a stage falls
        /// on is decided by the declared number and by nothing else.</summary>
        private const float Draw = 0.7f;

        [TestMethod]
        public void TheStageComesUpAtTheChanceTheDataDeclaresAndNowhereElse()
        {
            // One draw, two files: the chance in the section is the whole difference between a cast that
            // multicasts and one that does not.
            Assert.AreEqual(2, RollAgainst(Draw, Section(stage: 2, chance: 0.8f, cap: 1f)),
                "a chance the data puts above the draw did not bring the stage up");
            Assert.AreEqual(BaseStage, RollAgainst(Draw, Section(stage: 2, chance: 0.1f, cap: 1f)),
                "a chance the data puts below the draw brought the stage up anyway — the roll is not reading the section");
        }

        [TestMethod]
        public void TheCapTheDataDeclaresHoldsTheChanceDownHoweverMuchMulticastTheCasterCarries()
        {
            // The caster carries ten times the multicast the stage costs, so the raw chance is far past
            // the draw and only the cap can keep the stage off.
            const float Multicast = 10f;

            Assert.AreEqual(BaseStage, RollAgainst(Draw, Section(stage: 3, chance: 0.25f, cap: 0.65f), Multicast),
                "the cap in the data no longer holds the raised chance down");
            Assert.AreEqual(3, RollAgainst(Draw, Section(stage: 3, chance: 0.25f, cap: 0.8f), Multicast),
                "a cap the data raises did not let the stage through — the roll is not reading the section");
        }

        [TestMethod]
        public void TheShippedFileIsWhatTheStanceRollsOn()
        {
            // Two halves of one road, as with the extension budget: a key the record does not know reads
            // as a default rather than as a failure, so the walk takes the rows out of the shipped file
            // itself and asks the loader for the same ones — and then puts them through a real roll.
            JToken[] declared = [.. DeclaredStages()];
            MulticastRules shipped = ShippedRules().Multicast;

            Assert.AreEqual(declared.Length, shipped.Stages.Count, "the loader carries a different set of stages than the file declares");
            foreach (JToken row in declared)
            {
                int stage = row["stage"]!.Value<int>();
                MulticastStage? loaded = shipped.Stages.FirstOrDefault(candidate => candidate.Stage == stage);

                Assert.IsNotNull(loaded, $"the loader dropped stage {stage}, which the shipped file declares");
                Assert.AreEqual(row["chance"]!.Value<float>(), loaded.BaseChance, 0.0001f,
                    $"stage {stage}: the loader answers with a chance the file does not declare");
                Assert.AreEqual(row["cap"]!.Value<float>(), loaded.Cap, 0.0001f,
                    $"stage {stage}: the loader answers with a cap the file does not declare");
            }

            // The lowest rolled stage is the likeliest one, so a draw just past its declared chance is
            // past every other stage as well and the cast has nothing left to land on but stage 1.
            MulticastStage lowest = shipped.Stages[^1];
            float above = lowest.BaseChance + 0.001f;
            Assert.IsTrue(shipped.Stages.Where(stage => stage != lowest).All(stage => MathF.Min(stage.BaseChance, stage.Cap) < above),
                "a higher stage is at least as likely as the lowest one, so the draw below proves nothing about stage 1");

            Assert.AreEqual(lowest.Stage, RollAgainst(lowest.BaseChance, shipped),
                "a draw at the chance the shipped file declares did not bring the stage up");
            Assert.AreEqual(BaseStage, RollAgainst(above, shipped),
                "a draw past every chance the shipped file declares still brought a stage up");
        }

        [TestMethod]
        public void WithNoRulesToReachTheStanceStillRollsTheWorkingLadder()
        {
            // The sandbox side of the same seam: abilities are cast by hosts that compose no services at
            // all, and the roll there answers from the working ladder instead of falling silent. It is
            // what the stage walks of the ability suites lean on — DischargeBarrierTests draws below the
            // stage-3 chance and above the stage-4 one, DeepFreezeDamageTests draws at both ends.
            Assert.IsNull(GameServiceProvider.TryGet<ICombatRulesProvider>(),
                "this host composes combat rules after all, so what the walk measures is not the fallback");

            foreach (MulticastStage stage in MulticastRules.Default.Stages)
            {
                using var rolls = new CombatRandomScope(new FixedDraw(stage.BaseChance));

                Assert.AreEqual(stage.Stage, new MulticastActivation().Roll(Caster()),
                    $"a roll with no rules to reach no longer lands on stage {stage.Stage} at its own chance");
            }
        }

        [TestMethod]
        public void ARowNamingNoRollableStageIsDroppedAndASectionOfNothingElseFallsBack()
        {
            // Stage 1 is not rolled and a share outside 0..1 is not a chance; either row is a typo rather
            // than a rule, and taking one would quietly change what every cast of the stance comes out at.
            MulticastRules mixed = RulesFrom(
                """
                { "multicast": { "stages": [
                    { "stage": 1, "chance": 0.5, "cap": 1 },
                    { "stage": 3, "chance": 1.5, "cap": 1 },
                    { "stage": 2, "chance": 0.4, "cap": 1 } ] } }
                """);

            Assert.AreEqual(1, mixed.Stages.Count, "a row naming no rollable stage was taken as a rule");
            Assert.AreEqual(2, mixed.Stages[0].Stage, "the one sound row is not the one that survived");

            string[] empty = ["""{ "multicast": { "stages": [] } }""", "{ }"];
            foreach (string section in empty)
                Assert.IsTrue(RulesFrom(section).Stages.SequenceEqual(MulticastRules.Default.Stages),
                    $"a section declaring no rollable stage ({section}) left the stance without a ladder to roll on");
        }

        /// <summary>One multicast section carrying a single stage row.</summary>
        private static string Section(int stage, float chance, float cap) =>
            $$"""
              { "multicast": { "stages": [ { "stage": {{stage}}, "chance": {{Number(chance)}}, "cap": {{Number(cap)}} } ] } }
              """;

        private static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>The stage a cast lands on when the roll draws a fixed number against the rules the
        /// given section declares.</summary>
        private static int RollAgainst(float draw, string section, float multicast = 0f) =>
            RollAgainst(draw, RulesFrom(section), multicast);

        private static int RollAgainst(float draw, MulticastRules rules, float multicast = 0f)
        {
            using var rolls = new CombatRandomScope(new FixedDraw(draw));
            return new MulticastActivation().Roll(Caster(multicast), rules);
        }

        /// <summary>The multicast rules a section parses to, through the loader the game reads it with.</summary>
        private static MulticastRules RulesFrom(string section)
        {
            var provider = new CombatRulesProvider();
            provider.Apply(DataCatalog.CombatRules, new GameDataFile("CombatRules.json", section));
            return provider.Multicast;
        }

        /// <summary>The stage rows as the shipped file spells them out.</summary>
        private static IEnumerable<JToken> DeclaredStages()
        {
            string file = Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.CombatRules), "*.json").Single();
            JToken? stages = JObject.Parse(File.ReadAllText(file))["multicast"]?["stages"];

            Assert.IsNotNull(stages, "the shipped combat rules declare no multicast stages at all");
            return stages;
        }

        /// <summary>The combat rules as the shipped catalog declares them, through the real loader.</summary>
        private static ICombatRulesProvider ShippedRules()
        {
            var provider = new CombatRulesProvider();
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            return provider;
        }

        /// <summary>A caster carrying nothing but its own multicast chance.</summary>
        private static IFightable Caster(float multicast = 0f)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            parameters.Setup(entity => entity.GetValueForParameter(EntityParameter.MulticastChance)).Returns(multicast);

            var caster = new Mock<IFightable>();
            caster.Setup(fighter => fighter.Parameters).Returns(parameters.Object);
            return caster.Object;
        }

        /// <summary>Every draw the same, so where a stage falls is decided by its chance alone.</summary>
        private sealed class FixedDraw(float draw) : IRandomNumberGenerator
        {
            public float RandFloat() => draw;

            public float RandFloatRange(float min, float max) => max;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }
    }
}
