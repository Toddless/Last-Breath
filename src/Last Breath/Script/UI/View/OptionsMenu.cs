namespace Playground.Script
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class OptionsMenu : Control
    {
        private Button? _returnButton, _saveButton;

        [Signal]
        public delegate void ReturnPressedEventHandler();
        [Signal]
        public delegate void SavePressedEventHandler();

        public override void _Ready()
        {
            _returnButton = (Button?)NodeFinder.FindBFSCached(this, "ExitBtn");
            _saveButton = (Button?)NodeFinder.FindBFSCached(this, "SaveBtn");
            _saveButton.Pressed += SaveButtonPressed;
            _returnButton.Pressed += ReturnButtonPressed;
            NodeFinder.ClearCache();
        }

        private void SaveButtonPressed() => EmitSignal(SignalName.SavePressed);
        private void ReturnButtonPressed() => EmitSignal(SignalName.ReturnPressed);
    }
}
