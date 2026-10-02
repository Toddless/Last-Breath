namespace LastBreathTest.Narrative
{
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Items;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The second half of "a repeat pays what repeats, artefacts do not double": the data guard keeps a
    /// unique reward off a repeatable quest, and this keeps the hand-out itself honest even if one gets
    /// through. A repeatable quest turned in twice pays its ordinary items, influence and actions both
    /// times and mints its one-of-a-kind reward once — and the note saying so belongs to the quest, so it
    /// survives a fresh attempt, a decline and a save, and dies with the playthrough.
    /// </summary>
    [TestClass]
    public class QuestUniqueRewardTests
    {
        private const string QuestId = "Quest_Repeatable_Trial";
        private const string RewardlessQuestId = "Quest_Bare_Thanks";
        private const string BuriedQuestId = "Quest_Cut_From_The_Data";
        private const string UniqueRewardId = "Ornament_Tier_1";
        private const string OrdinaryRewardId = "Coal";
        private const int InfluenceReward = 40;
        private const int LedgerlessSaveVersion = 1;
        private const int CurrentSaveVersion = 3;

        private QuestCatalog _quests = null!;
        private WorldFactsService _facts = null!;
        private GameEventBus _events = null!;
        private Mock<Core.Inventory.IInventory> _inventory = null!;
        private Mock<IInfluenceMastery> _influence = null!;
        private Mock<IItemMinter> _minter = null!;
        private Mock<IUniqueItemQuery> _uniqueItems = null!;
        private List<string> _minted = null!;
        private CountingAction _rewardAction = null!;
        private QuestLogService _questLog = null!;
        private int _capacity;

        [TestInitialize]
        public void Setup()
        {
            _quests = new QuestCatalog();
            _facts = new WorldFactsService();
            _events = new GameEventBus();
            _minted = [];
            _rewardAction = new CountingAction();
            _capacity = 100;

            _inventory = new Mock<Core.Inventory.IInventory>();
            _inventory.Setup(bag => bag.GetAvailableCapacity()).Returns(() => _capacity);
            _inventory.Setup(bag => bag.TryAddItem(It.IsAny<IItem>(), It.IsAny<int>()))
                .Callback((IItem item, int _) => _minted.Add(item.Id))
                .Returns(true);

            _minter = new Mock<IItemMinter>();
            _minter.Setup(mint => mint.MintItem(It.IsAny<string>(), It.IsAny<Rarity?>()))
                .Returns((string id, Rarity? _) => Mock.Of<IItem>(item => item.Id == id));

            _influence = new Mock<IInfluenceMastery>();
            _uniqueItems = new Mock<IUniqueItemQuery>();
            _uniqueItems.Setup(query => query.IsUnique(UniqueRewardId)).Returns(true);

            _quests.Add(RepeatableQuest());
            _quests.Add(RewardlessQuest());
            _questLog = CreateQuestLog();
        }

        [TestMethod]
        public void RepeatTurnIn_PaysTheOrdinaryRewardsAgain()
        {
            TurnInOnce();
            TurnInOnce();

            Assert.AreEqual(2, _minted.Count(id => id == OrdinaryRewardId), "the repeat must pay its ordinary reward again");
            Assert.AreEqual(2, _rewardAction.Executions, "reward actions (tree points among them) are repeatable pay");
            _influence.Verify(mastery => mastery.AddExperience(InfluenceReward), Times.Exactly(2));
        }

        [TestMethod]
        public void RepeatTurnIn_MintsTheOneOfAKindRewardOnlyOnce()
        {
            TurnInOnce();
            TurnInOnce();

            Assert.AreEqual(1, _minted.Count(id => id == UniqueRewardId),
                "the artefact was minted a second time — a repeatable quest must not duplicate what the world has one of");
        }

        /// <summary>The ledger's claim is "the player holds it". A bag that refused the hand-over must not
        /// let that claim be written, or the artefact would be lost to a note saying it was paid.</summary>
        [TestMethod]
        public void ABagThatRefusedTheArtefact_WritesNothingDown()
        {
            _inventory.Setup(bag => bag.TryAddItem(It.IsAny<IItem>(), It.IsAny<int>())).Returns(false);

            TurnInOnce();

            Assert.AreEqual(0, _questLog.GetState(QuestId)!.GrantedUniqueRewards.Count,
                "an artefact the bag turned away was written down as handed over");
        }

        /// <summary>The gate and the payout read one list: a repeat that owes only the ordinary item must
        /// not demand bag room for the artefact it is not going to mint.</summary>
        [TestMethod]
        public void RepeatTurnIn_AsksTheBagOnlyForWhatItStillOwes()
        {
            TurnInOnce();
            Accept();

            _capacity = 1;
            Assert.IsTrue(_questLog.CanTurnIn(QuestId), "one free slot is all the repeat still owes");

            _capacity = 0;
            Assert.IsFalse(_questLog.CanTurnIn(QuestId), "a full bag must still block the ordinary reward");
        }

        [TestMethod]
        public void ANewAttempt_InheritsTheLedgerOfArtefactsAlreadyPaid()
        {
            TurnInOnce();

            Accept();

            CollectionAssert.Contains(_questLog.GetState(QuestId)!.GrantedUniqueRewards.ToList(), UniqueRewardId,
                "accepting the quest again wiped the note of what it already handed out");
        }

        [TestMethod]
        public void DecliningTheRepeatedOffer_KeepsTheLedger()
        {
            TurnInOnce();

            _questLog.Decline(QuestId, NarrativeContext.Empty);
            _minted.Clear();
            TurnInOnce();

            Assert.AreEqual(0, _minted.Count(id => id == UniqueRewardId),
                "turning the repeated offer down rebuilt the state and lost the note with it");
        }

        [TestMethod]
        public void TheLedger_SurvivesCaptureAndRestore()
        {
            TurnInOnce();
            JToken saved = new QuestLogSaveParticipant(_questLog, _quests).Capture();

            var reloaded = CreateQuestLog();
            new QuestLogSaveParticipant(reloaded, _quests).Restore(saved, CurrentSaveVersion);

            CollectionAssert.Contains(reloaded.GetState(QuestId)!.GrantedUniqueRewards.ToList(), UniqueRewardId);

            _minted.Clear();
            TurnInOnce(reloaded);

            Assert.AreEqual(0, _minted.Count(id => id == UniqueRewardId), "the loaded game minted the artefact a second time");
            Assert.AreEqual(1, _minted.Count(id => id == OrdinaryRewardId), "the loaded game must still pay the repeatable reward");
        }

        [TestMethod]
        public void ASaveWrittenBeforeTheLedger_ReadsAsNothingHandedOut()
        {
            new QuestLogSaveParticipant(_questLog, _quests).Restore(JToken.Parse(LedgerlessSaveJson), LedgerlessSaveVersion);

            var state = _questLog.GetState(QuestId);

            Assert.IsNotNull(state, "an old save must still restore its quests");
            Assert.AreEqual(0, state!.GrantedUniqueRewards.Count, "an old file says nothing about artefacts, not something wrong");
        }

        [TestMethod]
        public void ASavedQuestCutFromTheData_IsDropped()
        {
            new QuestLogSaveParticipant(_questLog, _quests).Restore(JToken.Parse(BuriedQuestSaveJson), CurrentSaveVersion);

            Assert.IsNull(_questLog.GetState(BuriedQuestId), "a quest a patch removed must not restore as a live state");
        }

        [TestMethod]
        public void ANewGame_InheritsNoLedger()
        {
            TurnInOnce();

            _questLog.ResetSession();
            _minted.Clear();
            TurnInOnce();

            Assert.AreEqual(1, _minted.Count(id => id == UniqueRewardId), "a fresh playthrough must be able to earn the artefact");
        }

        [TestMethod]
        public void AQuestRewardingNoItems_TurnsInWithoutMintingAnything()
        {
            Assert.IsTrue(_questLog.Accept(RewardlessQuestId, NarrativeContext.Empty));
            Assert.IsTrue(_questLog.TurnIn(RewardlessQuestId, NarrativeContext.Empty), "an empty reward list is not a reason to refuse a turn-in");

            Assert.AreEqual(0, _minted.Count);
        }

        /// <summary>The rule itself, read off the catalogs: a template rarity generation never rerolls, or
        /// membership of the ornament catalog. Everything else is an ordinary stack.</summary>
        [TestMethod]
        public void UniqueItemQuery_CountsFixedTemplateRaritiesAndOrnaments()
        {
            var blueprints = new Mock<IEquipBlueprintProvider>();
            blueprints.Setup(provider => provider.GetBlueprint(It.IsAny<string>())).Returns((EquipItemBlueprint?)null);
            blueprints.Setup(provider => provider.GetBlueprint("Weapon_Unique")).Returns(Blueprint("Weapon_Unique", Rarity.Unique));
            blueprints.Setup(provider => provider.GetBlueprint("Weapon_Mythic")).Returns(Blueprint("Weapon_Mythic", Rarity.Mythic));
            blueprints.Setup(provider => provider.GetBlueprint("Weapon_Legendary")).Returns(Blueprint("Weapon_Legendary", Rarity.Legendary));
            var ornaments = new OrnamentCatalog();
            ornaments.Apply(DataCatalog.Ornaments, new GameDataFile("test.json", OrnamentCatalogJson));

            var query = new UniqueItemQuery(blueprints.Object, ornaments);

            Assert.IsTrue(query.IsUnique("Weapon_Unique"));
            Assert.IsTrue(query.IsUnique("Weapon_Mythic"));
            Assert.IsTrue(query.IsUnique("Ornament_Test"), "every record of the ornament catalog is one of a kind");
            Assert.IsFalse(query.IsUnique("Weapon_Legendary"), "a legendary is a roll of its template, not the only one of it");
            Assert.IsFalse(query.IsUnique(OrdinaryRewardId), "an id no catalog claims is an ordinary stack");
        }

        private static EquipItemBlueprint Blueprint(string id, Rarity rarity) => new() { Id = id, Rarity = rarity };

        private QuestLogService CreateQuestLog() =>
            new(_quests, _facts, _inventory.Object, _minter.Object, _uniqueItems.Object, _influence.Object,
                Mock.Of<IWorldClock>(), Mock.Of<INpcWorldRegistry>(), _events,
                Mock.Of<Core.MessageBus.IGameMessageBus>(), Mock.Of<Core.Save.ILoadScope>());

        private void Accept(QuestLogService? questLog = null)
        {
            var log = questLog ?? _questLog;
            Assert.IsTrue(log.Accept(QuestId, NarrativeContext.Empty), "the repeatable quest must be offerable again");
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.GetStatus(QuestId), "the fixture objective is met from the start");
        }

        private void TurnInOnce(QuestLogService? questLog = null)
        {
            var log = questLog ?? _questLog;
            Accept(log);
            Assert.IsTrue(log.TurnIn(QuestId, NarrativeContext.Empty));
        }

        /// <summary>A repeatable quest paying all three kinds of reward at once: an ordinary stack, an
        /// artefact, influence and an action.</summary>
        private QuestDefinition RepeatableQuest() =>
            new(QuestId, string.Empty, Fractions.Human, 1, true, [], DeclinePolicy.CanReturn, 0, true, 0,
                [],
                [Stage()],
                new QuestRewards(InfluenceReward,
                    [new QuestRewardItem(OrdinaryRewardId, 1), new QuestRewardItem(UniqueRewardId, 1)],
                    [_rewardAction]),
                [], [], []);

        private static QuestDefinition RewardlessQuest() =>
            new(RewardlessQuestId, string.Empty, Fractions.Human, 1, true, [], DeclinePolicy.CanReturn, 0, true, 0,
                [], [Stage()], new QuestRewards(0, [], []), [], [], []);

        private static QuestStageDefinition Stage() =>
            new("Stage", [new QuestObjectiveDefinition("Objective", new MetCondition(), null, false, false)], [], [], [], null);

        private const string OrnamentCatalogJson = """
        { "ornaments": [ { "id": "Ornament_Test", "tier": 1, "rarity": "Unique" } ] }
        """;

        private const string LedgerlessSaveJson = $$"""
        {
          "quests": [
            {
              "questId": "{{QuestId}}",
              "status": "Completed",
              "stageIndex": 0,
              "counterBaselines": {},
              "ghostHintShown": false,
              "acceptedAtMinutes": 0,
              "nextOfferAtMinutes": 0
            }
          ]
        }
        """;

        private const string BuriedQuestSaveJson = $$"""
        {
          "quests": [
            {
              "questId": "{{BuriedQuestId}}",
              "status": "Completed",
              "stageIndex": 0,
              "counterBaselines": {},
              "ghostHintShown": false,
              "acceptedAtMinutes": 0,
              "nextOfferAtMinutes": 0,
              "grantedUniqueRewards": [ "{{UniqueRewardId}}" ]
            }
          ]
        }
        """;

        private sealed class MetCondition : INarrativeCondition
        {
            public bool IsMet(NarrativeContext context) => true;
        }

        private sealed class CountingAction : INarrativeAction
        {
            public int Executions { get; private set; }

            public void Execute(NarrativeContext context) => Executions++;
        }

        private sealed class QuestCatalog : IQuestProvider
        {
            private readonly Dictionary<string, QuestDefinition> _quests = [];

            public IReadOnlyCollection<QuestDefinition> All => _quests.Values;

            public QuestDefinition? Get(string questId) => _quests.GetValueOrDefault(questId);

            public void Add(QuestDefinition quest) => _quests[quest.Id] = quest;
        }
    }
}
