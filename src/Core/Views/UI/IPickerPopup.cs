namespace Core.Views.UI
{
    using System;
    using System.Collections.Generic;
    using Godot;

    /// <summary>One row of a picker: what it is called, what it looks like, and the id it hands back
    /// when it is chosen. The id is opaque to the popup — an item instance for one caller, a record id
    /// for another — because the list is the only thing a picker knows about what it is picking.</summary>
    public readonly record struct PickerEntry(
        string Id,
        string Label,
        Texture2D? Icon,
        string? Tooltip = null,
        Color? LabelColor = null);

    /// <summary>
    /// A list of candidates at the cursor: one click picks, everything else closes. Declared beside the
    /// UI contracts rather than beside the scene that draws it because more than one module opens one —
    /// crafting picks a piece out of the bag, a socket picks an augment — and a module cannot name a
    /// class living in another module's assembly. The project's bootstrap registers whatever picker it
    /// has under this type, the same arrangement the keyword tooltip already uses.
    /// </summary>
    public interface IPickerPopup : IPopup
    {
        /// <summary>Fills the list and names what is being picked. The callback is handed the chosen
        /// entry's own <see cref="PickerEntry.Id"/>; a picker closed without a choice calls nothing.</summary>
        void Present(string title, IReadOnlyList<PickerEntry> entries, Action<string> onPicked);
    }
}
