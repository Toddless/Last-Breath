namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Results;
    using Core.Services;
    using Crafting.Source;
    using Crafting.Source.RequestHandlers;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>The consumables catalog against the REAL SharedData: every additive id must resolve to a
    /// shipped resource, fluxes must stack their sharpening bonus on top of mastery, and creation runes
    /// must parse their rarity floor and feed the best one into the creation roll.</summary>
    [TestClass]
    public class CraftingAdditivesTests
    {
        private static CraftingAdditiveProvider s_additives = null!;

        [ClassInitialize]
        public static void SetUp(TestContext _)
        {
            s_additives = new CraftingAdditiveProvider();
            string json = File.ReadAllText(Path.Combine(SharedDataRoot(), "CraftingAdditives", "CraftingAdditives.json"));
            s_additives.Apply(DataCatalog.CraftingAdditives, new GameDataFile("CraftingAdditives.json", json));
        }

        [TestMethod]
        public void EveryAdditiveIdExistsInTheResourcesCatalog()
        {
            Assert.IsTrue(s_additives.KnownAdditiveIds.Count > 0, "The additives catalog is empty — the audit has nothing to audit.");

            var resourceIds = Directory.EnumerateFiles(Path.Combine(SharedDataRoot(), "Resources"), "*.json", SearchOption.AllDirectories)
                .Select(file => JObject.Parse(File.ReadAllText(file)))
                .SelectMany(root => ((root["upgradeResources"] as JArray) ?? []).Concat((root["craftingResources"] as JArray) ?? []))
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            var orphans = s_additives.KnownAdditiveIds.Where(id => !resourceIds.Contains(id)).ToList();
            Assert.AreEqual(0, orphans.Count, $"Additive ids without a resource: {string.Join(", ", orphans)}");
        }

        [TestMethod]
        public void FluxBonus_StacksOnTopOfTheMasteryBonus()
        {
            // Chance on a +0 item: 0.95 × (1 + 0.02 mastery) = 0.969. A roll of 0.98 fails bare…
            var mastery = new Mock<ICraftingMastery>();
            mastery.Setup(mock => mock.GetUpgradeChanceBonus()).Returns(0.02f);
            var rnd = new Mock<Core.Entity.Components.IRandomNumberGenerator>();
            rnd.Setup(mock => mock.RandFloat()).Returns(0.98f);
            var provider = new Mock<IItemDataProvider>();
            var upgrader = new ItemUpgrader(rnd.Object, mastery.Object, provider.Object, s_additives, new ModifierMaterializer(rnd.Object));

            var bare = upgrader.TryUpgradeItem(new EquipItem(EquipmentPiece.Helmet, "Crown", []));
            Assert.AreEqual(ItemUpgradeResult.Failure, bare, "0.98 must fail at 0.969 without a flux.");

            // …and succeeds with the real Simple Flux (+0.03 flat): 0.969 + 0.03 = 0.999.
            var fluxed = upgrader.TryUpgradeItem(new EquipItem(EquipmentPiece.Helmet, "Crown", []), ["Upgrade_Resource_Simple_Flux"]);
            Assert.AreEqual(ItemUpgradeResult.Success, fluxed, "The flux bonus must ADD to the mastery-scaled curve.");
        }

        [TestMethod]
        public void AllFourFluxes_CarryTheReferenceBonuses()
        {
            foreach ((string id, float bonus) in (ReadOnlySpan<(string, float)>)
                     [("Upgrade_Resource_Simple_Flux", 0.03f), ("Upgrade_Resource_Fine_Flux", 0.06f),
                      ("Upgrade_Resource_Quality_Flux", 0.09f), ("Upgrade_Resource_Perfect_Flux", 0.15f)])
            {
                var effects = s_additives.GetEffects(id);
                Assert.IsNotNull(effects, $"{id} is missing from the additives catalog.");
                Assert.AreEqual(bonus, effects.UpgradeChanceBonus, 0.0001f, $"{id} bonus drifted from the reference.");
            }
        }

        [TestMethod]
        public void CreationRunes_ParseTheirRarityFloorStrictly()
        {
            foreach ((string id, var floor) in (ReadOnlySpan<(string, Rarity)>)
                     [("Upgrade_Resource_Apprentice_Rune", Rarity.Uncommon), ("Upgrade_Resource_Journeyman_Rune", Rarity.Rare),
                      ("Upgrade_Resource_Master_Rune", Rarity.Epic), ("Upgrade_Resource_Grandmaster_Rune", Rarity.Legendary)])
            {
                var effects = s_additives.GetEffects(id);
                Assert.IsNotNull(effects, $"{id} is missing from the additives catalog.");
                Assert.AreEqual(floor, effects.MinRarity, $"{id} floor drifted from the reference.");
            }
        }

        [TestMethod]
        public async Task CreateHandler_TwoRunes_PassesTheBestFloorToTheCreationRoll()
        {
            // Journeyman (Rare) + Master (Epic): Epic is the lower enum value — the better floor wins.
            var creation = new Mock<IItemCreationService>();
            creation.Setup(mock => mock.CreateItemByRecipe(It.IsAny<string>(), It.IsAny<IEnumerable<IModifierDescriptor>>(), It.IsAny<Rarity?>()))
                .Returns(Mock.Of<IEquipItem>());
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetRecipeRequirements(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(99);
            var handler = new CreateEquipItemRequestHandler(
                creation.Object, Mock.Of<IGameMessageBus>(), provider.Object, Mock.Of<ICraftingMastery>(),
                new CraftingResources(inventory.Object), s_additives, inventory.Object);

            await handler.HandleRequest(new CreateEquipItemRequest("Recipe_Test",
                RequiredResources: [],
                OptionalResources: new Dictionary<string, int>
                {
                    ["Upgrade_Resource_Journeyman_Rune"] = 1,
                    ["Upgrade_Resource_Master_Rune"] = 1,
                }));

            creation.Verify(mock => mock.CreateItemByRecipe("Recipe_Test", It.IsAny<IEnumerable<IModifierDescriptor>>(), Rarity.Epic), Times.Once);
        }

        [TestMethod]
        public async Task CreateHandler_NoRunes_PassesNoFloor()
        {
            var creation = new Mock<IItemCreationService>();
            creation.Setup(mock => mock.CreateItemByRecipe(It.IsAny<string>(), It.IsAny<IEnumerable<IModifierDescriptor>>(), It.IsAny<Rarity?>()))
                .Returns(Mock.Of<IEquipItem>());
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetRecipeRequirements(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(99);
            var handler = new CreateEquipItemRequestHandler(
                creation.Object, Mock.Of<IGameMessageBus>(), provider.Object, Mock.Of<ICraftingMastery>(),
                new CraftingResources(inventory.Object), s_additives, inventory.Object);

            // A flux in the optional slot is a legal (if pointless) choice — it must not fabricate a floor.
            await handler.HandleRequest(new CreateEquipItemRequest("Recipe_Test",
                RequiredResources: [],
                OptionalResources: new Dictionary<string, int> { ["Upgrade_Resource_Simple_Flux"] = 1 }));

            creation.Verify(mock => mock.CreateItemByRecipe("Recipe_Test", It.IsAny<IEnumerable<IModifierDescriptor>>(), null), Times.Once);
        }

        private static string SharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
            return Path.Combine(directory.FullName, "SharedData");
        }
    }
}
