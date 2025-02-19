namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class SaveLoadMenu : Control
    {
        private Button? _returnButton;

        [Signal]
        public delegate void ReturnPressedEventHandler();


        public override void _Ready()
        {
            _returnButton = (Button?)NodeFinder.FindBFSCached(this, "Return");
            NodeFinder.ClearCache();
            _returnButton!.Pressed += ReturnButtonPressed;
        }

        private void ReturnButtonPressed() => EmitSignal(SignalName.ReturnPressed);
    }
}
