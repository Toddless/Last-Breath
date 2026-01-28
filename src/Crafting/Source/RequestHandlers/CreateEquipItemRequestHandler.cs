namespace Crafting.Source.RequestHandlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;
    using Utilities;

    public class CreateEquipItemRequestHandler(
        IItemCreator creator,
        IMediator mediator,
        IItemDataProvider itemDataProvider)
        : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        public Task<IEquipItem?> Handle(CreateEquipItemRequest request)
        {
            try
            {
                var modifiers =
                    request.UsedResources.SelectMany(res => itemDataProvider.GetResourceModifiers(res.Key));
                var item = creator.CreateEquipItem(request.RecipeId, modifiers);
                item.SaveUsedResources(request.UsedResources.ToDictionary());
                mediator.PublishAsync(new ConsumeResourcesInInventoryEvent(request.UsedResources));
                mediator.PublishAsync(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Create, item.Rarity));
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
