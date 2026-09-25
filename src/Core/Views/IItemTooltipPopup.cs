namespace Core.Views
{
    using Items;
    using UI;

    /// <summary>
    /// The full framed item card (icon, rarity-coloured name, implicits/mods/effect sections) as a
    /// cross-module contract: the scene lives in the game project, but pickers assembled in other
    /// modules want the SAME card instead of a plain-text stand-in. The project's bootstrap registers
    /// its item tooltip scene under this type — the arrangement the keyword tooltip already uses.
    /// </summary>
    public interface IItemTooltipPopup : IPopup
    {
        void ShowItem(IItem item);
    }
}
