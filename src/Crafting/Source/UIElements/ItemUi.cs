namespace Crafting.Source.UIElements
{
    using System.Collections.Generic;
    using Core.Views.UI;
    using Godot;
    using Godot.Collections;

    public partial class ItemUi : Control, IInitializable, IItemUi
    {
        private const string UID = "uid://b157jy1w03jwo";
        [Export] private Label? _itemName, _effectName, _rarity, _piece, _itemUpgradeLevel;
        [Export] private Array<Label> _itemBaseStats = [];
        [Export] private TextureRect? _itemIcon;
        [Export] private RichTextLabel? _itemDescription, _effectDescription;
        [Export] private ItemModifierList? _additionalStats;
        private IItemUiConfiguration? _configuration;

        [Signal]
        public delegate void ModifierSelectedEventHandler(int identifier, ItemModifierList source);

        public override void _Ready()
        {
            if (_additionalStats != null)
                _additionalStats.ItemSelected += OnModifierItemSelected;
        }

        public void SetModifiersSelectable(bool selectable) => _additionalStats?.SetItemsSelectable(selectable);

        public void SetConfiguration(IItemUiConfiguration? configuration)
        {
            _configuration = configuration;
            _configuration?.Configure(this);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetItemName(string name) => _itemName?.Text = name;

        public void SetItemDescription(string description) => _itemDescription?.Text = description;

        public void SetItemRarity(string rarity) => _rarity?.Text = rarity;

        public void SetItemPiece(string piece) => _piece?.Text = piece;

        public void SetItemUpgradeLevel(string level) => _itemUpgradeLevel?.Text = level;

        public void SetItemBaseStats(List<string> baseStats)
        {
            for (int i = 0; i < baseStats.Count; i++)
            {
                _itemBaseStats[i].Text = baseStats[i];
            }
        }

        public void SetItemIcon(Texture2D? icon) => _itemIcon?.Texture = icon;

        public void SetItemEffectName(string name) => _effectName?.Text = name;

        public void SetItemEffectDescription(string description) => _effectDescription?.Text = description;

        public void SetItemAdditionalStats(List<(string ModifierText, int Identifier)> additionalStats) => _additionalStats?.AddModifiersToList(additionalStats);

        private void OnModifierItemSelected(int identifier, ItemModifierList source) =>
            EmitSignal(global::Crafting.Source.UIElements.ItemUi.SignalName.ModifierSelected, identifier, source);
    }
}
