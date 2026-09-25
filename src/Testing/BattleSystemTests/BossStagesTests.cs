namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Context;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The boss stage stack (Rat King): strict all-or-nothing parse of the "stages" section and the
    /// controller's behavior — crossing the threshold grants damage immunity at once and publishes
    /// the transition event, the transformation executes on the next battle-bus TurnStartEvent
    /// (strip effects, apply stage, full restore), the transition is one-shot, the rage burst is
    /// one-time, and stage attack effects ride landed attacks. Pure C# + Moq, no Godot runtime.
    /// </summary>
    [TestClass]
    public class BossStagesTests
    {
        // ---------- parsing ----------

        [TestMethod]
        public void Parse_JsonString_ProducesStageConfigs()
        {
            const string json = """
            {
                "npcs": [
                    {
                        "id": "Npc_Test_Boss",
                        "stages": [
                            {
                                "parameterMultiplier": 0.15,
                                "abilities": [],
                                "attackEffects": [
                                    { "effect": "Poison", "chance": 0.35, "damagePercent": 0.75, "duration": 3, "maxStacks": 999 }
                                ],
                                "nextStageAtHealthPercent": 0.35
                            },
                            {
                                "parameterMultiplier": 1.0,
                                "abilities": [ "Ability_Increasing_Pressure", "Ability_Poison_Explosion" ],
                                "attackEffects": [
                                    { "effect": "Poison", "chance": 1.0, "damagePercent": 0.75, "duration": 3, "maxStacks": 999 },
                                    { "effect": "WitheringCurse", "chance": 0.15, "damagePercent": 0.15, "duration": 3, "maxStacks": 3 }
                                ],
                                "rageAtHealthPercent": 0.3,
                                "rageBonus": 0.25
                            }
                        ]
                    }
                ]
            }
            """;

            var npc = Newtonsoft.Json.JsonConvert.DeserializeObject<NpcsData>(json)!.Npcs.Single();
            var stages = NpcStageParser.Parse(npc.Id, npc.Stages);

            Assert.AreEqual(2, stages.Count);

            var first = stages[0];
            Assert.AreEqual(0.15f, first.ParameterMultiplier);
            Assert.AreEqual(0, first.Abilities.Count);
            Assert.AreEqual(0.35f, first.NextStageAtHealthPercent);
            Assert.IsNull(first.RageAtHealthPercent);
            Assert.AreEqual(1, first.AttackEffects.Count);
            var poison = first.AttackEffects[0];
            Assert.AreEqual(StageAttackEffectKind.Poison, poison.Effect);
            Assert.AreEqual(0.35f, poison.Chance);
            Assert.AreEqual(0.75f, poison.DamagePercent);
            Assert.AreEqual(3, poison.Duration);
            Assert.AreEqual(999, poison.MaxStacks);

            var second = stages[1];
            Assert.AreEqual(1.0f, second.ParameterMultiplier);
            CollectionAssert.AreEqual(
                new[] { "Ability_Increasing_Pressure", "Ability_Poison_Explosion" },
                second.Abilities.ToArray());
            Assert.IsNull(second.NextStageAtHealthPercent);
            Assert.AreEqual(0.3f, second.RageAtHealthPercent);
            Assert.AreEqual(0.25f, second.RageBonus);
            Assert.AreEqual(StageAttackEffectKind.WitheringCurse, second.AttackEffects[1].Effect);
        }

        [TestMethod]
        public void Parse_BrokenStage_DropsTheWholeSection()
        {
            var parsed = NpcStageParser.Parse("Npc_Test",
            [
                new NpcStageData { ParameterMultiplier = 0.5f },
                new NpcStageData
                {
                    ParameterMultiplier = 1f,
                    AttackEffects = [new NpcStageAttackEffectData { Effect = "NoSuchEffect", Chance = 0.5f, DamagePercent = 0.5f, Duration = 3, MaxStacks = 1 }]
                }
            ]);

            Assert.AreEqual(0, parsed.Count, "one broken stage must drop the whole section — skipping would shift transition indices");
        }

        [TestMethod]
        public void Parse_RageWithoutBonus_IsBroken()
        {
            var parsed = NpcStageParser.Parse("Npc_Test",
            [
                new NpcStageData { ParameterMultiplier = 1f, RageAtHealthPercent = 0.3f }
            ]);

            Assert.AreEqual(0, parsed.Count);
        }

        [TestMethod]
        public void Parse_RealNpcJson_RatKingCarriesTwoStages()
        {
            string json = File.ReadAllText(Path.Combine(FindSharedData(), "Npc", "Npc.json"));
            var npcs = Newtonsoft.Json.JsonConvert.DeserializeObject<NpcsData>(json)!.Npcs;

            var ratKing = npcs.Single(npc => npc.Id == "Npc_Boss_Rat_King");
            var stages = NpcStageParser.Parse(ratKing.Id, ratKing.Stages);

            Assert.AreEqual(2, stages.Count, "Rat King's stages section must parse");
            Assert.AreEqual(0.15f, stages[0].ParameterMultiplier);
            Assert.AreEqual(0.35f, stages[0].NextStageAtHealthPercent);
            Assert.AreEqual(0, stages[0].Abilities.Count, "stage 1 fights with basic attacks only");
            Assert.AreEqual(1.0f, stages[1].ParameterMultiplier);
            Assert.AreEqual(0.3f, stages[1].RageAtHealthPercent);
            Assert.AreEqual(0.25f, stages[1].RageBonus);
            Assert.AreEqual(2, stages[1].AttackEffects.Count);
        }

        [TestMethod]
        public void Parse_RealNpcJson_BonePackLeaderCarriesDeepWoundsPassive()
        {
            string json = File.ReadAllText(Path.Combine(FindSharedData(), "Npc", "Npc.json"));
            var npcs = Newtonsoft.Json.JsonConvert.DeserializeObject<NpcsData>(json)!.Npcs;

            var leader = npcs.Single(npc => npc.Id == "Npc_Boss_Bone_Pack_Leader");
            var passive = leader.Passives.Single();

            // Deep Wounds = the generic Bleeding passive at the boss's numbers (120% / 3 turns).
            Assert.AreEqual("Passive_Skill_Bleeding", passive.Id);
            Assert.AreEqual(1.2f, passive.Properties["percentFromDamage"]);
            Assert.AreEqual(3f, passive.Properties["duration"]);
            Assert.AreEqual(999f, passive.Properties["maxStacks"]);
        }

        // ---------- the controller: transition ----------

        [TestMethod]
        public void Controller_CrossingThreshold_GrantsImmunityAndPublishesEvent()
        {
            var harness = new Harness();

            harness.Health = 300f; // below 35% of 1000
            harness.TakeDamage();

            var started = harness.Published.OfType<BossStageTransitionStartedEvent>().Single();
            Assert.AreEqual(1, started.NextStage);
            Assert.AreEqual(0f, harness.RunDamagePipeline(100f), "all incoming damage must be zeroed while immune");
            harness.VerifyAppliedStages(); // the transformation itself waits for the next turn start
        }

        [TestMethod]
        public void Controller_AboveThreshold_DoesNothing()
        {
            var harness = new Harness();

            harness.Health = 400f; // above 35% of 1000
            harness.TakeDamage();

            Assert.AreEqual(0, harness.Published.OfType<BossStageTransitionStartedEvent>().Count());
            Assert.AreEqual(100f, harness.RunDamagePipeline(100f));
        }

        [TestMethod]
        public void Controller_Transformation_ExecutesOnNextTurnStart()
        {
            var harness = new Harness();
            harness.Health = 300f;
            harness.TakeDamage();

            harness.StartNewTurn();

            harness.VerifyAppliedStages(1);
            Assert.AreEqual(0, harness.Effects.Effects.Count, "ALL effects (immunity included) must be stripped");
            Assert.AreEqual(1000f, harness.Health, "health must be fully restored");
            Assert.AreEqual(100f, harness.Mana, "mana must be fully restored");
            Assert.AreEqual(50f, harness.Barrier, "barrier must be fully restored");
            Assert.AreEqual(100f, harness.RunDamagePipeline(100f), "immunity must not survive the transformation");
            var changed = harness.Published.OfType<BossStageChangedEvent>().Single();
            Assert.AreEqual(1, changed.Stage);
        }

        [TestMethod]
        public void Controller_Transition_IsOneShot()
        {
            var harness = new Harness();
            harness.Health = 300f;
            harness.TakeDamage();
            harness.TakeDamage(); // second hit while pending must not double-arm
            harness.StartNewTurn();

            harness.Health = 100f; // deep below the old threshold, now in the final stage
            harness.TakeDamage();
            harness.StartNewTurn();

            Assert.AreEqual(1, harness.Published.OfType<BossStageTransitionStartedEvent>().Count());
            Assert.AreEqual(1, harness.Published.OfType<BossStageChangedEvent>().Count());
            harness.VerifyAppliedStages(1);
        }

        // ---------- the controller: rage ----------

        [TestMethod]
        public void Controller_Rage_TriggersOnceAtItsThreshold()
        {
            var harness = new Harness();
            harness.TransformToFinalStage();

            harness.Health = 250f; // below 30% of 1000
            harness.TakeDamage();
            harness.TakeDamage();

            Assert.AreEqual(1, harness.ParameterModifiers.GetModifiers(EntityParameter.PhysicalDamage).Count);
            Assert.AreEqual(1, harness.ParameterModifiers.GetModifiers(EntityParameter.CriticalChance).Count);
            Assert.AreEqual(1, harness.ParameterModifiers.GetModifiers(EntityParameter.AdditionalHitChance).Count);
        }

        [TestMethod]
        public void Controller_Rage_DoesNotFireInStageOne()
        {
            var harness = new Harness();

            harness.Health = 250f;
            harness.TakeDamage(); // arms the transition, but stage 1 has no rage

            Assert.AreEqual(0, harness.ParameterModifiers.GetModifiers(EntityParameter.PhysicalDamage).Count);
        }

        [TestMethod]
        public void Controller_Dispose_RemovesRageWithTheBattle()
        {
            var harness = new Harness();
            harness.TransformToFinalStage();
            harness.Health = 250f;
            harness.TakeDamage();

            harness.Dispose();

            Assert.AreEqual(0, harness.ParameterModifiers.GetModifiers(EntityParameter.PhysicalDamage).Count);
        }

        // ---------- the controller: stage attack effects ----------

        [TestMethod]
        public void Controller_StageAttackEffects_RollTheChancePerLandedAttack()
        {
            var harness = new Harness();
            var victim = new Victim();

            harness.NextRoll = 0.9f; // above the 35% chance
            harness.LandAttack(victim);
            Assert.AreEqual(0, victim.Effects.Effects.Count);

            harness.NextRoll = 0.1f; // below the chance
            harness.LandAttack(victim);
            var applied = victim.Effects.Effects.Single();
            // Identity carries the damage kind: each DoT is its own slot, stack bucket and description.
            Assert.AreEqual("Effect_Damage_Over_Turn_Poison", applied.Id);
            Assert.AreEqual(StatusEffects.Poison, applied.Status);
        }

        [TestMethod]
        public void Controller_StageAttackEffects_IgnoreMissedAttacks()
        {
            var harness = new Harness();
            var victim = new Victim();

            harness.NextRoll = 0f;
            harness.LandAttack(victim, AttackResults.Evaded);

            Assert.AreEqual(0, victim.Effects.Effects.Count);
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

        private static List<NpcStageConfig> RatKingLikeStages() =>
        [
            new(0.15f,
                [],
                [new NpcStageAttackEffectConfig(StageAttackEffectKind.Poison, 0.35f, 0.75f, 3, 999)],
                NextStageAtHealthPercent: 0.35f,
                RageAtHealthPercent: null,
                RageBonus: 0f),
            new(1.0f,
                ["Ability_Increasing_Pressure", "Ability_Poison_Explosion"],
                [new NpcStageAttackEffectConfig(StageAttackEffectKind.Poison, 1f, 0.75f, 3, 999)],
                NextStageAtHealthPercent: null,
                RageAtHealthPercent: 0.3f,
                RageBonus: 0.25f),
        ];

        /// <summary>A fightable stub the stage effects can actually land on: real effects/pipelines.</summary>
        private sealed class Victim
        {
            private readonly Mock<IFightable> _mock = new();

            public EffectsComponent Effects { get; }
            public IFightable Object => _mock.Object;

            public Victim()
            {
                Effects = new EffectsComponent(_mock.Object);
                _mock.Setup(v => v.InstanceId).Returns(Guid.NewGuid().ToString());
                _mock.Setup(v => v.Effects).Returns(Effects);
                _mock.Setup(v => v.ModifierHandler).Returns(new ModifierHandlerComponent());
                _mock.Setup(v => v.CombatEvents).Returns(new CombatEventBus());
                _mock.Setup(v => v.Parameters).Returns(Mock.Of<IEntityParametersComponent>());
                _mock.Setup(v => v.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
            }
        }

        /// <summary>One staged boss wired through a real combat bus, real battle bus and real
        /// effect/modifier/parameter-modifier components; only the entity itself is a mock.</summary>
        private sealed class Harness
        {
            private readonly Mock<IFightableNpc> _boss = new();
            private readonly Mock<IFightable> _attacker = new();
            private readonly CombatEventBus _combatEvents = new();
            private readonly BattleEventBus _battleBus = new();
            private readonly ModifierHandlerComponent _modifierHandler = new();
            private readonly BossStagesController _controller;
            private readonly List<int> _appliedStages = [];
            private int _stageIndex;

            public List<ICombatEvent> Published { get; } = [];
            public EffectsComponent Effects { get; }
            public ParameterModifiersComponent ParameterModifiers { get; } = new();
            public bool BattleActive { get; set; } = true;
            public float NextRoll { get; set; }
            public float Health { get; set; } = 1000f;
            public float Mana { get; set; } = 40f;
            public float Barrier { get; set; } = 10f;

            public Harness()
            {
                Effects = new EffectsComponent(_boss.Object);

                var parameters = new Mock<IEntityParametersComponent>();
                parameters.Setup(p => p.MaxHealth).Returns(1000f);
                parameters.Setup(p => p.MaxMana).Returns(100f);
                parameters.Setup(p => p.MaxBarrier).Returns(50f);

                _boss.Setup(b => b.Id).Returns("Npc_Test_Boss");
                _boss.Setup(b => b.InstanceId).Returns(Guid.NewGuid().ToString());
                _boss.Setup(b => b.IsAlive).Returns(true);
                _boss.Setup(b => b.Stages).Returns(RatKingLikeStages());
                _boss.Setup(b => b.CurrentStageIndex).Returns(() => _stageIndex);
                _boss.Setup(b => b.ApplyStage(It.IsAny<int>())).Callback<int>(stage =>
                {
                    _stageIndex = stage;
                    _appliedStages.Add(stage);
                });
                _boss.Setup(b => b.CombatEvents).Returns(_combatEvents);
                _boss.Setup(b => b.Effects).Returns(Effects);
                _boss.Setup(b => b.ModifierHandler).Returns(_modifierHandler);
                _boss.Setup(b => b.ParameterModifiers).Returns(ParameterModifiers);
                _boss.Setup(b => b.Parameters).Returns(parameters.Object);
                _boss.Setup(b => b.TryApplyStatusEffect(It.IsAny<StatusEffects>())).Returns(false);
                _boss.SetupSet(b => b.CurrentHealth = It.IsAny<float>()).Callback<float>(value => Health = value);
                _boss.SetupGet(b => b.CurrentHealth).Returns(() => Health);
                _boss.SetupSet(b => b.CurrentMana = It.IsAny<float>()).Callback<float>(value => Mana = value);
                _boss.SetupGet(b => b.CurrentMana).Returns(() => Mana);
                _boss.SetupSet(b => b.CurrentBarrier = It.IsAny<float>()).Callback<float>(value => Barrier = value);
                _boss.SetupGet(b => b.CurrentBarrier).Returns(() => Barrier);

                _combatEvents.SubscribeAll(Published.Add);

                var rnd = new Mock<IRandomNumberGenerator>();
                rnd.Setup(r => r.RandFloat()).Returns(() => NextRoll);

                _controller = new BossStagesController(rnd.Object, () => BattleActive, _battleBus);
                _controller.TryAttach(_boss.Object);
            }

            public void TakeDamage()
            {
                var context = new DamageContext { Source = _attacker.Object, Cause = DamageCause.Attack };
                _combatEvents.Publish(new DamageTakenEvent(context, _boss.Object, new VitalsSnapshot(Health, 1000f, Barrier, 50f, Mana, 100f)));
            }

            public void StartNewTurn() => _battleBus.Publish(new TurnStartEvent(_boss.Object));

            /// <summary>Fast-forwards the boss into its final stage through the normal transition.</summary>
            public void TransformToFinalStage()
            {
                Health = 300f;
                TakeDamage();
                StartNewTurn();
            }

            public void LandAttack(Victim victim, AttackResults result = AttackResults.Succeed)
            {
                var context = new Mock<IAttackContext>();
                context.Setup(c => c.Result).Returns(result);
                context.Setup(c => c.Target).Returns(victim.Object);
                context.Setup(c => c.FinalDamage).Returns(DamageSnapshot.Of(DamageType.Physical, 100f));
                context.Setup(c => c.IsCritical).Returns(false);
                _combatEvents.Publish(new AfterAttackEvent(context.Object));
            }

            /// <summary>Runs a fresh physical hit through the boss's damage pipeline and returns the total left.</summary>
            public float RunDamagePipeline(float damage)
            {
                var context = new DamageContext { Source = _attacker.Object, Cause = DamageCause.Attack };
                context.Add(DamageType.Physical, damage);
                _modifierHandler.Apply(context);
                return context.TotalDamage;
            }

            public void Dispose() => _controller.Dispose();

            public void VerifyAppliedStages(params int[] stages) =>
                CollectionAssert.AreEqual(stages, _appliedStages);
        }
    }
}
