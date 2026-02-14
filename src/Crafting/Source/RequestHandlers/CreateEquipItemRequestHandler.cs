namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Utilities;

    public class CreateEquipItemRequestHandler(
        IItemCreator creator,
        IGameMessageBus gameMessageBus,
        IItemDataProvider itemDataProvider)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> HandleRequest(CreateEquipItemRequest request)
        {
            try
            {
                var modifiers =
                    request.UsedResources.SelectMany(res => itemDataProvider.GetResourceModifiers(res.Key));
                var item = creator.CreateEquipItem(request.RecipeId, modifiers);
                item.SaveUsedResources(request.UsedResources.ToDictionary());
                gameMessageBus.PublishAsync(new ConsumeResourcesInInventoryEvent(request.UsedResources));
                gameMessageBus.PublishAsync(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Create, item.Rarity));
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
