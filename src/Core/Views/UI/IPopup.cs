namespace Core.Views.UI
{

    /// <summary>How long an Overlay element lives.</summary>
    public enum PopupLifetime
    {
        /// <summary>Dismisses itself after a delay (notifications).</summary>
        Timed,

        /// <summary>Lives while the pointer stays on the source; follows the cursor, Alt pins it (item tooltips).</summary>
        WhileHovered,

        /// <summary>Stays until closed explicitly: ✕ button, Esc or a click outside (reference cards).</summary>
        Pinned,
    }

    /// <summary>
    /// An element of the Overlay layer (tooltips, popups, notifications). Fresh-instance policy:
    /// every show creates a new instance, closing frees it; showing a popup of the same type
    /// replaces the previous one, different types coexist.
    /// </summary>
    public interface IPopup : IInitializable
    {
        public PopupLifetime Lifetime { get; }

        /// <summary>The element's placement intent; the layer decides the actual geometry.</summary>
        public OverlayRegion Region { get; }

        /// <summary>Closing means dying: implementations QueueFree themselves.</summary>
        public void Close();
    }
}
