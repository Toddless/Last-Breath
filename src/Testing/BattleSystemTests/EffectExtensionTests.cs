namespace LastBreathTest.BattleSystemTests
{
    using System.IO;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.CombatRules;
    using Battle.Source.Effects;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.CombatRulesData;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A standing effect can be made to last longer, and everything that does it goes through one
    /// door: <see cref="Effect.Extend"/>. Raising the duration from outside is what used to happen —
    /// a rider firing on every impact of a long series added a turn each time, and a poison that
    /// nothing interrupts outlives the fight it was cast in.
    ///
    /// What replaces it is a budget carried by the INSTANCE: an effect may be extended by so many
    /// turns in total, whoever extends it, and past that the extension is a no-op that says so. The
    /// boundary the budget is drawn at matters as much as the number — extending is what happens to
    /// an effect that has ALREADY landed. The mutators that shape a duration while the effect is
    /// being applied (duration scaling, flat duration bonuses, control resistance, a re-application
    /// refreshing the standing stack) are the other side of that line and pay nothing here.
    /// </summary>
    [TestClass]
    public class EffectExtensionTests
    {
        private const int PoisonTurns = 3;
        private const int BuffTurns = 3;

        /// <summary>The shipped catalog the combat rules are read from.</summary>
        private const string RulesCatalog = "CombatRules";

        [TestMethod]
        public async Task ExtensionsAddUpWhileTheBudgetHasRoomForThem()
        {
            // The plain case the whole mechanism exists to serve: an extender lengthens the effect,
            // and two of them lengthen it twice.
            DamageOverTurnEffect poison = await StandingPoison();
            Assert.IsTrue(poison.ExtensionBudget >= 2, "the rules leave no room for two extensions, so this walk proves nothing");

            Assert.AreEqual(1, poison.Extend(1), "the first extension was refused");
            Assert.AreEqual(1, poison.Extend(1), "the second extension was refused");

            Assert.AreEqual(PoisonTurns + 2, poison.Duration, "the extensions did not add up on the effect");

            // And the other end of the same road: an effect that has run its last turn lies in the
            // list until the turn end takes it away, and the budget still has room in it. Extending
            // there would raise the dead rather than lengthen the living.
            for (int turn = 0; turn < PoisonTurns + 2; turn++) poison.TurnEnd();
            Assert.AreEqual(0, poison.Duration, "the poison did not run out, so the assert below proves nothing");

            Assert.AreEqual(0, poison.Extend(1), "an effect that had run out was raised by an extension");
            Assert.AreEqual(0, poison.Duration, "the refused extension moved the duration anyway");
        }

        [TestMethod]
        public async Task ACritLoopRunsTheSelfExtendingBuffIntoItsBudget()
        {
            // The buff that extends ITSELF: every critical hit its bearer lands adds a turn. On a raw
            // "Duration++" the crit build that the buff exists to reward held it up for the whole
            // fight — six crits made it six turns longer, and nothing anywhere said stop.
            var caster = new Fighter();
            var bearer = new Fighter();
            var buff = new CritCalculationBuff(BuffTurns, maxStacks: 1, value: 1.1f);
            await buff.Apply(new EffectApplyingContext
            {
                Caster = caster.Object,
                Target = bearer.Object,
                Source = "Test_Buff",
                Damage = default
            });
            Assert.AreEqual(BuffTurns, buff.Duration, "the buff never landed, so the crits below prove nothing");
            int budget = buff.ExtensionBudget;

            var crit = new AfterAttackEvent(Mock.Of<IAttackContext>(context => context.IsCritical));
            for (int hit = 0; hit < budget + 3; hit++) bearer.Object.CombatEvents.Publish(crit);

            Assert.AreEqual(BuffTurns + budget, buff.Duration, "the crit loop extended the buff past its budget");
        }

        [TestMethod]
        public async Task TheBudgetClampsWhatIsAskedForAndSaysHowMuchItGave()
        {
            // The pin of the cap itself. On a raw "Duration +=" this asked for twice the budget and got
            // it: the poison stood at PoisonTurns + budget * 2, and the extension after that pushed it
            // one turn further still, with nothing in the chain able to tell that it had.
            DamageOverTurnEffect poison = await StandingPoison();
            int budget = poison.ExtensionBudget;
            Assert.IsTrue(budget > 0, "the rules cap extension at nothing, so this walk proves nothing");

            int granted = poison.Extend(budget * 2);

            Assert.AreEqual(budget, granted, "the extension reported turns the effect never received");
            Assert.AreEqual(PoisonTurns + budget, poison.Duration, "the effect took more than its budget");

            Assert.AreEqual(0, poison.Extend(1), "a spent budget went on giving turns away");
            Assert.AreEqual(PoisonTurns + budget, poison.Duration, "the refused extension moved the duration anyway");
        }

        [TestMethod]
        public async Task EveryInstanceCarriesABudgetOfItsOwn()
        {
            // Two stacks on one victim. The budget belongs to the instance, so spending the first
            // one's room says nothing about the second: a second stack is worth stacking, and a cap
            // shared between them would make every stack after the first a shorter one.
            var caster = new Fighter();
            var victim = new Fighter();
            DamageOverTurnEffect first = await StandingPoison(caster, victim);
            DamageOverTurnEffect second = await StandingPoison(caster, victim);

            first.Extend(first.ExtensionBudget);

            Assert.AreEqual(0, first.Extend(1), "the first stack outspent its own budget");
            Assert.AreEqual(1, second.Extend(1), "the second stack paid for the first one's extensions");
            Assert.AreEqual(PoisonTurns + 1, second.Duration, "the second stack was not extended at all");
        }

        [TestMethod]
        public void TheBudgetIsWrittenInTheCombatRules()
        {
            // Two halves of one road: a key the record does not know reads as a default rather than as
            // a failure, which is how a mapping has died silently in this project before. So the walk
            // takes the number out of the shipped file itself and asks the loader for the same one —
            // a rename on either side splits them, whatever the defaults happen to be worth.
            const string json = """{ "effects": { "maxExtendedTurns": 7 } }""";
            var data = JsonConvert.DeserializeObject<CombatRulesData>(json)!;
            Assert.AreEqual(7, data.Effects.MaxExtendedTurns, "the section is in the file and nothing reads it");

            string file = Directory.EnumerateFiles(SharedData.Catalog(RulesCatalog), "*.json").Single();
            int declared = JObject.Parse(File.ReadAllText(file))["effects"]!["maxExtendedTurns"]!.Value<int>();

            Assert.AreEqual(declared, ShippedRules().Effects.MaxExtendedTurns,
                "the loader answers with something other than what the shipped file declares");
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

        /// <summary>A poison that has actually landed on a victim — extension is what happens AFTER an
        /// effect is applied, so a walk about it starts from an effect that went through the pipeline
        /// rather than from a bare instance.</summary>
        private static async Task<DamageOverTurnEffect> StandingPoison(Fighter? caster = null, Fighter? victim = null)
        {
            var poison = new DamageOverTurnEffect(PoisonTurns, StatusEffects.Poison);
            await poison.Apply(new EffectApplyingContext
            {
                Caster = (caster ?? new Fighter()).Object,
                Target = (victim ?? new Fighter()).Object,
                Source = "Test_Poison",
                Damage = DamageSnapshot.Of(DamageType.Physical, 100f)
            });

            Assert.AreEqual(PoisonTurns, poison.Duration, "the poison never landed, so the extensions below prove nothing");
            return poison;
        }

        /// <summary>A fightable an effect can land on: real effects and modifier pipelines, only the
        /// entity itself is a mock.</summary>
        private sealed class Fighter
        {
            private readonly Mock<IFightable> _mock = new();

            public IFightable Object => _mock.Object;

            public Fighter()
            {
                var effects = new EffectsComponent(_mock.Object);
                _mock.Setup(fighter => fighter.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(fighter => fighter.IsAlive).Returns(true);
                _mock.Setup(fighter => fighter.Effects).Returns(effects);
                _mock.Setup(fighter => fighter.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(fighter => fighter.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(fighter => fighter.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(fighter => fighter.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
            }
        }
    }
}
