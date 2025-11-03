namespace Crafting.Source.UIElements
{
    using Godot;
    using System;

    public partial class DraggableWindow : PanelContainer
    {
        private bool _isDragging = false;
        private Vector2 _dragOffset;

        [Export] protected Control? DragArea;

        public event Action<Vector2>? PositionChangedExternally;

        protected void DragWindow(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    if (GetViewport().GetVisibleRect().HasPoint(mb.Position))
                    {
                        _isDragging = true;
                        _dragOffset = mb.Position;
                    }
                }
                else
                    _isDragging = false;
            }

            if (@event is InputEventMouseMotion mm && _isDragging)
            {
                Position += (Vector2I)mm.Position - _dragOffset;
                ClampToViewport();
            }
        }

        private void ClampToViewport()
        {
            var viewPortSize = GetViewport().GetVisibleRect().Size;
            Position = Position.Clamp(Vector2.Zero, viewPortSize - Size);
            PositionChangedExternally?.Invoke(Position);
        }
    }
}
