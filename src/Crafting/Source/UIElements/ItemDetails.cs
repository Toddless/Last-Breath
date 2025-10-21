namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Crafting.Source.UIElements.Styles;

    public partial class ItemDetails : PanelContainer, IInitializable, IClosable, IRequireServices, IRequireReposition
    {
        private const string UID = "uid://bqx5ow411nolc";
        private Vector2 _baseSize;
        private Vector2 _currentMaxSize;
        [Export] private TextureRect? _itemIcon;
        [Export] private BoxContainer? _additionalStatsContainer, _baseStatsContainer, _itemSkillDescription;
        [Export] private Label? _itemName, _itemUpdateLevel;
        private UIResourcesProvider? _uiResourcesProvider;

        public event Action? Close;
        public event Action<Control>? Reposition;

        public override void _Ready()
        {
            CallDeferred(nameof(CalculateNewHorizonalSize));
        }

        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _uiResourcesProvider = provider.GetService<UIResourcesProvider>();
        }

        public void SetItemName(string itemName) => _itemName.Text = itemName;

        public void SetItemIcon(Texture2D icon)
        {
            if (_itemIcon != null) _itemIcon.Texture = icon;
        }
        public void SetItemUpdateLevel(int level)
        {
            _itemUpdateLevel.Text = level > 0 ? $"+{level}" : string.Empty;
        }

        public void SetItemBaseStats(InteractiveLabel item)
        {
            item.SetLabelSetting(_uiResourcesProvider?.GetResource("BaseItemStatsSetting") as LabelSettings);
            _baseStatsContainer?.AddChild(item);
            var childSize = item.GetCombinedMinimumSize();
            _currentMaxSize = childSize > _currentMaxSize ? childSize : _currentMaxSize;
        }

        public void SetItemAdditionalStats(InteractiveLabel item)
        {
            item.SetLabelSetting(_uiResourcesProvider?.GetResource("AdditionalStatsSettings") as LabelSettings);
            _additionalStatsContainer?.AddChild(item);
        }

        public void SetSkillDescription(SkillDescription skillDescription) => _itemSkillDescription?.AddChild(skillDescription);

        public void Clear()
        {
            FreeChildren(_baseStatsContainer?.GetChildren() ?? []);
            FreeChildren(_additionalStatsContainer?.GetChildren() ?? []);
            FreeChildren(_itemSkillDescription?.GetChildren() ?? []);
        }

        public override void _ExitTree() => Close?.Invoke();

        private void FreeChildren(Array<Node> children)
        {
            foreach (var child in children)
                child.QueueFree();
        }
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

        private void UpdateSize()
        {
            CustomMinimumSize = Size;
            Reposition?.Invoke(this);
        }
    }
}
