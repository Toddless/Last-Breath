namespace Crafting.Source.UIElements.Modules
{
    using Core.Localization;

    /// <summary>Shared number formatting of the crafting bench: percent chips and the
    /// "data base struck through → real chance" forecast values.</summary>
    public static class CraftingFormat
    {
        /// <summary>Sub-10% chances keep one decimal, so early mastery does not round to a flat lie.</summary>
        public static string PercentText(float value) =>
            (value * 100f).ToString(value < 0.095f ? "0.#" : "0", System.Globalization.CultureInfo.InvariantCulture) + "%";

        /// <summary>The data base struck through, the real chance highlighted next to it.</summary>
        public static string WasNow(float before, float now) =>
            $"[s][color={TextPalette.Muted}]{PercentText(before)}[/color][/s] → {Highlight(PercentText(now))}";

        public static string Highlight(string text) => $"[color={TextPalette.Number}]{text}[/color]";
    }
}
