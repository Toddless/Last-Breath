namespace Crafting.Source.UIElements
{
    using Godot;

    [GlobalClass]
    public partial class InteractiveLabel : Label
    {
        [Export] private StyleBoxFlat? _styleHovered, _styleNormal;

        [Signal]
        public delegate void SelectedEventHandler(int identifier);

        protected bool IsHovered
        {
            get;
            set
            {
                field = value;
                UpdateState();
            }
        }

        public bool Selectable { get; set; } = false;

        public int Identifier { get; set; }

        public override void _Ready()
        {
            MouseEntered += () => IsHovered = true;
            MouseExited += () => IsHovered = false;
            //AddThemeStyleboxOverride("normal", _styleNormal);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton mb) return;
            if (mb is { ButtonIndex: MouseButton.Left, Pressed: true } && Selectable)
            {
                EmitSignal(global::Crafting.Source.UIElements.InteractiveLabel.SignalName.Selected, Identifier);
                AcceptEvent();
            }
        }

        private void UpdateState()
        {
            if (!Selectable) return;

            CallDeferred(Control.MethodName.AddThemeStyleboxOverride, "panel", IsHovered ? _styleHovered! : _styleNormal!);
        }
    }
}
