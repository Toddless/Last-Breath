namespace Crafting.Source.UIElements.Modules
{
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>The always-visible mastery strip over the mode tabs: level with the XP progress to
    /// the next one, then the six bonus channels as chips. Every channel lerps on the same level
    /// factor, so each chip's mini bar IS that factor.</summary>
    [GlobalClass]
    public partial class MasteryRail : HBoxContainer
    {
        [Export] private Label? _title;
        [Export] private Label? _level;
        [Export] private ProgressBar? _xpBar;
        [Export] private Label? _xpLabel;
        [Export] private GridContainer? _chips;

        public override void _Ready() => _title?.Text = Localization.Localize("UI_Craft_Mastery").ToUpper();

        /// <summary>Repaints the whole rail from the current mastery state.</summary>
        public void Refresh(ICraftingMastery mastery)
        {
            if (_chips == null) return;

            _level?.Text = mastery.BonusLevel > 0
                ? $"{mastery.CurrentLevel}+{mastery.BonusLevel} / {mastery.MaximumLevel}"
                : $"{mastery.CurrentLevel} / {mastery.MaximumLevel}";

            int expTotal = mastery.ExpToNextLevelTotal();
            _xpBar?.Value = expTotal > 0 ? mastery.CurrentExperience / (float)expTotal : 1f;
            _xpLabel?.Text = expTotal > 0
                ? $"{mastery.CurrentExperience} / {expTotal}"
                : Localization.Localize("UI_Mastery_Max");

            _chips.QueueFreeChildren();

            float progress = mastery.MaximumLevel <= 0
                ? 0f
                : Mathf.Clamp((mastery.CurrentLevel + mastery.BonusLevel) / (float)mastery.MaximumLevel, 0f, 1f);
            foreach ((string key, float bonus) in Channels(mastery))
                _chips.AddChild(Chip(Localization.Localize(key), bonus, progress));
        }

        private static IEnumerable<(string Key, float Bonus)> Channels(ICraftingMastery mastery) =>
        [
            ("UI_Mastery_Channel_Upgrade", mastery.GetUpgradeChanceBonus()),
            ("UI_Mastery_Channel_Values", mastery.GetCurrentValueMultiplier() - 1f),
            ("UI_Mastery_Channel_Rarity", mastery.GetRarityChanceBonus()),
            ("UI_Mastery_Channel_Effect", mastery.GetExtraEffectChanceBonus()),
            ("UI_Mastery_Channel_Mythic", mastery.GetMythicModifierChanceBonus()),
            ("UI_Mastery_Channel_Salvage", mastery.GetResourceReturnBonus()),
        ];

        private static Control Chip(string name, float bonus, float progress)
        {
            var chip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_top", 6);
            margin.AddThemeConstantOverride("margin_right", 8);
            margin.AddThemeConstantOverride("margin_bottom", 6);
            chip.AddChild(margin);

            var content = new VBoxContainer();
            content.AddThemeConstantOverride("separation", 2);
            margin.AddChild(content);

            var title = new Label
            {
                Text = name,
                ThemeTypeVariation = "DimLabel",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            title.AddThemeFontSizeOverride("font_size", 10);
            content.AddChild(title);

            var value = new Label { Text = $"+{CraftingFormat.PercentText(bonus)}" };
            value.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(value);

            content.AddChild(new ProgressBar
            {
                CustomMinimumSize = new Vector2(0, 3),
                MaxValue = 1.0,
                Step = 0.001,
                Value = progress,
                ShowPercentage = false,
            });
            return chip;
        }
    }
}
