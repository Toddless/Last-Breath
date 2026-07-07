namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views.UI;
    using Godot;

    public class OptionalCraftingResourceRequirementConfiguration(string[] categories, IGameServiceProvider provider, Func<List<string>> resourcesAlreadyInSlots)
        : IRequirementUiConfiguration
    {
        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IItemDataProvider? _itemDataProvider;
        private IRequirementUi? _ui;

        public void Configure(IRequirementUi ui)
        {
            _ui = ui;
            _messageBus = provider.GetService<IGameMessageBus>();
            _inventory = provider.GetService<IInventory>();
            _itemDataProvider = provider.GetService<IItemDataProvider>();
            ui.SetDisplayText("Optional", 0, 0);
            ui.SetIcon(null);
            ui.IsClickable = true;
            ui.LeftClick += OnLeftClick;
            ui.RightClick += OnRightClick;
            _inventory.ItemAmountChanges += OnItemAmountChanges;
        }

        private void OnItemAmountChanges(string id, int amountHave)
        {
            var item = _inventory?.GetItem<IItem>(id);
            if (item == null || _ui?.Id != item.Id) return;
            _ui?.SetDisplayText(Localization.Localize(item.Id), amountHave, 1);
        }

        private void OnRightClick(IRequirementUi req)
        {
            req.SetDisplayText("Optional", 0, 0);
            req.SetIcon(null);
            req.IsClickable = true;
        }

        private async void OnLeftClick(IRequirementUi req)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                ArgumentNullException.ThrowIfNull(_inventory);
                ArgumentNullException.ThrowIfNull(_itemDataProvider);

                var selected = await _messageBus.SendRequest<OpenCraftingItemsWindowRequest, IEnumerable<string>>(new(resourcesAlreadyInSlots.Invoke(), categories, true));
                string? id = selected.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(id)) return;
                req.SetDisplayText(Localization.Localize(id), _inventory.GetTotalItemAmount(id), 1);
                req.SetIcon(_itemDataProvider.GetItemIcon(id));
                req.Id = id;
                req.IsResource = true;
            }
            catch (Exception e)
            {
                GD.Print($"Failed to create crafting items list: {e.Message} \n {e.StackTrace}");
                Tracker.TrackException("Failed to create crafting items list", e, this);
            }
        }

        public void Dispose()
        {
            _ui?.LeftClick -= OnLeftClick;
            _ui?.RightClick -= OnRightClick;
            _ui = null;
            _inventory = null;
            _itemDataProvider = null;
            _messageBus = null;
        }
    }
}
