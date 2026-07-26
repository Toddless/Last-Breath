namespace Crafting.Source.UIElements
{
    using System;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Use;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Views.UI;
    using Godot;

    public partial class InventorySlotTooltipButtons : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://dor0kden4oc1j";
        [Export] private Button? _equip, _update, _recraft, _ascend, _destroy, _favorite, _use;
        private IGameMessageBus? _mediator;
        private IInventory? _inventory;
        private IItemUseService? _useService;
        private string _itemInstance = string.Empty;

        public event Action? Close;

        public override void _Ready()
        {
            _equip?.Pressed += OnEquipPressed;
            _update?.Pressed += () => OpenCrafting(CraftingMode.Upgrade);
            _recraft?.Pressed += () => OpenCrafting(CraftingMode.Recraft);
            _ascend?.Pressed += () => OpenCrafting(CraftingMode.Ascend);
            _destroy?.Pressed += OnDestroyPressed;
            _favorite?.Pressed += OnFavoritePressed;
            _use?.Pressed += OnUsePressed;

            _update?.Text = Localization.Localize("UI_Crafting_Upgrade");
            _recraft?.Text = Localization.Localize("UI_Crafting_Recraft");
            _ascend?.Text = Localization.Localize("UI_Crafting_Ascend");
        }

        public override void _ExitTree()
        {
            // _mediator?.RaiseUpdateUi();
            Close?.Invoke();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IGameMessageBus>();
            _inventory = provider.GetService<IInventory>();
            _useService = provider.GetService<IItemUseService>();
            RefreshCraftButtons();
        }

        public void SetItemInstanceId(string instanceId)
        {
            _itemInstance = instanceId;
            RefreshCraftButtons();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>The tooltip mirrors only the obvious (EquipItemCraftActions): recraft on any
        /// unsealed equip, ascend on legendaries — the deep CanAscend check stays with the window.
        /// A usable item (recipe scroll, future consumables) swaps the craft row for its use
        /// button — caption and enabled-state come from the behavior, whose Use stays the real
        /// gate. Runs on both entry points because the id and the services arrive in either order.</summary>
        private void RefreshCraftButtons()
        {
            _use?.Visible = false;
            if (_inventory?.GetItem<IEquipItem>(_itemInstance) is { } item)
            {
                _update?.Visible = EquipItemCraftActions.CanShowUpgrade(item);
                _recraft?.Visible = EquipItemCraftActions.CanShowRecraft(item);
                _ascend?.Visible = EquipItemCraftActions.CanShowAscend(item);
                return;
            }

            if (_inventory?.GetItem<IItem>(_itemInstance) is not { } usable
                || _useService?.BehaviorFor(usable) is not { } behavior) return; // leave the scene defaults

            _equip?.Visible = false;
            _update?.Visible = false;
            _recraft?.Visible = false;
            _ascend?.Visible = false;
            _favorite?.Visible = false;
            _use?.Visible = true;
            _use?.Text = Localization.Localize(behavior.LabelKey);
            _use?.Disabled = !behavior.CanUse(usable);
        }

        private void OpenCrafting(CraftingMode mode)
        {
            _mediator?.PublishMessageAsync(new OpenCraftingWindowMessage(_itemInstance, true, mode));
            Close?.Invoke();
        }

        private void OnFavoritePressed()
        {
        }

        private void OnDestroyPressed()
        {
            _mediator?.PublishMessageAsync(new DestroyItemMessage(_itemInstance));
            Close?.Invoke();
        }

        private void OnUsePressed()
        {
            _mediator?.PublishMessageAsync(new UseItemMessage(_itemInstance));
            Close?.Invoke();
        }

        private void OnEquipPressed()
        {
        }
    }
}
