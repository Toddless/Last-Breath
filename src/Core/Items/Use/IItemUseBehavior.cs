namespace Core.Items.Use
{
    /// <summary>
    /// One "use from the bag" behavior for a family of items (recipe scrolls, future flasks and
    /// buff scrolls). Modules implement and register behaviors; the shared UseItemMessage channel
    /// and the tooltip Use button are already wired — new consumables only add a behavior.
    /// </summary>
    public interface IItemUseBehavior
    {
        /// <summary>Localization key for the tooltip button caption (Learn / Drink / Use...).</summary>
        string LabelKey { get; }

        /// <summary>Whether this behavior owns the item kind at all.</summary>
        bool CanHandle(IItem item);

        /// <summary>Whether a use would currently do something — the UI disables the button on
        /// false, but <see cref="Use"/> stays the real gate and must refuse on its own.</summary>
        bool CanUse(IItem item);

        /// <summary>Perform the use. Consuming the item is the BEHAVIOR's decision (a duplicate
        /// recipe survives to be sold; a flask would always be drunk).</summary>
        void Use(IItem item);
    }
}
