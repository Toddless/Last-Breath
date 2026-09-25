namespace Core.Views.UI
{
    using Godot;

    /// <summary>Keeps floating UI fully on screen (tooltips, context popups, draggable windows).</summary>
    public static class UiPlacement
    {
        /// <summary>One-shot placement: the panel is hidden until it stands at its clamped position, then shown.</summary>
        public static void PlaceClamped(Control panel, Vector2 desiredGlobalPosition, Vector2 offset = default)
        {
            if (!panel.IsInsideTree()) return; // the viewport is unknown outside the tree — a caller raced ahead of AddChild

            panel.Hide();
            Follow(panel, desiredGlobalPosition, offset);
            panel.Show();
        }

        /// <summary>Moves the panel to the desired global point, clamped to the viewport. Visibility is left untouched,
        /// so keyboard focus and tooltips inside the panel survive per-frame tracking.</summary>
        public static void Follow(Control panel, Vector2 desiredGlobalPosition, Vector2 offset = default)
        {
            if (!panel.IsInsideTree()) return;

            var viewport = panel.GetViewportRect().Size;
            var size = panel.Size;
            var target = desiredGlobalPosition + offset;
            target.X = Mathf.Clamp(target.X, 0, Mathf.Max(0, viewport.X - size.X));
            target.Y = Mathf.Clamp(target.Y, 0, Mathf.Max(0, viewport.Y - size.Y));
            panel.GlobalPosition = target;
        }
    }
}
