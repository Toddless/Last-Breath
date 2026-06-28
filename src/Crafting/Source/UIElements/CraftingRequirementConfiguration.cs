namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using Godot;
    using Utilities;

    public class CraftingRequirementConfiguration(IRequirement requirement, IGameServiceProvider provider, Func<List<string>> resourcesAlreadyInSlots) : IRequirementUiConfiguration
    {
        private IRequirementUi? _ui;
        private IInventory? _inventory;
        private IGameMessageBus? _messageBus;
        private string _category = string.Empty;
        private IItemDataProvider? _itemDataProvider;

        public void Configure(IRequirementUi ui)
        {
            _ui = ui;
            _inventory = provider.GetService<IInventory>();
            _itemDataProvider = provider.GetService<IItemDataProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _inventory.ItemAmountChanges += OnItemAmountChanges;
            switch (requirement.Type)
            {
                case RequirementType.ResourceCategory:
                    ConfigureResourceCategory();
                    break;
                case RequirementType.Resource:
                    ConfigureResource();
                    break;
                case RequirementType.MasteryLevel:
                    ConfigureMasteryLevel();
                    break;
            }
        }

        public void Dispose()
        {
            _ui?.LeftClick -= OnLeftClick;
            _ui?.RightClick -= OnRightClick;
            GC.SuppressFinalize(this);
        }

        private void ConfigureMasteryLevel()
        {
            var craftingMastery = provider.GetService<ICraftingMastery>();
            int currentLevel = craftingMastery.CurrentLevel;
            SetProperties(Localization.Localize(requirement.Id), currentLevel, requirement.Amount, craftingMastery.Icon);
            _ui?.Id = requirement.Id;
            _ui?.IsResource = false;
        }

        private void ConfigureResource()
        {
            int have = _inventory?.GetTotalItemAmount(requirement.Id) ?? 0;
            var icon = _itemDataProvider?.GetItemIcon(requirement.Id);
            SetProperties(Localization.Localize(requirement.Id), have, requirement.Amount, icon);
            _ui?.Id = requirement.Id;
            _ui?.IsResource = true;
        }


        private void ConfigureResourceCategory()
        {
            SetProperties(Localization.Localize(requirement.Id), 0, requirement.Amount, null, true);
            _ui?.Id = requirement.Id;
            _category = requirement.Id;
            _ui?.IsResource = false;
            _ui?.LeftClick += OnLeftClick;
            _ui?.RightClick += OnRightClick;
        }

        private void OnRightClick(IRequirementUi ui)
        {
            SetProperties(Localization.Localize(requirement.Id), 0, requirement.Amount, null, true);
            ui.Id = _category;
            ui.IsResource = false;
        }

        private void OnItemAmountChanges(string id, int amount)
        {
            var item = _inventory?.GetItem<IItem>(id);
            if (item == null || _ui?.Id != item.Id) return;
            SetProperties(Localization.Localize(item.Id), amount, requirement.Amount, null, true);
        }

        private async void OnLeftClick(IRequirementUi ui)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                ArgumentNullException.ThrowIfNull(_inventory);
                ArgumentNullException.ThrowIfNull(_itemDataProvider);

                var selected = await _messageBus.SendRequest<OpenCraftingItemsWindowRequest, IEnumerable<string>>(new(resourcesAlreadyInSlots.Invoke(), [ui.Id], true));
                string? id = selected.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(id)) return;
                SetProperties(Localization.Localize(id), _inventory.GetTotalItemAmount(id), requirement.Amount, _itemDataProvider.GetItemIcon(id), true);
                _ui?.Id = id;
                _ui?.IsResource = true;
            }
            catch (Exception exception)
            {
                GD.PrintErr("Failed to chose crafting item ", exception.Message, exception.StackTrace);
                Tracker.TrackException("Failed to chose crafting item", exception, this);
            }
        }

        private void SetProperties(string displayText, int amountHave, int amountNeed, Texture2D? icon, bool isClickable = false)
        {
            _ui?.SetDisplayText(displayText, amountHave, amountNeed);
            _ui?.SetIcon(icon);
            _ui?.IsClickable = isClickable;
        }
    }
}
