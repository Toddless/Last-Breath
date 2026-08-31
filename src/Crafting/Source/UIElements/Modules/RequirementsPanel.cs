namespace Crafting.Source.UIElements.Modules
{
    using System;
    using System.Collections.Generic;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>One requirement card: a fixed row shows have/need; a card carrying a category id is
    /// a clickable slot inviting a concrete resource pick.</summary>
    public sealed record RequirementCardView(Texture2D? Icon, string Name, string? Count, bool CountMet, string? CategoryId = null);

    /// <summary>One additive slot: a null name renders the empty slot's lone "+".</summary>
    public sealed record AdditiveSlotView(Texture2D? Icon, string? Name);

    /// <summary>The requirement column of the bench: requirement cards, the three additive slots,
    /// the nested forecast box and the ascension warning. Slot clicks travel up as events — the
    /// window owns the choices and the pickers.</summary>
    [GlobalClass]
    public partial class RequirementsPanel : VBoxContainer
    {
        private static readonly Vector2 s_cardMinSize = new(0, 44);

        [Export] private Label? _header;
        [Export] private VBoxContainer? _requirements;
        [Export] private Label? _additivesHeader;
        [Export] private VBoxContainer? _additives;
        [Export] private ForecastPanel? _forecast;
        [Export] private Label? _ascendWarning;

        /// <summary>The forecast box nested between the additives and the ascension warning.</summary>
        public ForecastPanel? Forecast => _forecast;

        /// <summary>A category slot was clicked; carries the requirement's category id.</summary>
        public event Action<string>? CategorySlotClicked;

        /// <summary>An additive slot was clicked; carries the slot index.</summary>
        public event Action<int>? AdditiveSlotClicked;

        public override void _Ready()
        {
            _header?.Text = Localization.Localize("UI_Craft_Requirements");
            _additivesHeader?.Text = Localization.Localize("UI_Craft_Additives");
            _ascendWarning?.Text = Localization.Localize("UI_Craft_AscendWarning");
        }

        /// <summary>Repaints the requirement cards top to bottom.</summary>
        public void SetRequirements(IReadOnlyList<RequirementCardView> cards, bool ascendWarningVisible)
        {
            if (_requirements == null) return;

            _requirements.QueueFreeChildren();
            foreach (var card in cards)
                _requirements.AddChild(card.CategoryId == null ? StaticCard(card) : SlotCard(card));
            _ascendWarning?.Visible = ascendWarningVisible;
        }

        /// <summary>Repaints the additive slots; an empty list hides the whole block.</summary>
        public void SetAdditives(IReadOnlyList<AdditiveSlotView> slots)
        {
            if (_additives == null) return;

            _additives.QueueFreeChildren();
            bool visible = slots.Count > 0;
            _additivesHeader?.Visible = visible;
            _additives.Visible = visible;
            for (int slot = 0; slot < slots.Count; slot++)
                _additives.AddChild(AdditiveCard(slot, slots[slot]));
        }

        private static Control StaticCard(RequirementCardView view)
        {
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = s_cardMinSize };
            card.AddChild(CardContent(view.Icon, view.Name, view.Count, view.CountMet));
            return card;
        }

        /// <summary>A category requirement is a slot: click → pick a concrete resource carrying the tag.</summary>
        private Control SlotCard(RequirementCardView view)
        {
            var card = SlotButton();
            card.AddChild(CardContent(view.Icon, view.Name, view.Count, view.CountMet));
            card.Pressed += () => CategorySlotClicked?.Invoke(view.CategoryId!);
            return card;
        }

        private Control AdditiveCard(int slot, AdditiveSlotView view)
        {
            var card = SlotButton();
            card.AddChild(view.Name == null
                ? CardContent(null, "+", null, countMet: true)
                : CardContent(view.Icon, view.Name, null, countMet: true));
            card.Pressed += () => AdditiveSlotClicked?.Invoke(slot);
            return card;
        }

        /// <summary>Clickable slot card: the button is the frame, the content overlays it mouse-transparently.</summary>
        private static Button SlotButton() => new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = s_cardMinSize,
            FocusMode = FocusModeEnum.None,
        };

        /// <summary>Compact horizontal slot-row content: icon on the left, name, have/need on the
        /// right. The row itself is the shared IconLabelRow; the margin and mouse transparency stay
        /// this card's concerns.</summary>
        private static Control CardContent(Texture2D? icon, string name, string? count, bool countMet)
        {
            var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            margin.SetAnchorsPreset(LayoutPreset.FullRect);
            margin.AddThemeConstantOverride("margin_left", 8);
            margin.AddThemeConstantOverride("margin_right", 8);
            margin.AddThemeConstantOverride("margin_top", 4);
            margin.AddThemeConstantOverride("margin_bottom", 4);

            var row = SharedUi.IconLabelRow.Initialize().Instantiate<SharedUi.IconLabelRow>();
            // The empty additive slot is a lone "+" — center it; real rows read left to right.
            row.Alignment = icon == null && count == null ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin;
            row.Set(icon, name);
            if (count != null)
            {
                row.SetTrailing(new Label
                {
                    Text = count,
                    VerticalAlignment = VerticalAlignment.Center,
                    ThemeTypeVariation = countMet ? null : "DimLabel",
                });
            }

            row.MakeMouseTransparent();
            margin.AddChild(row);
            return margin;
        }
    }
}
