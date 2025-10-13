namespace Crafting.Source.UIElements
{
    using Godot;

    public abstract partial class ClickableItem : Control
    {
        private bool _clickable = false;

        [Signal] public delegate void LeftClickEventHandler();
        [Signal] public delegate void RightClickEventHandler();
        [Signal] public delegate void CtrLeftClickEventHandler();

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb || !_clickable) return;
            switch (true)
            {
                case var _ when mb.ButtonIndex == MouseButton.Left && mb.CtrlPressed:
                    EmitSignal(SignalName.CtrLeftClick);
                    break;
                case var _ when mb.ButtonIndex == MouseButton.Left && mb.Pressed:
                    EmitSignal(SignalName.LeftClick);
                    break;
                case var _ when mb.ButtonIndex == MouseButton.Right && mb.Pressed:
                    EmitSignal(SignalName.RightClick);
                    break;
            }
            AcceptEvent();
        }

        public virtual void SetClickable(bool clickable) => _clickable = clickable;
    }
}
