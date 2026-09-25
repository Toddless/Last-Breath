namespace Crafting.Internal.Inventory
{
    using System;
    using Core.Enums;
    using Core.Inventory;
    using Core.Views.UI;
    using Godot;

    internal partial class InventorySlot : Slot, IInventorySlot, IInitializable
    {
        private const string UID = "uid://cekpl68hghs2v";
        private bool _isMouseInside, _rmbWasPressed, _detailsShowing;
        [Export] protected Label? QuantityLabel;

        public event Action<IInventorySlot, MouseInteractions>? ItemInteraction;

        // only show amount items, icon
        // notify about interactions (clicks, drag and drop, etc.)

        public override void _Ready()
        {
            MouseExited += OnMouseExit;
            MouseEntered += OnMouseEnter;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb) return;
            if (CurrentItem == null) return;
            if (!mb.Pressed) return;

            if (mb.AltPressed)
            {
                switch (true)
                {
                    case var _ when mb.ButtonIndex == MouseButton.Left:
                        RaiseEvent(MouseInteractions.AltLmb);
                        break;
                    case var _ when mb.ButtonIndex == MouseButton.Right:
                        RaiseEvent(MouseInteractions.AltRmb);
                        break;
                }
            }

            if (mb.CtrlPressed)
            {
                switch (true)
                {
                    case var _ when mb.ButtonIndex == MouseButton.Left:
                        RaiseEvent(MouseInteractions.CtrLmb);
                        break;
                    case var _ when mb.ButtonIndex == MouseButton.Right:
                        RaiseEvent(MouseInteractions.CtrRmb);
                        break;
                }
            }
            AcceptEvent();
        }

        public override void _Input(InputEvent @event)
        {
            if (!_rmbWasPressed || !@event.IsActionPressed("ui_cancel")) return;
            Clear();
            GetViewport().SetInputAsHandled();
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
            QuantityLabel?.Text = Quantity > 1 ? Quantity.ToString() : string.Empty;
        }

        private async void OnMouseEnter()
        {
            _isMouseInside = true;

            await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);

            if (_isMouseInside && CurrentItem != null)
            {
                _detailsShowing = true;
            }
        }

        private void OnMouseExit()
        {
            _isMouseInside = false;
            if (_rmbWasPressed) return;
            _detailsShowing = false;
        }
    }
}
