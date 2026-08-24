namespace Battle.Source.UIElements
{
    using Core.Inventory;
    using Core.Localization;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// One carried augment in the window's tray. A drag SOURCE and nothing else — the tray is a view of
    /// the bag, and everything that moves an augment goes through the socket gates.
    /// <para>
    /// The payload carries the copy's instance key alone. A bag slot accepts a drop carrying the item
    /// key and then resolves the drag's origin as another bag slot, which this is not — so putting that
    /// key in would let the bag take the tile and then report a missing source instead of moving
    /// anything. The instance key is the same one a bag slot puts in, which is why dragging straight
    /// out of an open inventory window into a socket works without a line of code on that side.
    /// </para>
    /// </summary>
    [GlobalClass]
    public partial class AugmentTrayTile : PanelContainer
    {
        /// <summary>Filled in when the scene is built.</summary>
        private const string UID = "uid://dp4ta9cn1mh8k";

        private const float DragPreviewSize = 60f;

        [Export] private TextureRect? _icon;
        [Export] private Label? _tier;

        private AugmentTrayTileView? _view;
        private IUiElementsManager? _windows;
        private StyleBox? _emptyStyle;

        public override void _Ready()
        {
            _emptyStyle = GetThemeStylebox("panel");
            HoverTooltip.Attach(this, ShowTooltip);
        }

        public void SetView(AugmentTrayTileView view, IUiElementsManager? windows)
        {
            _view = view;
            _windows = windows;
            _icon?.Texture = view.Icon;
            _tier?.Text = view.Tier.ToString();
            ApplyStyle(view);
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (_view == null) return new Variant();

            var preview = new TextureRect
            {
                Texture = _view.Icon,
                MouseFilter = MouseFilterEnum.Ignore,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = new Vector2(DragPreviewSize, DragPreviewSize),
            };
            SetDragPreview(preview);
            return new Godot.Collections.Dictionary { [DragPayload.Instance] = _view.InstanceId };
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private IPopup? ShowTooltip()
        {
            if (_view == null || _windows == null) return null;
            if (_windows.ShowPopup(typeof(TextTooltipPopup)) is not TextTooltipPopup popup) return null;

            AugmentCard card = AugmentText.Card(_view);
            popup.Show(card.Name, card.TierLine, card.Details, card.RarityColor);
            return popup;
        }

        private void ApplyStyle(AugmentTrayTileView view)
        {
            if (_emptyStyle is not StyleBoxFlat flat) return;

            var rarity = Color.FromHtml(TextPalette.RarityColor(view.Rarity));
            var styled = (StyleBoxFlat)flat.Duplicate();
            styled.BorderColor = rarity;
            styled.BgColor = flat.BgColor.Lerp(rarity, 0.14f);
            AddThemeStyleboxOverride("panel", styled);
        }
    }
}
