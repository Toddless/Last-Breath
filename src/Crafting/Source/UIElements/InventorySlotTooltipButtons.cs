namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Mediator.Requests;

    public partial class InventorySlotTooltipButtons : Control, IInitializable, IRequireServices, IClosable
    {
        private const string UID = "uid://dor0kden4oc1j";
        [Export] private Button? _equip, _update, _destroy, _favorite;
        private ISystemMediator? _systemMediator;
        private IUiMediator? _uiMediator;
        private string _itemInstance = string.Empty;

        public event Action? Close;

        public override void _Ready()
        {
            // TODO: Not sure if i need this tooltipbuttons at all
            if (_equip != null) _equip.Pressed += OnEquipPressed;
            if (_update != null) _update.Pressed += OnUpdatePressed;
            if (_destroy != null) _destroy.Pressed += OnDestroyPressed;
            if (_favorite != null) _favorite.Pressed += OnFavoritePressed;
        }

        public override void _ExitTree()
        {
            _uiMediator?.RaiseUpdateUi();
            Close?.Invoke();
        }

        public void InjectServices(Core.Interfaces.Data.IGameServiceProvider provider)
        {
            _systemMediator = provider.GetService<ISystemMediator>();
            _uiMediator = provider.GetService<IUiMediator>();
        }

        public void SetItemInstanceId(string instanceId) => _itemInstance = instanceId;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnFavoritePressed() { }

        private void OnDestroyPressed()
        {
            _systemMediator?.Publish(new DestroyItemEvent(_itemInstance));
            Close?.Invoke();
        }

        private void OnUpdatePressed()
        {
            _uiMediator?.Publish(new OpenCraftingWindowEvent(_itemInstance, true));
            Close?.Invoke();
        }

        private void OnEquipPressed()
        {
        }
    }
}
