namespace Core.Views.UI
{
    /// <summary>
    /// Where on the Overlay layer an element WANTS to be. The element only declares the intent;
    /// the layer owns the region's actual screen position and how neighbours stack inside it
    /// (region container nodes in the scene — tweak placement in the editor, not in code).
    /// </summary>
    public enum OverlayRegion
    {
        /// <summary>Free placement at a point the caller provides (tooltips near the cursor).</summary>
        Cursor,

        /// <summary>Prominent announcements: entering a location, boss encounters.</summary>
        TopCenter,

        /// <summary>System feed: saved, level up, ability unlocked.</summary>
        TopRight,

        /// <summary>Reserved for hints/prompts above the hotbar.</summary>
        BottomRight,
    }
}
