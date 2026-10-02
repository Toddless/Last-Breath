namespace LastBreathTest.CraftingSystem
{
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Use;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Crafting.Source;
    using Crafting.Source.ItemUse;
    using Crafting.Source.RequestHandlers;
    using Moq;

    /// <summary>
    /// Recipe knowledge (the recipes-as-items rework, HANDOFF №111): the mastery ladder unlocks
    /// data-gated recipes by itself, scrolls teach the rest through the item-use seam
    /// (UseItemMessage → LearnRecipeUseBehavior), and the create handler refuses unknown recipes
    /// before touching resources. Duplicates of a known recipe stay in the bag — vendor fodder,
    /// never silently burned.
    /// </summary>
    [TestClass]
    public class RecipeKnowledgeTests
    {
        private const string LadderRecipe = "Recipe_Ladder";
        private const string ScrollRecipe = "Recipe_Scroll";

        [TestMethod]
        public void MasteryLadder_UnlocksAtGate_BonusLevelsCount()
        {
            var mastery = new Mock<ICraftingMastery>();
            var knowledge = Knowledge(mastery);

            mastery.SetupGet(mock => mock.CurrentLevel).Returns(3);
            Assert.IsFalse(knowledge.IsKnown(LadderRecipe), "below the gate");

            mastery.SetupGet(mock => mock.CurrentLevel).Returns(5);
            Assert.IsTrue(knowledge.IsKnown(LadderRecipe), "at the gate");

            mastery.SetupGet(mock => mock.CurrentLevel).Returns(4);
            mastery.SetupGet(mock => mock.BonusLevel).Returns(1);
            Assert.IsTrue(knowledge.IsKnown(LadderRecipe), "earned + bonus reaches the gate");
        }

        [TestMethod]
        public void ScrollOnlyRecipe_UnknownAtAnyMastery_UntilLearned()
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.SetupGet(mock => mock.CurrentLevel).Returns(50);
            var knowledge = Knowledge(mastery);

            Assert.IsFalse(knowledge.IsKnown(ScrollRecipe), "no unlockAtMastery = mastery never unlocks it");
            Assert.IsTrue(knowledge.Learn(ScrollRecipe));
            Assert.IsTrue(knowledge.IsKnown(ScrollRecipe));
            Assert.IsFalse(knowledge.Learn(ScrollRecipe), "a second learn refuses");
        }

        [TestMethod]
        public void LearnOfMasteryUnlockedRecipe_Refuses_SoTheScrollSurvives()
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.SetupGet(mock => mock.CurrentLevel).Returns(10);
            var knowledge = Knowledge(mastery);

            Assert.IsFalse(knowledge.Learn(LadderRecipe));
            Assert.AreEqual(0, knowledge.LearnedIds.Count);
        }

        [TestMethod]
        public void RestoreState_RoundTrips_AndSessionResetClears()
        {
            var knowledge = Knowledge(new Mock<ICraftingMastery>());
            knowledge.Learn(ScrollRecipe);

            var restored = Knowledge(new Mock<ICraftingMastery>());
            restored.RestoreState(knowledge.LearnedIds);
            Assert.IsTrue(restored.IsKnown(ScrollRecipe));

            restored.ResetSession();
            Assert.IsFalse(restored.IsKnown(ScrollRecipe));
        }

        [TestMethod]
        public async Task UseSeam_UnknownRecipe_LearnsAndConsumesTheScroll()
        {
            var knowledge = Knowledge(new Mock<ICraftingMastery>());
            var scroll = Scroll(ScrollRecipe);
            var inventory = InventoryWith(scroll);

            await Handle(inventory, knowledge, scroll);

            Assert.IsTrue(knowledge.IsKnown(ScrollRecipe));
            inventory.Verify(mock => mock.RemoveItemByInstanceId(scroll.InstanceId), Times.Once);
        }

        [TestMethod]
        public async Task UseSeam_KnownRecipe_RefusesAndKeepsTheScroll()
        {
            var knowledge = Knowledge(new Mock<ICraftingMastery>());
            knowledge.Learn(ScrollRecipe);
            var scroll = Scroll(ScrollRecipe);
            var inventory = InventoryWith(scroll);

            await Handle(inventory, knowledge, scroll);

            inventory.Verify(mock => mock.RemoveItemByInstanceId(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateHandler_UnknownRecipe_RefusesBeforeSpending()
        {
            var creation = new Mock<IItemCreationService>();
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetRecipeRequirements(It.IsAny<string>())).Returns([]);
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(99);
            var handler = new CreateEquipItemRequestHandler(
                creation.Object, Mock.Of<IGameMessageBus>(), provider.Object, Mock.Of<ICraftingMastery>(),
                new CraftingResources(inventory.Object), Mock.Of<ICraftingAdditiveProvider>(), inventory.Object,
                Knowledge(new Mock<ICraftingMastery>()));

            var result = await handler.HandleRequest(new CreateEquipItemRequest(ScrollRecipe, [], []));

            Assert.IsNull(result);
            creation.Verify(mock => mock.CreateItemByRecipe(It.IsAny<string>(), It.IsAny<System.Collections.Generic.IEnumerable<Core.Modifiers.IModifierDescriptor>>(), It.IsAny<Rarity?>()), Times.Never);
            inventory.Verify(mock => mock.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>The ladder recipe unlocks at mastery 5; the scroll recipe never by itself.</summary>
        private static RecipeKnowledge Knowledge(Mock<ICraftingMastery> mastery)
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetCraftingRecipes()).Returns(
            [
                new CraftingRecipe(LadderRecipe, "Item_A", [], Rarity.Uncommon, [], ItemType.Equipment, [], unlockAtMastery: 5),
                new CraftingRecipe(ScrollRecipe, "Item_B", [], Rarity.Rare, [], ItemType.Equipment, []),
            ]);
            return new RecipeKnowledge(provider.Object, mastery.Object);
        }

        private static CraftingRecipe Scroll(string recipeId) =>
            new(recipeId, "Item_B", [], Rarity.Rare, [], ItemType.Equipment, []);

        private static Mock<IInventory> InventoryWith(CraftingRecipe scroll)
        {
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetItem<IItem>(scroll.InstanceId)).Returns(scroll);
            return inventory;
        }

        /// <summary>The full seam, not the behavior in isolation: message → dispatch → behavior.</summary>
        private static Task Handle(Mock<IInventory> inventory, IRecipeKnowledge knowledge, CraftingRecipe scroll)
        {
            var behavior = new LearnRecipeUseBehavior(inventory.Object, knowledge, Mock.Of<IGameMessageBus>());
            var handler = new UseItemMessageHandler(inventory.Object, new ItemUseService([behavior]));
            return handler.HandleMessageAsync(new UseItemMessage(scroll.InstanceId));
        }
    }
}
