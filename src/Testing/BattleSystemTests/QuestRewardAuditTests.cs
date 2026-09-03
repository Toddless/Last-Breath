namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Items;
    using LootGeneration.Internal;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Audits the REAL Quests catalog against the shipped item catalogs: a quest handing out a
    /// one-of-a-kind item must not be repeatable, or every turn-in would mint another copy of an artefact
    /// the world has exactly one of. Nothing in the files says "repeatable" today — this is the guard that
    /// keeps the first entry that does from being the one nobody notices. Both doors count: the reward
    /// list and any GiveItem action the quest runs.
    /// </summary>
    [TestClass]
    public class QuestRewardAuditTests
    {
        private const string GiveItemType = "GiveItem";
        private const string ForgedQuestId = "Quest_Forged_Repeatable";
        private const string ForgedRewardId = "Ornament_Tier_1";

        [TestMethod]
        public void ShippedQuests_NeverRepeatAOneOfAKindReward()
        {
            var uniqueItems = ShippedUniqueItemQuery();
            var quests = ShippedQuests();

            Assert.IsTrue(quests.SelectMany(HandedOutItemIds).Any(uniqueItems.IsUnique),
                "no shipped quest hands out a one-of-a-kind item any more — the guard is checking nothing");

            var offenders = RepeatedUniqueRewards(quests, uniqueItems);

            Assert.AreEqual(0, offenders.Count,
                $"repeatable quests that would mint their artefact again on every turn-in: {string.Join(", ", offenders)}");
        }

        /// <summary>The mutation the guard exists for, run against the same rule and the same catalogs:
        /// without this, a rule that answered "unique" to nothing would pass the audit above in silence.</summary>
        [TestMethod]
        public void ARepeatableQuestRewardingAnOrnament_IsCaught()
        {
            var forged = JsonConvert.DeserializeObject<QuestsData>(ForgedQuestJson)!.Quests;

            var offenders = RepeatedUniqueRewards(forged, ShippedUniqueItemQuery());

            CollectionAssert.AreEqual(new[] { $"{ForgedQuestId} -> {ForgedRewardId}" }, offenders,
                "a repeatable quest rewarding an ornament went unnoticed — the guard cannot see the shipped ornaments");
        }

        /// <summary>The other door, on data: an ornament given by a GiveItem action would slip past a guard
        /// that only reads the reward list.</summary>
        [TestMethod]
        public void ARepeatableQuestGivingAnOrnamentByAction_IsCaught()
        {
            var forged = JsonConvert.DeserializeObject<QuestsData>(ForgedGiveItemQuestJson)!.Quests;

            var offenders = RepeatedUniqueRewards(forged, ShippedUniqueItemQuery());

            CollectionAssert.AreEqual(new[] { $"{ForgedQuestId} -> {ForgedRewardId}" }, offenders,
                "a GiveItem action handing out an ornament went unnoticed");
        }

        /// <summary>The rule reads equip blueprints and the ornament catalog; an id no catalog declares at
        /// all is one it cannot judge, so it must not pass by being unrecognised.</summary>
        [TestMethod]
        public void ShippedQuestRewards_NameItemsTheCatalogsDeclare()
        {
            (var items, var ornaments) = ShippedCatalogs();
            var resourceIds = items.GetAllResources().Select(resource => resource.Id).ToHashSet(StringComparer.Ordinal);

            var strangers = ShippedQuests()
                .SelectMany(quest => HandedOutItemIds(quest).Select(itemId => (quest.Id, ItemId: itemId)))
                .Where(entry => items.GetBlueprint(entry.ItemId) == null
                    && ornaments.Find(entry.ItemId) == null
                    && !resourceIds.Contains(entry.ItemId))
                .Select(entry => $"{entry.Id} -> {entry.ItemId}")
                .ToList();

            Assert.AreEqual(0, strangers.Count,
                $"quest hand-outs naming an id no catalog declares — the uniqueness rule does not judge those: {string.Join(", ", strangers)}");
        }

        private static List<string> RepeatedUniqueRewards(IEnumerable<QuestEntry> quests, IUniqueItemQuery uniqueItems) =>
            quests
                .Where(quest => quest.Repeatable)
                .SelectMany(quest => HandedOutItemIds(quest)
                    .Where(uniqueItems.IsUnique)
                    .Select(itemId => $"{quest.Id} -> {itemId}"))
                .ToList();

        /// <summary>Every id a quest puts in the player's hands: the quest-wide reward list, the reward
        /// list of every outcome it can end on, plus every GiveItem action it runs — rewards, the
        /// accept/decline/fail hooks and the stage hooks are all the same door.</summary>
        private static IEnumerable<string> HandedOutItemIds(QuestEntry quest) =>
            RewardLists(quest).SelectMany(rewards => rewards.Items).Select(reward => reward.ItemId).Concat(GivenItemIds(quest));

        private static IEnumerable<QuestRewardsEntry> RewardLists(QuestEntry quest) =>
            [quest.Rewards, .. quest.Stages.Select(stage => stage.Outcome?.Rewards).OfType<QuestRewardsEntry>()];

        private static IEnumerable<string> GivenItemIds(QuestEntry quest) =>
            ActionLists(quest)
                .SelectMany(actions => actions ?? new JArray())
                .OfType<JObject>()
                .Where(action => (string?)action["type"] == GiveItemType)
                .Select(action => (string?)action["itemId"] ?? string.Empty);

        private static IEnumerable<JToken?> ActionLists(QuestEntry quest) =>
        [
            .. RewardLists(quest).Select(rewards => rewards.Actions), quest.OnAccept, quest.OnDecline, quest.OnFail,
            .. quest.Stages.SelectMany(stage => new[] { stage.OnEnter, stage.OnComplete }),
        ];

        /// <summary>Every file of the catalog, the way the game loads it — a second quest file must not walk
        /// past the guard because the audit knows one name.</summary>
        private static List<QuestEntry> ShippedQuests() =>
            Directory.GetFiles(SharedData.Catalog(DataCatalog.Quests), "*.json")
                .SelectMany(file => JsonConvert.DeserializeObject<QuestsData>(File.ReadAllText(file))!.Quests)
                .ToList();

        private static IUniqueItemQuery ShippedUniqueItemQuery()
        {
            (var items, var ornaments) = ShippedCatalogs();
            return new UniqueItemQuery(items, ornaments);
        }

        /// <summary>The real parsers over the real catalogs — the rule under audit must answer from the
        /// data the game ships, not from a fixture describing it.</summary>
        private static (ItemDataProvider Items, OrnamentCatalog Ornaments) ShippedCatalogs()
        {
            var items = new ItemDataProvider(new DataParser(new ItemGameDataFactory()));
            var ornaments = new OrnamentCatalog();
            ApplyCatalog(items, DataCatalog.EquipItems);
            ApplyCatalog(items, DataCatalog.Resources);
            ApplyCatalog(ornaments, DataCatalog.Ornaments);
            return (items, ornaments);
        }

        private static void ApplyCatalog(IGameDataParticipant participant, string catalog)
        {
            foreach (string file in Directory.GetFiles(SharedData.Catalog(catalog), "*.json"))
                participant.Apply(catalog, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));
        }

        private const string ForgedQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "repeatable": true,
              "rewards": { "items": [ { "itemId": "{{ForgedRewardId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;

        private const string ForgedGiveItemQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "repeatable": true,
              "rewards": { "items": [], "actions": [ { "type": "{{GiveItemType}}", "itemId": "{{ForgedRewardId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;
    }
}
