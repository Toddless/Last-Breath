namespace Battle.Source.UIElements
{
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>What a cell needs from whoever owns it. One interface rather than five delegates: the
    /// five are one job — the panel's — and a cell handed them separately could be wired with half of
    /// them.</summary>
    public interface IAugmentCellHost
    {
        /// <summary>What an install of that copy into that slot would answer, right now and moving
        /// nothing. Called from the engine's drop check, which is synchronous.</summary>
        AugmentInstallResult Judge(string socketAddress, string itemInstanceId);

        /// <summary>Offers what the bag holds for that slot and seats whatever is chosen. The other way
        /// in, beside the drag: an empty slot is a question, and clicking it is how the player asks it
        /// without hunting through his bag for something that fits.</summary>
        void Pick(string socketAddress);

        /// <summary>Actually moves it, through the bus.</summary>
        void Install(string socketAddress, string itemInstanceId);

        /// <summary>Takes what is in the slot back to the bag, through the bus.</summary>
        void Extract(string socketAddress);

        /// <summary>Why a drop would be refused, for the one line the panel shows during a drag. Empty
        /// clears it.</summary>
        void ShowReason(string text);
    }

    /// <summary>
    /// One augment slot on screen: empty, filled, or a leftover the player may only empty (the node
    /// behind it was refunded and his augment is still in it).
    ///
    /// Accepts a dragged copy and answers the drag from the SAME gate the install goes through, so the
    /// cursor never promises what the bus then refuses for a different reason. Right-click takes the
    /// augment out — the mirror of unequipping. It is not a drag SOURCE: moving an augment from one
    /// slot to another is two operations through two gates, and the bag's own slots resolve a drag
    /// source as one of their own, which this is not.
    /// <para>
    /// The two buttons mean opposite things and each means only one: a left click on an EMPTY slot asks
    /// what could go in it, a right click on a FILLED one takes what is in it back out. A left click on
    /// a filled slot does nothing rather than something clever — the slot is taken, and the way to
    /// change what is in it is to empty it first.
    /// </para>
    /// <para>
    /// Hovering a slot that holds something shows its card. An empty one shows none: what could go into
    /// it is what the left button asks, and the answer to that is a list rather than a card.
    /// </para>
    /// </summary>
    [GlobalClass]
    public partial class AugmentCell : PanelContainer
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://bt5rv8kd1ye3q";

        private static readonly Color s_sealedTint = new(0.55f, 0.55f, 0.55f);

        [Export] private Control? _empty;
        [Export] private TextureRect? _icon;
        [Export] private Label? _tier;
        [Export] private TextureRect? _sealedMark;
        [Export] private TextureRect? _dormantMark;
        [Export] private Control? _highlight;

        private IAugmentCellHost? _host;
        private IUiElementsManager? _windows;
        private AugmentCellView? _view;
        private StyleBox? _emptyStyle;

        /// <summary>The card of whatever is in the slot appears on hover and dies with the cell — a cell
        /// is rebuilt whenever the board moves, and a tooltip outliving the thing it describes would go on
        /// naming an augment that has been taken out.</summary>
        public override void _Ready()
        {
            _emptyStyle = GetThemeStylebox("panel");
            HoverTooltip.Attach(this, ShowTooltip);
        }

        /// <param name="windows">Where the hover card is opened. Optional: a composition without one
        /// seats augments and shows no cards, which is what the sandbox does.</param>
        public void Bind(IAugmentCellHost host, IUiElementsManager? windows)
        {
            _host = host;
            _windows = windows;
        }

        public void SetView(AugmentCellView view)
        {
            _view = view;
            bool filled = view.Kind != AugmentCellKind.Empty;

            if (_empty != null) _empty.Visible = !filled;
            if (_icon != null)
            {
                _icon.Texture = view.Icon;
                _icon.Visible = filled;
            }

            _tier?.Text = view.Tier.ToString();
            if (_sealedMark != null) _sealedMark.Visible = view.Kind == AugmentCellKind.Held;
            if (_dormantMark != null)
                _dormantMark.Visible = view is { Kind: AugmentCellKind.Filled, Activity: AugmentActivity.Dormant };

            ApplyStyle(view, filled);
            SetHighlight(null);
        }

        /// <summary>Whether this slot would take that copy, asked through the host — the same road the
        /// drop check takes, and therefore the same gate. Answered by the cell because the address it is
        /// about is the cell's own, and never by measuring anything: the rule lives in one place and
        /// this is a reading of it.</summary>
        public bool WouldAccept(IAugmentCellHost host, string itemInstanceId) =>
            _view != null && host.Judge(_view.SocketAddress, itemInstanceId).Installed;

        /// <summary>Lights the cell up while a drag is in the air: green where the copy would go in,
        /// red where it would not, nothing at all when no drag is happening.</summary>
        public void SetHighlight(bool? accepted)
        {
            if (_highlight == null) return;

            _highlight.Visible = accepted != null;
            _highlight.Modulate = accepted == true ? Colors.LimeGreen : Colors.IndianRed;
        }

        /// <summary>Answers the drag out of the install gate itself and paints the answer. The reason is
        /// put on the panel's own line rather than into a tooltip: the engine suppresses tooltips while
        /// a drag is in the air, so a tooltip here would never be read.</summary>
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (_view == null || _host == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return false;

            AugmentInstallResult verdict = _host.Judge(_view.SocketAddress, instanceId);
            SetHighlight(verdict.Installed);
            _host.ShowReason(verdict.Installed ? string.Empty : Localization.Localize(AugmentRefusalText.KeyFor(verdict)));
            return verdict.Installed;
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (_view == null || _host == null || !DragPayloadReader.TryReadInstance(data, out string instanceId)) return;

            _host.Install(_view.SocketAddress, instanceId);
        }

        /// <summary>A slot the allocation no longer backs (<see cref="AugmentCellKind.Held"/>) answers
        /// the right button and nothing else: it exists to be emptied, and there is no such thing as an
        /// empty one to offer anything to.</summary>
        public override void _GuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true } click) return;
            if (_view == null || _host == null) return;

            bool empty = _view.Kind == AugmentCellKind.Empty;
            switch (click.ButtonIndex)
            {
                case MouseButton.Left when empty:
                    _host.Pick(_view.SocketAddress);
                    break;
                case MouseButton.Right when !empty:
                    _host.Extract(_view.SocketAddress);
                    break;
                default:
                    return;
            }

            AcceptEvent();
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        /// <summary>What is in the slot, worded by the one place that words an augment — the same card
        /// the tray shows for the same copy and the picker showed before it was seated. An empty slot has
        /// no card: what could go in it is what a click asks, and the answer to that is a list.</summary>
        private IPopup? ShowTooltip()
        {
            if (_view == null || _windows == null || _view.Kind == AugmentCellKind.Empty) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            AugmentCard card = AugmentText.Card(_view);
            popup.Show(card.Name, card.TierLine, card.Details, card.RarityColor);
            return popup;
        }

        /// <summary>A filled cell borrows the augment's rarity for its frame; a remove-only one is
        /// dimmed on top of that, and an empty one falls back to the scene's neutral stylebox.</summary>
        private void ApplyStyle(AugmentCellView view, bool filled)
        {
            Modulate = view.Kind == AugmentCellKind.Held ? s_sealedTint : Colors.White;
            if (_emptyStyle == null) return;

            if (!filled || _emptyStyle is not StyleBoxFlat flat)
            {
                AddThemeStyleboxOverride("panel", _emptyStyle);
                return;
            }

            var rarity = Color.FromHtml(TextPalette.RarityColor(view.Rarity));
            var styled = (StyleBoxFlat)flat.Duplicate();
            styled.BorderColor = rarity;
            styled.BgColor = flat.BgColor.Lerp(rarity, 0.14f);
            AddThemeStyleboxOverride("panel", styled);
        }
    }
}
