namespace LastBreath.UI
{
    using Core.Items;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Hover tooltip of an item (bag slots, equipment rows): name colored by rarity, type line,
    /// then every line the item carries — implicits, modifiers, context lines and grants — through
    /// the same formatters the crafting window uses. Plain items show name and description only.
    /// </summary>
    [GlobalClass]
    public partial class ItemTooltipPopup : Control, IHoverTooltipPopup
    {
        private const string ScenePath = "res://UI/View/ItemTooltipPopup.tscn";

        [Export] private PanelContainer? _panel;
        [Export] private Label? _title;
        [Export] private Label? _subtitle;
        [Export] private VBoxContainer? _lines;

        public PopupLifetime Lifetime => PopupLifetime.WhileHovered;

        public OverlayRegion Region => OverlayRegion.Cursor;

        public bool IsPinned { get; private set; }

        public override void _Ready() => HoverTooltipMotion.Setup(this, _panel);

        public override void _Process(double delta)
        {
            if (!IsPinned) HoverTooltipMotion.Follow(this, _panel);
        }

        public override void _UnhandledKeyInput(InputEvent @event) => IsPinned = HoverTooltipMotion.TogglePin(@event, IsPinned);

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        public void ShowItem(IItem item)
        {
            if (_title != null)
            {
                _title.Text = item is IEquipItem { UpdateLevel: > 0 } upgraded
                    ? $"{item.DisplayName} +{upgraded.UpdateLevel}"
                    : item.DisplayName;
                _title.AddThemeColorOverride("font_color", Color.FromHtml(TextPalette.RarityColor(item.Rarity)));
            }

            if (_subtitle != null)
                _subtitle.Text = item is IEquipItem equip ? $"{item.Rarity} · {equip.EquipmentPiece}" : item.Rarity.ToString();

            if (_lines == null) return;
            if (item is IEquipItem equipItem) RenderEquipLines(equipItem);
            RenderDescription(item);
        }

        private void RenderEquipLines(IEquipItem item)
        {
            foreach (var implicitModifier in item.Implicits)
                AddLine(Localization.Format(implicitModifier), dim: true);
            foreach (var entry in item.ContextImplicits)
                AddLine(Localization.Format(entry), dim: true);

            bool hasImplicits = item.Implicits.Count > 0 || item.ContextImplicits.Count > 0;
            bool hasRolled = item.Modifiers.Count > 0 || item.ContextModifiers.Count > 0 || item.Grants.Count > 0;
            if (hasImplicits && hasRolled) _lines?.AddChild(new HSeparator());

            foreach (var modifier in item.Modifiers)
                AddLine(Localization.Format(modifier));
            foreach (var entry in item.ContextModifiers)
                AddLine(Localization.Format(entry));
            foreach (var grant in item.Grants)
                AddLine(Localization.Localize(grant.Id));
        }

        private void RenderDescription(IItem item)
        {
            if (string.IsNullOrEmpty(item.Description) || item.Description == $"{item.Id}_Description") return;
            _lines?.AddChild(new HSeparator());
            AddLine(item.Description, dim: true);
        }

        private void AddLine(string text, bool dim = false)
        {
            var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            if (dim) label.ThemeTypeVariation = "DimLabel";
            _lines?.AddChild(label);
        }
    }
}
