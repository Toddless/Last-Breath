namespace Crafting.Source.MediatorHandlers
{
    using System;
    using Utilities;
    using System.Linq;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Mediator.Requests;

    public class CreateEquipItemHandler : IRequestHandler<CreateEquipItemRequest, IEquipItem?>
    {
        private readonly IItemCreator _itemCreator;
        private readonly ISystemMediator _systemMediator;
        private readonly IItemDataProvider _itemDataProvider;

        public CreateEquipItemHandler(IItemCreator creator, ISystemMediator systemMediator, IItemDataProvider itemDataProvider)
        {
            _itemCreator = creator;
            _systemMediator = systemMediator;
            _itemDataProvider = itemDataProvider;
        }

        public Task<IEquipItem?> Handle(CreateEquipItemRequest request)
        {
            try
            {
                var modifiers = request.UsedResources.SelectMany(res => _itemDataProvider.GetResourceModifiers(res.Key));
                var item = _itemCreator.CreateEquipItem(request.RecipeId, modifiers);
                item.SaveUsedResources(request.UsedResources.ToDictionary());
                _systemMediator.Publish(new ConsumeResourcesInInventoryEvent(request.UsedResources));
                _systemMediator.Publish(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Create, item.Rarity));
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
