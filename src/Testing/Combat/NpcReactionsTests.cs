namespace LastBreathTest.Combat
{
    using Battle.Source;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Narrative.Facts;
    using Moq;

    /// <summary>
    /// The twin-boss reaction stack: strict parse of the "reactions" section and the driver's
    /// gates — per-turn cap with a reset on every new turn, the "twin finally dead" world-fact
    /// block, the attack-only trigger, and no fire from a dead owner or a finished battle.
    /// Pure C# + Moq, no Godot runtime.
    /// </summary>
    [TestClass]
    public class NpcReactionsTests
    {
        // ---------- parsing ----------

        [TestMethod]
        public void Parse_ValidEntry_ProducesConfig()
        {
            var parsed = NpcReactionParser.Parse("Npc_Boss_Digri",
            [
                new NpcReactionData
                {
                    AbilityId = "Ability_Twin_Assist_Attack",
                    Trigger = "DamagedByAttack",
                    Chance = 0.15f,
                    MaxPerTurn = 3,
                    BlockedByFinalDeathOf = "Npc_Boss_Zigri"
                }
            ]);

            Assert.AreEqual(1, parsed.Count);
            var config = parsed[0];
            Assert.AreEqual("Ability_Twin_Assist_Attack", config.AbilityId);
            Assert.AreEqual(ReactionTrigger.DamagedByAttack, config.Trigger);
            Assert.AreEqual(0.15f, config.Chance);
            Assert.AreEqual(3, config.MaxPerTurn);
            Assert.AreEqual("Npc_Boss_Zigri", config.BlockedByFinalDeathOf);
        }

        [TestMethod]
        public void Parse_BrokenEntries_AreSkippedWithoutKillingTheRest()
        {
            var parsed = NpcReactionParser.Parse("Npc_Test",
            [
                new NpcReactionData { AbilityId = "Ability_A", Trigger = "NoSuchTrigger", Chance = 0.5f, MaxPerTurn = 1 },
                new NpcReactionData { AbilityId = "", Trigger = "DamagedByAttack", Chance = 0.5f, MaxPerTurn = 1 },
                new NpcReactionData { AbilityId = "Ability_B", Trigger = "DamagedByAttack", Chance = 1.5f, MaxPerTurn = 1 },
                new NpcReactionData { AbilityId = "Ability_C", Trigger = "DamagedByAttack", Chance = 0.5f, MaxPerTurn = 0 },
                new NpcReactionData { AbilityId = "Ability_D", Trigger = "DamagedByAttack", Chance = 0.5f, MaxPerTurn = 2 }
            ]);

            Assert.AreEqual(1, parsed.Count, "only the last entry is valid");
            Assert.AreEqual("Ability_D", parsed[0].AbilityId);
        }

        [TestMethod]
        public void Parse_MissingBlockedBy_IsNull()
        {
            var parsed = NpcReactionParser.Parse("Npc_Test",
            [
                new NpcReactionData { AbilityId = "Ability_A", Trigger = "DamagedByAttack", Chance = 0.5f, MaxPerTurn = 1 }
            ]);

            Assert.IsNull(parsed[0].BlockedByFinalDeathOf);
        }

        [TestMethod]
        public void Parse_RealNpcJson_TwinsCarryMirroredReactions()
        {
            string json = File.ReadAllText(Path.Combine(FindSharedData(), "Npc", "Npc.json"));
            var npcs = Newtonsoft.Json.JsonConvert.DeserializeObject<NpcsData>(json)!.Npcs;

            var digri = npcs.Single(npc => npc.Id == "Npc_Boss_Digri");
            var zigri = npcs.Single(npc => npc.Id == "Npc_Boss_Zigri");

            var digriReactions = NpcReactionParser.Parse(digri.Id, digri.Reactions);
            var zigriReactions = NpcReactionParser.Parse(zigri.Id, zigri.Reactions);

            Assert.AreEqual(1, digriReactions.Count, "Digri's reactions section must parse");
            Assert.AreEqual("Ability_Twin_Assist_Attack", digriReactions[0].AbilityId);
            Assert.AreEqual("Npc_Boss_Zigri", digriReactions[0].BlockedByFinalDeathOf);
            Assert.AreEqual(3, digriReactions[0].MaxPerTurn);

            Assert.AreEqual(1, zigriReactions.Count, "Zigri's reactions section must parse");
            Assert.AreEqual("Ability_Twin_Assist_Shield", zigriReactions[0].AbilityId);
            Assert.AreEqual("Npc_Boss_Digri", zigriReactions[0].BlockedByFinalDeathOf);
        }

        // ---------- the driver ----------

        [TestMethod]
        public void Driver_CapsFiringsPerTurn_AndResetsOnNewTurn()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, null));

            for (int i = 0; i < 5; i++) harness.TakeAttackDamage();
            harness.VerifyExecutions(3);

            harness.StartNewTurn();
            for (int i = 0; i < 5; i++) harness.TakeAttackDamage();
            harness.VerifyExecutions(6);
        }

        [TestMethod]
        public void Driver_FinalDeathFact_BlocksTheReaction()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, "Npc_Boss_Zigri"));
            harness.Facts.SetFact(FactKeys.NpcFinalDeath("Npc_Boss_Zigri"));

            harness.TakeAttackDamage();

            harness.VerifyExecutions(0);
        }

        [TestMethod]
        public void Driver_WithoutTheFact_BlockedByReactionStillFires()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, "Npc_Boss_Zigri"));

            harness.TakeAttackDamage();

            harness.VerifyExecutions(1);
        }

        [TestMethod]
        public void Driver_NonAttackDamage_DoesNotTrigger()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, null));

            harness.TakeDamage(DamageCause.Ability);
            harness.TakeDamage(DamageCause.Effect);

            harness.VerifyExecutions(0);
        }

        [TestMethod]
        public void Driver_DeadOwner_DoesNotReact()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, null));
            harness.SetOwnerAlive(false);

            harness.TakeAttackDamage();

            harness.VerifyExecutions(0);
        }

        [TestMethod]
        public void Driver_FinishedBattle_DoesNotReact()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, null));
            harness.BattleActive = false;

            harness.TakeAttackDamage();

            harness.VerifyExecutions(0);
        }

        [TestMethod]
        public void Driver_FailedChanceRoll_DoesNotConsumeTheCap()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 0.5f, 1, null));

            harness.NextRoll = 0.9f; // above the chance — no fire
            harness.TakeAttackDamage();
            harness.VerifyExecutions(0);

            harness.NextRoll = 0.1f; // the cap must still be available
            harness.TakeAttackDamage();
            harness.VerifyExecutions(1);
        }

        [TestMethod]
        public void Driver_Dispose_DetachesFromTheOwner()
        {
            var harness = new Harness(new NpcReactionConfig("Ability_X", ReactionTrigger.DamagedByAttack, 1f, 3, null));

            harness.Dispose();
            harness.TakeAttackDamage();

            harness.VerifyExecutions(0);
        }

        private static string FindSharedData()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "SharedData");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("SharedData was not found above the test host directory");
        }

        /// <summary>One boss + one reaction wired through a real combat bus and a real battle bus.</summary>
        private sealed class Harness
        {
            private readonly Mock<IFightableNpc> _owner = new();
            private readonly Mock<IFightable> _attacker = new();
            private readonly Mock<IAbility> _ability = new();
            private readonly CombatEventBus _combatEvents = new();
            private readonly BattleEventBus _battleBus = new();
            private readonly NpcReactionsDriver _driver;
            private bool _ownerAlive = true;

            public WorldFactsService Facts { get; } = new();
            public bool BattleActive { get; set; } = true;
            public float NextRoll { get; set; }

            public Harness(NpcReactionConfig reaction)
            {
                _owner.Setup(npc => npc.CombatEvents).Returns(_combatEvents);
                _owner.Setup(npc => npc.IsAlive).Returns(() => _ownerAlive);
                _owner.Setup(npc => npc.Reactions).Returns([reaction]);
                _owner.Setup(npc => npc.Id).Returns("Npc_Test_Boss");

                _ability.Setup(a => a.Execute(It.IsAny<List<IFightable>>(), It.IsAny<IBattleField>()))
                    .Returns(Task.CompletedTask);

                var abilities = new Mock<IAbilityProvider>();
                abilities.Setup(p => p.CreateAbility(reaction.AbilityId)).Returns(_ability.Object);

                var rnd = new Mock<IRandomNumberGenerator>();
                rnd.Setup(r => r.RandFloat()).Returns(() => NextRoll);

                _driver = new NpcReactionsDriver(
                    Mock.Of<IBattleField>(), abilities.Object, Facts, rnd.Object, () => BattleActive, _battleBus);
                _driver.TryAttach(_owner.Object);
            }

            public void SetOwnerAlive(bool alive) => _ownerAlive = alive;

            public void TakeAttackDamage() => TakeDamage(DamageCause.Attack);

            public void TakeDamage(DamageCause cause)
            {
                var context = new DamageContext { Source = _attacker.Object, Cause = cause };
                _combatEvents.Publish(new DamageTakenEvent(context, _owner.Object, new VitalsSnapshot(1f, 1f, 0f, 0f, 0f, 0f)));
            }

            public void StartNewTurn() => _battleBus.Publish(new TurnStartEvent(_attacker.Object));

            public void Dispose() => _driver.Dispose();

            public void VerifyExecutions(int count) =>
                _ability.Verify(a => a.Execute(It.IsAny<List<IFightable>>(), It.IsAny<IBattleField>()), Times.Exactly(count));
        }
    }
}
