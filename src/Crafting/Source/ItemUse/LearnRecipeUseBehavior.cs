namespace Crafting.Source.ItemUse
{
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Use;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;

    /// <summary>Using a recipe scroll: an unknown recipe is learned and the scroll is consumed; a
    /// known one refuses WITHOUT consuming — the duplicate stays sellable.</summary>
    public class LearnRecipeUseBehavior(
        IInventory inventory,
        IRecipeKnowledge knowledge,
        IGameMessageBus gameMessageBus)
        : IItemUseBehavior
    {
        public string LabelKey => "UI_Recipe_Learn";

        public bool CanHandle(IItem item) => item is CraftingRecipe;

        public bool CanUse(IItem item) => item is CraftingRecipe recipe && !knowledge.IsKnown(recipe.Id);

        public void Use(IItem item)
        {
            if (item is not CraftingRecipe recipe) return;

            if (!knowledge.Learn(recipe.Id))
            {
                Notify("UI_Recipe_Already_Known", recipe.Id);
                return;
            }

            inventory.RemoveItemByInstanceId(recipe.InstanceId);
            Notify("UI_Recipe_Learned", recipe.Id);
        }

        private void Notify(string templateKey, string recipeId) =>
            gameMessageBus.PublishMessageAsync(new SendNotificationMessageMessage(templateKey,
                Values: new Dictionary<string, object?> { ["Recipe"] = Localization.Localize(recipeId) }));
    }
}
