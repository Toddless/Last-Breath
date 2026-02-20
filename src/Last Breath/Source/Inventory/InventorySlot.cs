namespace LastBreath.Source.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Godot;

    public partial class InventorySlot : Slot, IInventorySlot
    {
        private const string UID = "uid://bqlqfsqoepfhs";

        private bool _isMouseInside, _rmbWasPressed, _detailsShowing;
        [Export] protected Label? QuantityLabel;

        private IGameMessageBus? _gameMessageBus;
        public Func<string, IItem?>? GetItemInstance;
        public event Action<IInventorySlot, MouseInteractions>? ItemInteraction;

        public override void _Ready()
        {
            MouseExited += OnMouseExit;
            MouseEntered += OnMouseEnter;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb)
            {
                if (CurrentItem == null) return;
                if (mb.Pressed)
                {
                    if (mb.AltPressed)
                    {
                        switch (true)
                        {
                            case var _ when mb.ButtonIndex == MouseButton.Left:
                                RaiseEvent(MouseInteractions.AltRMB);
                                break;
                            case var _ when mb.ButtonIndex == MouseButton.Right:
                                RaiseEvent(MouseInteractions.AltRMB);
                                break;
                        }
                    }

                    if (mb.CtrlPressed)
                    {
                        switch (true)
                        {
                            case var _ when mb.ButtonIndex == MouseButton.Left:
                                RaiseEvent(MouseInteractions.CtrLMB);
                                break;
                            case var _ when mb.ButtonIndex == MouseButton.Right:
                                RaiseEvent(MouseInteractions.CtrRMB);
                                break;
                        }
                    }
                    if (mb.ButtonIndex == MouseButton.Right)
                        ShowButtonsTooltip();

                    AcceptEvent();
                }
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (_rmbWasPressed && @event.IsActionPressed("ui_cancel"))
            {
                Clear();
                GetViewport().SetInputAsHandled();
            }
        }

        public void SetUIElementProvider(IGameMessageBus mediator)
        {
            _gameMessageBus = mediator;
        }

        private void Clear()
        {
            _rmbWasPressed = false;
            _detailsShowing = false;
        }

        private void RaiseEvent(MouseInteractions interaction) => ItemInteraction?.Invoke(this, interaction);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        protected override void RefreshUI()
        {
            base.RefreshUI();
            if (QuantityLabel != null) QuantityLabel.Text = Quantity > 1 ? Quantity.ToString() : string.Empty;
        }

        private async void OnMouseEnter()
        {
            _isMouseInside = true;

            await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);

            if (_isMouseInside && CurrentItem != null)
            {
                _gameMessageBus?.PublishAsync(new ShowInventoryItemEvent(CurrentItem, this));
                _detailsShowing = true;
            }
        }

        private void OnMouseExit()
        {
            _isMouseInside = false;
            if (_rmbWasPressed) return;
            _detailsShowing = false;
            _gameMessageBus?.PublishAsync(new ClearUiElementsEvent(this));
        }

        private void ShowButtonsTooltip()
        {
            _rmbWasPressed = true;
            if (!_detailsShowing)
                _gameMessageBus?.PublishAsync(new ShowInventoryItemEvent(CurrentItem!, this));
            _gameMessageBus?.PublishAsync(new ShowInventorySlotButtonsTooltipEvent(this, CurrentItem!.InstanceId));
        }
    }
}
