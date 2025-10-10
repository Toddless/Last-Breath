namespace Crafting.Source.UIElements
{
    using Godot;

    public partial class ItemDetails : PanelContainer
    {
        private const string UID = "uid://bqx5ow411nolc";
        private Vector2 _baseSize;
        private Vector2 _currentMaxSize;
        [Export] private TextureRect? _itemIcon;
        [Export] private BoxContainer? _additionalStatsContainer, _baseStatsContainer, _itemSkillDescription;
        [Export] private Label? _itemName, _itemUpdateLevel;


        public override void _Ready()
        {
            CallDeferred(nameof(CalculateNewHorizonalSize));
            if (_itemName != null)
                _itemName.LabelSettings = new LabelSettings()
                {
                    FontSize = 22
                };
        }

        public void SetItemIcon(Texture2D icon)
        {
            if (_itemIcon != null) _itemIcon.Texture = icon;
        }

        public void SetItemBaseStats(SelectableItem item)
        {
            item.SetLabelSetting(UIResourcesProvider.Instance?.GetResource("BaseItemStatsSetting") as LabelSettings);
            _baseStatsContainer?.AddChild(item);
            var childSize = item.GetCombinedMinimumSize();
            _currentMaxSize = childSize > _currentMaxSize ? childSize : _currentMaxSize;
        }

        public void SetItemName(string itemName) => _itemName.Text = itemName;
        public void SetItemUpdateLevel(int level)
        {
            _itemUpdateLevel.Text = level > 0 ? $"+{level}" : string.Empty;
        }
        public void SetItemAdditionalStats(SelectableItem item)
        {
            item.SetLabelSetting(UIResourcesProvider.Instance?.GetResource("AdditionalStatsSettings") as LabelSettings);
            _additionalStatsContainer?.AddChild(item);
        }

        public void SetSkillDescription(SkillDescription skillDescription) => _itemSkillDescription?.AddChild(skillDescription);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void CalculateNewHorizonalSize()
        {
            var parent = _baseStatsContainer?.GetParent<Control>();
            if (parent != null)
            {
                var separation = parent.GetThemeConstant("separation");
                var width = (parent.Size.X - separation) / 2;
                if (_currentMaxSize.X > width)
                {
                    var newWidth = parent.Size.X + (_currentMaxSize.X - width);
                    parent.CustomMinimumSize = new Vector2(newWidth, parent.Size.Y);
                }
                CallDeferred(nameof(UpdateSize));
            }
        }
        private void UpdateSize() => this.CustomMinimumSize = Size;
    }
}
