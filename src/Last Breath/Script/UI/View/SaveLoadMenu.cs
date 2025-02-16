namespace Playground.Script.UI
{
    using Godot;

    public partial class SaveLoadMenu : Control
    {
        private Button? _returnButton;

        [Signal]
        public delegate void ReturnPressedEventHandler();


        public override void _Ready()
        {
            _returnButton = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<Button>(nameof(Button));

            _returnButton.Pressed += ReturnButtonPressed;
        }

        private void ReturnButtonPressed() => EmitSignal(SignalName.ReturnPressed);
    }
}
