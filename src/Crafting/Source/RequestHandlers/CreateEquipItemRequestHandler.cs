namespace Crafting.Source.RequestHandlers
{
    using System;
    using Core.Data;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

    public class CreateEquipItemRequestHandler(
        IItemCreationService creationService,
        IGameMessageBus gameMessageBus,
        IItemDataProvider itemDataProvider)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> HandleRequest(CreateEquipItemRequest request)
        {
            try
            {
                var modifiers = request.UsedResources.SelectMany(res => itemDataProvider.GetResourceModifiers(res.Key));
                var item = (IEquipItem)creationService.CreateItemByRecipe(request.RecipeId, modifiers);
                item.SaveUsedResources(request.UsedResources.ToDictionary());
                gameMessageBus.PublishAsync(new ConsumeResourcesInInventoryEvent(request.UsedResources));
                gameMessageBus.PublishAsync(new GainCraftingExpirienceEvent(CraftingMode.Create, item.Rarity));
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
