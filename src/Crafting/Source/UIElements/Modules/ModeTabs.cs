namespace Crafting.Source.UIElements.Modules
{
    using System;
    using Core.Enums;
    using Core.Localization;
    using Godot;

    /// <summary>The four mode tabs (Create / Upgrade / Reroll / Ascend). A press travels up as an
    /// event; the pressed state only mirrors what the window decided, without echoing it back.</summary>
    [GlobalClass]
    public partial class ModeTabs : HBoxContainer
    {
        /// <summary>Tab captions, shared with the tooltip buttons that open the same modes.</summary>
        public const string CreateTabKey = "UI_Crafting_Create";
        public const string UpgradeTabKey = "UI_Crafting_Upgrade";
        public const string RecraftTabKey = "UI_Crafting_Recraft";
        public const string AscendTabKey = "UI_Crafting_Ascend";

        [Export] private Button? _create, _upgrade, _recraft, _ascend;

        public event Action<CraftingMode>? ModeChanged;

        public override void _Ready()
        {
            _create?.Text = Localization.Localize(CreateTabKey);
            _upgrade?.Text = Localization.Localize(UpgradeTabKey);
            _recraft?.Text = Localization.Localize(RecraftTabKey);
            _ascend?.Text = Localization.Localize(AscendTabKey);

            _create?.Pressed += () => ModeChanged?.Invoke(CraftingMode.Create);
            _upgrade?.Pressed += () => ModeChanged?.Invoke(CraftingMode.Upgrade);
            _recraft?.Pressed += () => ModeChanged?.Invoke(CraftingMode.Recraft);
            _ascend?.Pressed += () => ModeChanged?.Invoke(CraftingMode.Ascend);
        }

        /// <summary>Mirrors the window's mode onto the toggle states.</summary>
        public void SetMode(CraftingMode mode)
        {
            Sync(_create, mode == CraftingMode.Create);
            Sync(_upgrade, mode == CraftingMode.Upgrade);
            Sync(_recraft, mode == CraftingMode.Recraft);
            Sync(_ascend, mode == CraftingMode.Ascend);
        }

        private static void Sync(Button? tab, bool pressed) => tab?.SetPressedNoSignal(pressed);
    }
}
