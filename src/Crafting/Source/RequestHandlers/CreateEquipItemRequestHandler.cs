namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Godot;

    /// <summary>
    /// The one place an item is born from a recipe: validates the mastery gate and the resources
    /// FIRST (the UI button is a mirror, not the gate), spends only after the item exists, and is
    /// the only code that puts the crafted item into the bag.
    /// </summary>
    public class CreateEquipItemRequestHandler(
        IItemCreationService creationService,
        IGameMessageBus gameMessageBus,
        IItemDataProvider itemDataProvider,
        ICraftingMastery mastery,
        CraftingResources resources,
        IInventory inventory)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> HandleRequest(CreateEquipItemRequest request)
        {
            try
            {
                // TODO: validates mastery + resource availability but NOT that UsedResources satisfy the recipe's
                // requirements (categories/specific ids/amounts). Safe while items are only created through the
                // crafting UI; add domain-level recipe conformance if a non-UI creation path appears.
                if (!MasteryAllows(request.RecipeId) || !resources.HasAll(request.UsedResources))
                    return Task.FromResult<IEquipItem?>(null);

                var descriptors = request.UsedResources.SelectMany(res => itemDataProvider.GetResourceDescriptors(res.Key));
                var item = (IEquipItem)creationService.CreateItemByRecipe(request.RecipeId, descriptors);
                item.SaveUsedResources(request.UsedResources.ToDictionary());

                resources.TrySpend(request.UsedResources);
                gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Create, item.Rarity));
                inventory.TryAddItem(item);
                return Task.FromResult<IEquipItem?>(item);
            }
            catch (InvalidOperationException ex)
            {
                Tracker.TrackException($"Failed to create equip item for recipe: {request.RecipeId}", ex, this);
                GD.Print($"Failed to create equip item for recipe {request.RecipeId}: {ex.Message}");
                return Task.FromResult<IEquipItem?>(null);
            }
        }

        private bool MasteryAllows(string recipeId) =>
            itemDataProvider.GetRecipeRequirements(recipeId)
                .Where(requirement => requirement.Type == RequirementType.MasteryLevel)
                .All(requirement => mastery.CurrentLevel >= requirement.Amount);
    }
}
