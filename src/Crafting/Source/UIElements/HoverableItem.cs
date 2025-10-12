namespace Crafting.Source.UIElements
{
    using Godot;

    [GlobalClass]
    public partial class HoverableItem : Panel
    {
        private bool _isMouseInside;
        [Signal] public delegate void HoveredEventHandler();
        public void Setup()
        {
            MouseEntered += OnMouseEnter;
            MouseExited += OnMouseExit;
            CustomMinimumSize = new Vector2(50, 50);
        }

        private void OnMouseExit() => _isMouseInside = false;

        private async void OnMouseEnter()
        {
            _isMouseInside = true;
            await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
            if (_isMouseInside)
                EmitSignal(SignalName.Hovered);
        }
    }
}
