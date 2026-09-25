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
        /// <summary>Channel captions, shared with the window's forecast lines — one wording per channel.</summary>
        public const string UpgradeChannelKey = "UI_Mastery_Channel_Upgrade";
        public const string ValuesChannelKey = "UI_Mastery_Channel_Values";
        public const string RarityChannelKey = "UI_Mastery_Channel_Rarity";
        public const string EffectChannelKey = "UI_Mastery_Channel_Effect";
        public const string MythicChannelKey = "UI_Mastery_Channel_Mythic";
        public const string SalvageChannelKey = "UI_Mastery_Channel_Salvage";

        private const string MasteryTitleKey = "UI_Craft_Mastery";
        private const string MaxLevelKey = "UI_Mastery_Max";

        [Export] private Label? _title;
        [Export] private Label? _level;
        [Export] private ProgressBar? _xpBar;
        [Export] private Label? _xpLabel;
        [Export] private GridContainer? _chips;

        public override void _Ready() => _title?.Text = Localization.Localize(MasteryTitleKey).ToUpper();

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
                : Localization.Localize(MaxLevelKey);

            _chips.QueueFreeChildren();

            float progress = mastery.MaximumLevel <= 0
                ? 0f
                : Mathf.Clamp((mastery.CurrentLevel + mastery.BonusLevel) / (float)mastery.MaximumLevel, 0f, 1f);
            foreach ((string key, float bonus) in Channels(mastery))
            {
                var chip = MasteryChip.Initialize().Instantiate<MasteryChip>();
                chip.Set(Localization.Localize(key), $"+{CraftingFormat.PercentText(bonus)}", progress);
                _chips.AddChild(chip);
            }
        }

        private static IEnumerable<(string Key, float Bonus)> Channels(ICraftingMastery mastery) =>
        [
            (UpgradeChannelKey, mastery.GetUpgradeChanceBonus()),
            (ValuesChannelKey, mastery.GetCurrentValueMultiplier() - 1f),
            (RarityChannelKey, mastery.GetRarityChanceBonus()),
            (EffectChannelKey, mastery.GetExtraEffectChanceBonus()),
            (MythicChannelKey, mastery.GetMythicModifierChanceBonus()),
            (SalvageChannelKey, mastery.GetResourceReturnBonus()),
        ];

    }
}
