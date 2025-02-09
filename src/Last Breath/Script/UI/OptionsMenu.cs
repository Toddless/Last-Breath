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

            _exitButton = GetPathToButton("ExitBtn");
            _saveButton = GetPathToButton("SaveBtn");
            _saveButton.Pressed += SaveButtonPressed;
            _exitButton.Pressed += ExitButtonPressed;
            // for performance purpose
            SetProcess(false);
        }

        private Button GetPathToButton(string name) => GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<Button>(name);

        private void SaveButtonPressed() => EmitSignal(SignalName.SavePressed);
        private void ExitButtonPressed() => EmitSignal(SignalName.ExitPressed);
    }
}
