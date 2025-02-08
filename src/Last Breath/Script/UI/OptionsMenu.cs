namespace Playground.Script
{
    using Godot;

    public partial class OptionsMenu : Control
    {
        private Button? _exitButton, _saveButton;

        [Signal]
        public delegate void ExitPressedEventHandler();
        [Signal]
        public delegate void SavePressedEventHandler();

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer));

            _exitButton = root.GetNode<Button>("ExitBtn");
            _saveButton = root.GetNode<Button>("SaveBtn");
            _saveButton.Pressed += SaveButtonPressed;
            _exitButton.Pressed += ExitButtonPressed;
            // for performance purpose
            SetProcess(false);
        }

        private void SaveButtonPressed() => EmitSignal(SignalName.SavePressed);
        private void ExitButtonPressed() => EmitSignal(SignalName.ExitPressed);
    }
}
