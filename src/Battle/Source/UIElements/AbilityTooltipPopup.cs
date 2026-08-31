namespace Battle.Source.UIElements
{
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;
    using SharedUi;

    /// <summary>
    /// The ability card as the Umbral "scroll" draws it: the framed icon beside the gold name and its
    /// genus, the target/cost/cooldown pairs, the tag plates, the live description and — where the
    /// domain ever words them — the numbered stages. Serves EVERY surface that shows an ability
    /// (battle bar, socket sheet, wheel ability nodes); everything that is not an ability stays on
    /// <see cref="TextTooltipPopup"/>.
    /// <para>Every part a card does not carry hides whole, header included, and the column shrinks to
    /// what is left — the width alone is fixed, which is also what keeps autowrap from inflating the
    /// height.</para>
    /// </summary>
    [GlobalClass]
    public partial class AbilityTooltipPopup : Control, IHoverTooltipPopup
    {
        private const string ScenePath = "uid://bd4hw6yj2p8ka";

        private const string TargetCaptionKey = "UI_AbilityTarget";
        private const string CostCaptionKey = "UI_AbilityCostCaption";
        private const string CooldownCaptionKey = "UI_AbilityCooldownCaption";
        private const string TagsSectionKey = "UI_AbilityTags";
        private const string StagesSectionKey = "UI_AbilityStages";

        // The pool tints of the cost value. Neither the theme nor TextPalette words these three today
        // (the mana blue exists nowhere else); once a channel palette lands they move there.
        private static readonly Color s_manaTint = new("6f9fd8");
        private static readonly Color s_barrierTint = new("d8c46f");
        private static readonly Color s_healthTint = new("c05050");

        [Export] private PanelContainer? _panel;
        [Export] private FramedIconSlot? _iconSlot;
        [Export] private Label? _name;
        [Export] private Label? _kind;
        [Export] private KeyValueRow? _target;
        [Export] private KeyValueRow? _cost;
        [Export] private KeyValueRow? _cooldown;
        [Export] private SectionHeader? _tagsHeader;
        [Export] private TagRow? _tags;
        [Export] private RichTextLabel? _description;
        [Export] private SectionHeader? _stagesHeader;
        [Export] private VBoxContainer? _stages;
        [Export] private RichTextLabel? _footnote;

        public PopupLifetime Lifetime => PopupLifetime.WhileHovered;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public bool IsPinned { get; private set; }

        public override void _Ready() => HoverTooltipMotion.Setup(this, _panel);

        public override void _Process(double delta)
        {
            if (!IsPinned) HoverTooltipMotion.Follow(this, _panel);
        }

        public override void _UnhandledKeyInput(InputEvent @event) =>
            IsPinned = HoverTooltipMotion.HandlePinInput(this, @event, IsPinned);

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        /// <param name="card">The card, structured — the popup words nothing itself beyond captions.</param>
        /// <param name="icon">The ability's art; null leaves the frame empty, and the frame stays — the
        /// slot is part of the header's shape, not a bonus for abilities that have art.</param>
        /// <param name="footnoteBbcode">What the SURFACE has to add under the card — the wheel's node
        /// lines and its refund arithmetic. Null or empty for the surfaces that have nothing to.</param>
        public void Show(AbilityCard card, Texture2D? icon, string? footnoteBbcode = null)
        {
            _name?.Text = card.Name;
            SetLabel(_kind, card.Kind);
            _iconSlot?.SetIcon(icon);

            SetRow(_target, TargetCaptionKey, card.Target);
            SetRow(_cost, CostCaptionKey,
                card.CostPool is { } pool ? AbilityText.CostValueText(card.CostValue, pool) : string.Empty,
                card.CostPool is { } tinted ? CostTint(tinted) : null);
            SetRow(_cooldown, CooldownCaptionKey, AbilityText.CooldownValueText(card.CooldownTurns));

            SetTags(card);
            SetText(_description, card.Description);
            SetStages(card);
            SetText(_footnote, footnoteBbcode ?? string.Empty);
        }

        /// <summary>What pool the price is drawn from is the one thing the cost line says beyond its
        /// number, and it says it in colour.</summary>
        private static Color CostTint(Costs pool) => pool switch
        {
            Costs.Health => s_healthTint,
            Costs.Barrier => s_barrierTint,
            _ => s_manaTint,
        };

        private static void SetLabel(Label? label, string text)
        {
            if (label == null) return;
            label.Text = text;
            label.Visible = text.Length > 0;
        }

        private static void SetText(RichTextLabel? label, string bbcode)
        {
            if (label == null) return;
            label.Text = bbcode;
            label.Visible = bbcode.Length > 0;
        }

        private void SetRow(KeyValueRow? row, string captionKey, string value, Color? valueColor = null)
        {
            if (row == null) return;
            row.Visible = value.Length > 0;
            if (row.Visible) row.Set(Localization.Localize(captionKey), value, valueColor);
        }

        private void SetTags(AbilityCard card)
        {
            bool any = card.Tags.Count > 0;
            _tagsHeader?.Visible = any;
            if (_tagsHeader != null && any) _tagsHeader.SetTitle(Localization.Localize(TagsSectionKey));
            _tags?.Visible = any;
            if (any) _tags?.SetTags(card.Tags);
        }

        /// <summary>The numbered stages, one row per line and rebuilt whole: a popup is shown once, but
        /// rebuilt-not-diffed is what every dynamic list in the project does.</summary>
        private void SetStages(AbilityCard card)
        {
            bool any = card.Tiers.Count > 0;
            _stagesHeader?.Visible = any;
            if (_stagesHeader != null && any) _stagesHeader.SetTitle(Localization.Localize(StagesSectionKey));
            if (_stages == null) return;

            _stages.Visible = any;
            foreach (var child in _stages.GetChildren()) child.QueueFree();
            if (!any) return;

            var rowScene = TierRow.Initialize();
            for (int i = 0; i < card.Tiers.Count; i++)
            {
                var row = rowScene.Instantiate<TierRow>();
                _stages.AddChild(row);
                row.Set(i + 1, card.Tiers[i]);
            }
        }
    }
}
