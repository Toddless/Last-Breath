namespace Playground.Script
{
    using Godot;

    public partial class OptionsMenu : Control
    {
        private Button? _returnButton, _saveButton;

        [Signal]
        public delegate void ReturnPressedEventHandler();
        [Signal]
        public delegate void SavePressedEventHandler();

        public override void _Ready()
        {
            _returnButton = GetPathToButton("ExitBtn");
            _saveButton = GetPathToButton("SaveBtn");
            _saveButton.Pressed += SaveButtonPressed;
            _returnButton.Pressed += ReturnButtonPressed;
        }
        public override void _GuiInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
                GD.Print("Call from OptionsMenu.cs");
        }

        private Button GetPathToButton(string name) => GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<Button>(name);

        private void SaveButtonPressed() => EmitSignal(SignalName.SavePressed);
        private void ReturnButtonPressed() => EmitSignal(SignalName.ReturnPressed);
    }
}
