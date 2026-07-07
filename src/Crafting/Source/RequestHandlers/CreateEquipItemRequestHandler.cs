namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;

    public class CreateEquipItemRequestHandler(
        IItemCreationService creationService,
        IGameMessageBus gameMessageBus,
        IItemDataProvider itemDataProvider,
        IInventory inventory)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> HandleRequest(CreateEquipItemRequest request)
        {
            try
            {
                var modifiers = request.UsedResources.SelectMany(res => itemDataProvider.GetResourceModifiers(res.Key));
                var item = (IEquipItem)creationService.CreateItemByRecipe(request.RecipeId, modifiers);
                item.SaveUsedResources(request.UsedResources.ToDictionary());
                gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Create, item.Rarity));

                foreach ((string resourceId, int quantity) in request.UsedResources)
                    inventory.RemoveItemById(resourceId, quantity);

                inventory.TryAddItem(item);
                return Task.FromResult<IEquipItem?>(item);
            }
            catch (InvalidOperationException ex)
            {
                Tracker.TrackException($"Failed to create equip item for recipe: {request.RecipeId}", ex, this);
                return Task.FromResult<IEquipItem?>(null);
            }
        }
    }
}
