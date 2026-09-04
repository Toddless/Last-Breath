namespace Core.Views.UI
{
    using Enums;
    using Godot;
    using Localization;

    /// <summary>The shared display convention for an item's rolled lines: how a slot family announces itself
    /// above its block. Both panels that list lines (item tooltip, crafting bench) build their rows themselves
    /// but take the block headers from here, so the two can never drift apart.</summary>
    public static class ItemLineRows
    {
        // Smaller and dimmer than a section header: these caption blocks INSIDE the modifiers section,
        // they don't open one. The gap is what actually separates the blocks — the caption alone reads
        // as just another row.
        private const int HeaderFontSize = 10;
        private const int HeaderGap = 8;

        /// <summary>Caption for a block of lines, or null when the family shows no caption — the family-less
        /// leftovers (legacy saves, authored fodder) sit under the suffixes as a bare tail.
        /// <paramref name="count"/> (pool tables) appends a rule and the block's entry count on the right.</summary>
        public static Control? AffixHeader(AffixKind affix, int? count = null)
        {
            string? key = ItemTooltipText.AffixHeaderKey(affix);
            if (key == null) return null;

            var label = new Label { Text = Localization.Localize(key).ToUpper(), ThemeTypeVariation = "DimLabel" };
            label.AddThemeFontSizeOverride("font_size", HeaderFontSize);

            var spaced = new MarginContainer();
            spaced.AddThemeConstantOverride("margin_top", HeaderGap);
            if (count == null)
            {
                spaced.AddChild(label);
                return spaced;
            }

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(label);
            row.AddChild(new HSeparator { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            var countLabel = new Label { Text = count.Value.ToString(), ThemeTypeVariation = "DimLabel" };
            countLabel.AddThemeFontSizeOverride("font_size", HeaderFontSize);
            row.AddChild(countLabel);
            spaced.AddChild(row);
            return spaced;
        }
    }
}
