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
        private const float CursorOffset = 18f;

        /// <summary>_Ready part: the tooltip must never steal hover from its source — the whole
        /// subtree is mouse-transparent.</summary>
        public static void Setup(Control root, Control? panel)
        {
            root.MouseFilter = Control.MouseFilterEnum.Ignore;
            if (panel != null) panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            Follow(root, panel);
        }

        /// <summary>_Process part: the panel follows the cursor, clamped to the viewport.</summary>
        public static void Follow(Control root, Control? panel)
        {
            if (panel != null) UiPlacement.PlaceClamped(panel, root.GetGlobalMousePosition(), new Vector2(CursorOffset, CursorOffset));
        }

        /// <summary>_UnhandledKeyInput part: Alt toggles the pin; returns the new state.</summary>
        public static bool TogglePin(InputEvent @event, bool pinned) =>
            @event is InputEventKey { Pressed: true, Keycode: Key.Alt } ? !pinned : pinned;
    }
}
