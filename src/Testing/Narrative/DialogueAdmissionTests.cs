namespace LastBreathTest.Narrative
{
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Services;
    using Moq;

    [TestClass]
    public sealed class DialogueAdmissionTests
    {
        private sealed class EffectfulCondition : INarrativeCondition
        {
            public int Calls;
            public bool IsMet(NarrativeContext context) { Calls++; return true; }
        }

        private static DialogueNode Node(string id, INarrativeCondition? optionCondition = null) =>
            new(id, [new(DialogueSpeaker.Npc, id)], [],
                [new("leave", "Leave", optionCondition == null ? [] : [optionCondition], [], [], null, false, false, null)]);

        private static DialogueService Service(DialogueDefinition definition, WorldFactsService facts, IInfluenceMastery influence)
        {
            var provider = new Mock<IDialogueProvider>();
            provider.Setup(x => x.Get(definition.NpcId)).Returns(definition);
            return new(provider.Object, facts, influence, new Mock<IRandomNumberGenerator>(MockBehavior.Strict).Object, new GameEventBus());
        }

        [TestMethod]
        public void RepeatedAdmission_DoesNotEnterOrEvaluateOptions_AndStartUsesTheSameEntry()
        {
            var facts = new WorldFactsService();
            var influence = new Mock<IInfluenceMastery>();
            var effectful = new EffectfulCondition();
            var high = Node("high", effectful);
            var low = Node("low");
            var definition = new DialogueDefinition("npc",
                [new(10, [new FactCondition(facts, "ready", 1)], "high"), new(0, [], "low")],
                new Dictionary<string, DialogueNode> { ["high"] = high, ["low"] = low });
            var service = Service(definition, facts, influence.Object);
            for (int i = 0; i < 100; i++) Assert.IsTrue(service.CanStart("npc", "instance", Fractions.Human));
            Assert.AreEqual(0, facts.Snapshot.Count);
            Assert.AreEqual(0, effectful.Calls);
            Assert.IsFalse(service.IsActive);
            influence.VerifyNoOtherCalls();
            facts.SetFact("ready");
            Assert.IsTrue(service.Start("npc", "instance", Fractions.Human));
            Assert.AreEqual("high", service.Current!.Lines[0].TextKey);
            Assert.AreEqual(1, effectful.Calls);
            var current = service.Current;
            Assert.IsFalse(service.CanStart("missing", null, Fractions.Human));
            Assert.AreSame(current, service.Current);
            Assert.IsTrue(facts.IsSet(FactKeys.NpcTalked("npc")));
        }

        [TestMethod]
        public void UnsafeNestedEntries_NeverExecuteEvenBehindAShortCircuit()
        {
            var effectful = new EffectfulCondition();
            INarrativeCondition[] unsafeConditions = [
                effectful,
                new NotCondition(effectful),
                new AnyOfCondition([new AllOfCondition([]), effectful]),
                new AllOfCondition([new FactCondition(new WorldFactsService(), "absent", 1), effectful]),
                new CanAcceptQuestCondition(() => throw new InvalidOperationException("Must not resolve the quest log"), "quest")
            ];
            foreach (var condition in unsafeConditions)
            {
                var node = Node("start");
                var service = Service(new("npc", [new(0, [condition], "start")],
                    new Dictionary<string, DialogueNode> { ["start"] = node }),
                    new WorldFactsService(), Mock.Of<IInfluenceMastery>());
                Assert.IsFalse(service.CanStart("npc", null, Fractions.Human));
                Assert.IsFalse(service.Start("npc", null, Fractions.Human));
            }
            Assert.AreEqual(0, effectful.Calls);
        }

        [TestMethod]
        public void NoMatchingRule_OpensNothingAndRewardsNothing()
        {
            var facts = new WorldFactsService();
            var node = Node("start");
            var influence = new Mock<IInfluenceMastery>(MockBehavior.Strict);
            var service = Service(new("npc", [new(0, [new FactCondition(facts, "ready", 1)], "start")],
                new Dictionary<string, DialogueNode> { ["start"] = node }), facts, influence.Object);
            Assert.IsFalse(service.CanStart("npc", null, Fractions.Human));
            Assert.IsFalse(service.Start("npc", null, Fractions.Human));
            Assert.AreEqual(0, facts.Snapshot.Count);
            Assert.IsNull(service.Current);
        }

        [TestMethod]
        public void LoaderRejectsEffectfulEntriesButKeepsEffectfulOptions()
        {
            var parser = new NarrativeConditionParser([
                new QuestOfferRollConditionFactory(Mock.Of<IWorldFactsService>(), Mock.Of<IInfluenceMastery>(),
                    Mock.Of<Core.Ai.World.Time.IWorldClock>(), Mock.Of<IRandomNumberGenerator>(), () => Mock.Of<IQuestProvider>()),
                new CanAcceptQuestConditionFactory(() => throw new InvalidOperationException("Must not resolve")),
                new AnyOfConditionFactory(), new NotConditionFactory()
            ]);
            var provider = new DialogueProvider(parser, new NarrativeActionParser([]));
            string roll = """{"type":"QuestOfferRoll","questId":"q"}""";
            string indirect = """{"type":"CanAcceptQuest","questId":"q"}""";
            string nested = """{"type":"Not","condition":{"type":"AnyOf","conditions":[{"type":"QuestOfferRoll","questId":"q"}]}}""";
            foreach (string clause in new[] { roll, indirect, nested })
            {
                string json = """{"dialogues":[{"npcId":"npc","entryRules":[{"conditions":[CLAUSE],"node":"start"}],"nodes":[{"id":"start","options":[{"id":"exit","key":"Exit"}]}]}]}""".Replace("CLAUSE", clause);
                provider.Apply("Dialogues", new GameDataFile("probe.json", json));
                Assert.IsNull(provider.Get("npc"));
            }
            string valid = """{"dialogues":[{"npcId":"npc","entryRules":[{"node":"start"}],"nodes":[{"id":"start","options":[{"id":"exit","key":"Exit","visibleConditions":[CLAUSE]}]}]}]}""".Replace("CLAUSE", roll);
            provider.Apply("Dialogues", new GameDataFile("probe.json", valid));
            Assert.IsNotNull(provider.Get("npc"));
        }
    }
}
