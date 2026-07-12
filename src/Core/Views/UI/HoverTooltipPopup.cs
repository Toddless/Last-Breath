namespace Core.Views.UI
{
    using Godot;

    /// <summary>Hover tooltip contract for <see cref="HoverTooltip.Attach"/>: an Alt-pinned
    /// tooltip survives leaving the source until the next show or an overlay clear.</summary>
    public interface IHoverTooltipPopup : IPopup
    {
        bool IsPinned { get; }
    }

    /// <summary>
    /// Shared behavior of hover tooltips (cursor follow, Alt-pin, mouse transparency), used by
    /// COMPOSITION: a Godot-derived base class living in Core double-registers in the engine's
    /// script map on assembly reload (both game assemblies subclass it), so the concrete popups
    /// derive Control in their own assembly and call these from their lifecycle overrides.
    /// </summary>
    public static class HoverTooltipMotion
    {
        private const float CursorOffset = 16;

        /// <summary>_Process part: the panel follows the cursor, clamped to the viewport.</summary>
        public static void Setup(Control root, Control? panel)
        {
            root.MouseFilter = Control.MouseFilterEnum.Ignore;
            panel?.MouseFilter = Control.MouseFilterEnum.Ignore;
            Follow(root, panel);
        }

        /// <summary>_Process part: the panel follows the cursor, clamped to the viewport.</summary>
        public static void Follow(Control root, Control? panel)
        {
            if (panel == null) return;
            UiPlacement.PlaceClamped(panel, root.GetGlobalMousePosition(), new Vector2(CursorOffset, CursorOffset));
        }

        /// <summary>_Process part: the panel follows the cursor, clamped to the viewport.</summary>
        public static bool TogglePin(InputEvent @event, bool pinned) =>
            @event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Alt } ? !pinned : pinned;
    }
}
