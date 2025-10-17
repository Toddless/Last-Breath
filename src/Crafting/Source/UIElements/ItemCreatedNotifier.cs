namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Core.Interfaces.UI;

    [Tool]
    [GlobalClass]
    public partial class ItemCreatedNotifier : Control, IInitializable, IClosable
    {
        private const string UID = "uid://cu1u7ht1lp0hc";
        [Export] private Button? _okButton, _destroyButton;
        [Export] private Control? _container;
        [Export] private PanelContainer? _panel;
        public event Action? Close;
        [Signal] public delegate void CanBeClosedEventHandler();

        public event Action? DestroyPressed;

        public override void _Ready()
        {
            if (_okButton != null) _okButton.Pressed += QueueFree;

            if (_destroyButton != null) _destroyButton.Pressed += OnDestroyPressed;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_accept"))
            {
                QueueFree();
                GetViewport().SetInputAsHandled();
            }
        }

        public override void _ExitTree()
        {
            if (_destroyButton != null)
            {
                _destroyButton.Pressed -= OnDestroyPressed;
                DestroyPressed = null;
            }
        }

        public void SetItemDetails(ItemDetails node)
        {
            _panel.CustomMinimumSize += node.Size;
            _container?.AddChild(node);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnDestroyPressed()
        {
            DestroyPressed?.Invoke();
            QueueFree();
        }
    }
}
