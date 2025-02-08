namespace Playground.Script
{
    using Godot;

    public partial class OptionsMenu : Control
    {
        private Button? _exitButton;

        [Signal]
        public delegate void ExitPressedEventHandler();

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));

            _exitButton = root.GetNode<Button>("ExitBtn");

            _exitButton.Pressed += ExitButtonPressed;

            SetProcess(false);
        }

        private void ExitButtonPressed() => EmitSignal(SignalName.ExitPressed);
    }
}
