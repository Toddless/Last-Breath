namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
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
        ICraftingAdditiveProvider additives,
        IInventory inventory,
        IRecipeKnowledge knowledge)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> HandleRequest(CreateEquipItemRequest request)
        {
            try
            {
                // TODO: validates mastery + resource availability but NOT that the resources satisfy the recipe's
                // requirements (categories/specific ids/amounts). Safe while items are only created through the
                // crafting UI; add domain-level recipe conformance if a non-UI creation path appears.
                var allResources = MergeResources(request.RequiredResources, request.OptionalResources);
                if (!knowledge.IsKnown(request.RecipeId) || !MasteryAllows(request.RecipeId) || !resources.HasAll(allResources))
                    return Task.FromResult<IEquipItem?>(null);

                // Required and optional resources both feed the creation pool by design; entries from
                // byCategory sections only serve the crafted item's own equipment category.
                var descriptors = allResources.Keys.SelectMany(itemDataProvider.GetResourceDescriptors);
                string resultItemId = itemDataProvider.GetRecipeResultItemId(request.RecipeId);
                if (itemDataProvider.GetBlueprint(resultItemId)?.Piece is { } piece)
                    descriptors = descriptors.ForCategory(piece.ConvertEquipmentPartToCategory());
                // Creation runes ride the optional slots: the best floor among them guarantees the rarity.
                var item = (IEquipItem)creationService.CreateItemByRecipe(request.RecipeId, descriptors, BestMinRarity(request.OptionalResources.Keys));
                item.SaveUsedResources(request.RequiredResources, request.OptionalResources);

                resources.TrySpend(allResources);
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

        /// <summary>One spendable map: a resource picked in both a requirement and an additive slot sums.</summary>
        private static Dictionary<string, int> MergeResources(Dictionary<string, int> required, Dictionary<string, int> optional)
        {
            var merged = new Dictionary<string, int>(required);
            foreach ((string id, int amount) in optional)
                merged[id] = merged.GetValueOrDefault(id) + amount;
            return merged;
        }

        /// <summary>The BEST rarity floor among the used optional resources (lower enum value = better —
        /// Math.Min over the enum); null when no creation rune took part.</summary>
        private Rarity? BestMinRarity(IEnumerable<string> optionalResourceIds) =>
            optionalResourceIds
                .Select(id => additives.GetEffects(id)?.MinRarity)
                .Where(minRarity => minRarity != null)
                .OrderBy(minRarity => minRarity!.Value)
                .FirstOrDefault();

        private bool MasteryAllows(string recipeId) =>
            itemDataProvider.GetRecipeRequirements(recipeId)
                .Where(requirement => requirement.Type == RequirementType.MasteryLevel)
                .All(requirement => mastery.CurrentLevel >= requirement.Amount);
    }
}
