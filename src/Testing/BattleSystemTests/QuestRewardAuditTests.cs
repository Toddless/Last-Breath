namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Data.Validation;
    using Core.Items;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using LootGeneration.Internal;
    using Newtonsoft.Json;
    using Tooling.Catalogs.Checks;

    /// <summary>
    /// Audits the REAL Quests catalog against the shipped item catalogs: a quest handing out a
    /// one-of-a-kind item must not be repeatable, or every turn-in would mint another copy of an artefact
    /// the world has exactly one of. Nothing in the files says "repeatable" today — this is the guard that
    /// keeps the first entry that does from being the one nobody notices. Every door counts: the reward
    /// lists and any GiveItem action the quest runs.
    /// <para>The rule is the game's own (<see cref="QuestRewardRules"/>), which the authoring tool reads
    /// over the documents an author has open. It stays a GATE here — the tool reports and the game
    /// refuses.</para>
    /// </summary>
    [TestClass]
    public class QuestRewardAuditTests
    {
        private const string ForgedQuestId = "Quest_Forged_Repeatable";
        private const string ForgedRewardId = "Ornament_Tier_1";
        private const string ForgedStrangerId = "Ornament_Nobody_Wrote";

        /// <summary>The recipe the forged quest hands over: a recipe is a thing in the bag as much as a
        /// thing to craft with, so a catalog of items that does not hold them calls every recipe reward an
        /// id nobody wrote.</summary>
        private const string ForgedRecipeId = "Recipe_Creators_Ring";

        /// <summary>The catalogs the rule asks about an id a quest hands over. Held against everywhere an
        /// item may be WRITTEN, so a seventh place to write one joins this audit and the tool's run
        /// together.</summary>
        private static readonly string[] s_itemCatalogs =
            [DataCatalog.EquipItems, DataCatalog.Recipes, DataCatalog.Resources, DataCatalog.Ornaments];

        [TestMethod]
        public void ShippedQuests_NeverRepeatAOneOfAKindReward()
        {
            IQuestRewardItems items = ShippedItems();
            List<QuestEntry> quests = ShippedQuests();

            // Every shipped quest read as if it repeated: the guard is only worth running while the
            // catalogs still hold a quest that hands out an artefact at all.
            Assert.AreNotEqual(0, Repeated(QuestRewardRules.Check([.. quests.Select(quest => quest with { Repeatable = true })], items)).Length,
                "no shipped quest hands out a one-of-a-kind item any more — the guard is checking nothing");

            string[] offenders = Repeated(QuestRewardRules.Check(quests, items));

            Assert.AreEqual(0, offenders.Length,
                $"repeatable quests that would mint their artefact again on every turn-in: {string.Join(", ", offenders)}");
        }

        /// <summary>The mutation the guard exists for, run against the same rule and the same catalogs:
        /// without this, a rule that answered "unique" to nothing would pass the audit above in silence.</summary>
        [TestMethod]
        public void ARepeatableQuestRewardingAnOrnament_IsCaught()
        {
            string[] offenders = Repeated(QuestRewardRules.Check(Forged(ForgedQuestJson), ShippedItems()));

            CollectionAssert.AreEqual(new[] { $"{ForgedQuestId} -> {ForgedRewardId}" }, offenders,
                "a repeatable quest rewarding an ornament went unnoticed — the guard cannot see the shipped ornaments");
        }

        /// <summary>The other door, on data: an ornament given by a GiveItem action would slip past a guard
        /// that only reads the reward list.</summary>
        [TestMethod]
        public void ARepeatableQuestGivingAnOrnamentByAction_IsCaught()
        {
            string[] offenders = Repeated(QuestRewardRules.Check(Forged(ForgedGiveItemQuestJson), ShippedItems()));

            CollectionAssert.AreEqual(new[] { $"{ForgedQuestId} -> {ForgedRewardId}" }, offenders,
                "a GiveItem action handing out an ornament went unnoticed");
        }

        /// <summary>The rule reads equip blueprints, the resources and the ornament catalog; an id no
        /// catalog declares at all is one it cannot judge, so it must not pass by being unrecognised.</summary>
        [TestMethod]
        public void ShippedQuestRewards_NameItemsTheCatalogsDeclare()
        {
            string[] strangers = Undeclared(QuestRewardRules.Check(ShippedQuests(), ShippedItems()));

            Assert.AreEqual(0, strangers.Length,
                $"quest hand-outs naming an id no catalog declares — the uniqueness rule does not judge those: {string.Join(", ", strangers)}");
        }

        /// <summary>A quest naming an id nothing declares is reported as that and never quietly read as an
        /// ordinary stack: the uniqueness answer about an unknown id means nothing.</summary>
        [TestMethod]
        public void AQuestRewardingAnIdNoCatalogDeclares_IsCaught()
        {
            string[] strangers = Undeclared(QuestRewardRules.Check(Forged(ForgedStrangerQuestJson), ShippedItems()));

            CollectionAssert.AreEqual(new[] { $"{ForgedQuestId} -> {ForgedStrangerId}" }, strangers,
                "a quest naming an id no catalog declares went unnoticed");
        }

        /// <summary>The same rule over the documents the authoring tool has open: a repeatable quest
        /// written into the catalog reaches the panel's Check as a finding of its own.</summary>
        [TestMethod]
        public void ARepeatableArtefactQuestInTheDocuments_ReachesTheChecksOfTheTool()
        {
            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.Quests, ForgedQuestJson);

            CollectionAssert.AreEqual(
                new[] { ForgedRewardId },
                added.Select(finding => finding.Named).ToArray(),
                $"the forged quest was read by the tool's checks as:\n  {CatalogCrossCheckTests.Lines(added)}");
        }

        /// <summary>A recipe is one of the things a quest may hand over, and it lives in a catalog of its
        /// own. The mutation the case exists for is that catalog being left out of what the rule is asked:
        /// then every quest rewarding a recipe is called an id nobody wrote. Both readings are held — the
        /// gate's own and the tool's over its open documents — because the two answer the question with
        /// catalogs of their own and either could be the one that forgets.</summary>
        [TestMethod]
        public void AQuestRewardingARecipe_IsNotCalledUndeclared()
        {
            string[] strangers = Undeclared(QuestRewardRules.Check(Forged(ForgedRecipeQuestJson), ShippedItems()));

            Assert.AreEqual(0, strangers.Length,
                $"a recipe handed over by a quest was read as an id no catalog declares: {string.Join(", ", strangers)}");

            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.Quests, ForgedRecipeQuestJson);

            Assert.AreEqual(0, added.Count,
                $"the tool's checks read the forged recipe quest as:\n  {CatalogCrossCheckTests.Lines(added)}");
        }

        /// <summary>The catalogs the rule reads an id out of, held against everywhere an item may be
        /// written. The legacy plain items are the one place left out, and on purpose: no reader of this
        /// build can build one, so a quest handing out such an id is reported rather than passed. The day
        /// that catalog is readable this pin is what says the rule has to take it in.</summary>
        [TestMethod]
        public void TheRuleReadsEveryCatalogAnItemMayBeWrittenIn_ExceptTheLegacyPlainItems()
        {
            string[] unread =
            [
                .. ItemReference.Targets
                    .Select(target => target.Catalog)
                    .Distinct(StringComparer.Ordinal)
                    .Where(catalog => !s_itemCatalogs.Contains(catalog, StringComparer.Ordinal))
                    .Order(StringComparer.Ordinal)
            ];

            CollectionAssert.AreEqual(new[] { DataCatalog.Items }, unread,
                $"the places an item may be written that the rule does not read: {string.Join(", ", unread)}");
        }

        /// <summary>A quest whose reward list is written as null is a record halfway through being typed:
        /// json takes the word and hands the reader nothing. The rule has nothing to say about it and says
        /// nothing — and the quest beside it is still judged, which is what a run over documents being
        /// edited is for.</summary>
        [TestMethod]
        public void AQuestWritingItsRewardsAsNull_IsNothingToJudgeAndLeavesItsNeighbourAlone()
        {
            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.Quests, NullRewardsQuestJson);

            CollectionAssert.AreEqual(
                new[] { ForgedStrangerId },
                added.Select(finding => finding.Named).ToArray(),
                $"the quest writing null rewards and the one beside it were read by the tool's checks as:"
                + $"\n  {CatalogCrossCheckTests.Lines(added)}");
        }

        private static string[] Repeated(IReadOnlyList<DataFinding> found) => Named(found, DataFindingKind.RepeatedUniqueReward);

        private static string[] Undeclared(IReadOnlyList<DataFinding> found) => Named(found, DataFindingKind.UndeclaredReward);

        private static string[] Named(IReadOnlyList<DataFinding> found, DataFindingKind kind) =>
            [.. found.Where(finding => finding.Kind == kind).Select(finding => $"{finding.Record} -> {finding.Named}")];

        private static List<QuestEntry> Forged(string json) => JsonConvert.DeserializeObject<QuestsData>(json)!.Quests;

        /// <summary>Every file of the catalog, the way the game loads it — a second quest file must not walk
        /// past the guard because the audit knows one name.</summary>
        private static List<QuestEntry> ShippedQuests() =>
            [.. Directory.GetFiles(SharedData.Catalog(DataCatalog.Quests), "*.json")
                .SelectMany(file => JsonConvert.DeserializeObject<QuestsData>(File.ReadAllText(file))!.Quests)];

        /// <summary>The real parsers over the real catalogs — the rule under audit must answer from the
        /// data the game ships, not from a fixture describing it.</summary>
        private static IQuestRewardItems ShippedItems()
        {
            var items = new ItemDataProvider(new DataParser(new ItemGameDataFactory()));
            var ornaments = new OrnamentCatalog();

            ApplyCatalog(items, DataCatalog.EquipItems);
            ApplyCatalog(items, DataCatalog.Recipes);
            ApplyCatalog(items, DataCatalog.Resources);
            ApplyCatalog(ornaments, DataCatalog.Ornaments);

            var unique = new UniqueItemQuery(items, ornaments);
            HashSet<string> resources = new(items.GetAllResources().Select(resource => resource.Id), StringComparer.Ordinal);
            HashSet<string> recipes = new(items.GetCraftingRecipes().Select(recipe => recipe.Id), StringComparer.Ordinal);

            return new QuestRewardItems(
                itemId => items.GetBlueprint(itemId) != null
                          || ornaments.Find(itemId) != null
                          || resources.Contains(itemId)
                          || recipes.Contains(itemId),
                unique.IsUnique);
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

        /// <summary>Written with the type the factory itself declares: a hand-out the audit spelled by hand
        /// would go on being caught after the action was renamed, and catch nothing.</summary>
        private static readonly string ForgedGiveItemQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "repeatable": true,
              "rewards": { "items": [], "actions": [ { "type": "{{GiveItemActionFactory.Spec.TypeName}}", "itemId": "{{ForgedRewardId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;

        private const string ForgedRecipeQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "rewards": { "items": [ { "itemId": "{{ForgedRecipeId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;

        /// <summary>One quest whose reward list json wrote as null, and one beside it handing over an id
        /// nobody wrote: the second is what says the first cost the file nothing.</summary>
        private const string NullRewardsQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "rewards": null,
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            },
            {
              "id": "{{ForgedQuestId}}_Neighbour",
              "rewards": { "items": [ { "itemId": "{{ForgedStrangerId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;

        private const string ForgedStrangerQuestJson = $$"""
        {
          "quests": [
            {
              "id": "{{ForgedQuestId}}",
              "rewards": { "items": [ { "itemId": "{{ForgedStrangerId}}", "amount": 1 } ] },
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;
    }
}
