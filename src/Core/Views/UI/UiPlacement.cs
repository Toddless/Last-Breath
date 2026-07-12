namespace Core.Views.UI
{
    using Godot;

    /// <summary>Keeps floating UI fully on screen (tooltips, context popups, draggable windows).</summary>
    public static class UiPlacement
    {
        /// <summary>Positions the panel at the desired global point, clamped so it never sticks out of the viewport.</summary>
        public static void PlaceClamped(Control panel, Vector2 desiredGlobalPosition, Vector2 offset = default)
        {
            if (!panel.IsInsideTree()) return; // the viewport is unknown outside the tree — a caller raced ahead of AddChild

            panel.Hide();
            var viewport = panel.GetViewportRect().Size;
            var size = panel.Size;
            var target = desiredGlobalPosition + offset;
            target.X = Mathf.Clamp(target.X, 0, Mathf.Max(0, viewport.X - size.X));
            target.Y = Mathf.Clamp(target.Y, 0, Mathf.Max(0, viewport.Y - size.Y));
            panel.GlobalPosition = target;
            panel.Show();
        }
    }
}
