namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Core.Data;
    using Core.Interfaces.UI;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    public partial class InventorySlotTooltipButtons : Control, IInitializable, IRequireServices, IClosable
    {
        private const string UID = "uid://dor0kden4oc1j";
        [Export] private Button? _equip, _update, _destroy, _favorite;
        private IGameMessageBus? _mediator;
        private string _itemInstance = string.Empty;

        public event Action? Close;

        public override void _Ready()
        {
            _equip?.Pressed += OnEquipPressed;
            _update?.Pressed += OnUpdatePressed;
            _destroy?.Pressed += OnDestroyPressed;
            _favorite?.Pressed += OnFavoritePressed;
        }

        public override void _ExitTree()
        {
            // _mediator?.RaiseUpdateUi();
            Close?.Invoke();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IGameMessageBus>();
        }

        public void SetItemInstanceId(string instanceId) => _itemInstance = instanceId;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnFavoritePressed()
        {
        }

        private void OnDestroyPressed()
        {
            _mediator?.PublishAsync(new DestroyItemEvent(_itemInstance));
            Close?.Invoke();
        }

        private void OnUpdatePressed()
        {
            _mediator?.PublishAsync(new OpenCraftingWindowEvent(_itemInstance, true));
            Close?.Invoke();
        }

        private void OnEquipPressed()
        {
        }
    }
}
