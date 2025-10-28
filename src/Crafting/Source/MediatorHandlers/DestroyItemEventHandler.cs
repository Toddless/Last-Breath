namespace Crafting.Source.MediatorHandlers
{
    using Godot;
    using Crafting.Source;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;

    public class DestroyItemEventHandler : IEventHandler<DestroyItemEvent>
    {
        private readonly IInventory _inventory;
        private readonly IItemDataProvider _dataProvider;
        private readonly ISystemMediator _systemMediator;
        private readonly CraftingMastery _craftingMastery;

        public DestroyItemEventHandler(IInventory inventory, IItemDataProvider provider, ISystemMediator systemMediator, CraftingMastery craftingMastery)
        {
            _inventory = inventory;
            _dataProvider = provider;
            _systemMediator = systemMediator;
            _craftingMastery = craftingMastery;
        }

        public void Handle(DestroyItemEvent request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return;
            // here for example we can later call a service with player´s mastery to get minimum amount resources to return
            foreach (var res in item.UsedResources)
            {
                var amount = Mathf.RoundToInt(res.Value * _craftingMastery.GetFinalResourceMultiplier());
                if (!_inventory.TryAddItemStacks(res.Key, amount))
                {
                    var itemInstance = _dataProvider.CopyBaseItem(res.Key);
                    _inventory.TryAddItem(itemInstance, amount);
                }
            }
            // ______________________________________________________________________________________________________________
            _inventory.RemoveItemByInstanceId(request.ItemInstanceId);
            _systemMediator.Publish(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Shatter, item.Rarity));
        }
    }
}
