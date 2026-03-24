namespace Crafting.TestResources
{
    using Godot;
    using Source.UIElements;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Services;
    using Source;

    public partial class EquipItemUi : Control, IInitializable, IEquipItemUi
    {
        private const string UID = "uid://b157jy1w03jwo";
        [Export] private Label? _itemName, _effectName, _rarity, _piece;
        [Export] private Array<Label> _itemBaseStats = [];
        [Export] private TextureRect? _itemIcon;
        [Export] private RichTextLabel? _itemDescription, _effectDescription;
        [Export] private ItemModifierList? _additionalStats;
        private IEquipItemUiConfiguration? _configuration;


        public override void _Ready()
        {
            // var provider = GameServiceProvider.Instance.GetService<IItemDataProvider>();
            // var messageBus = GameServiceProvider.Instance.GetService<IGameMessageBus>();
            // var mastery = new CraftingMastery(messageBus, new RandomNumberGenerator());
            // mastery.AddExperience(5000000);
            // var item = (IEquipItem)provider.CopyItem("Ring_Of_Fire_Demon");
            // SetConfiguration(new EquipItemCreationConfiguration(item, mastery));
        }

        public void SetConfiguration(IEquipItemUiConfiguration? configuration)
        {
            _configuration = configuration;
            _configuration?.Configure(this);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetItemName(string name) => _itemName?.Text = name;

        public void SetItemDescription(string description) => _itemDescription?.Text = description;

        public void SetItemRarity(string rarity) => _rarity?.Text = rarity;

        public void SetItemPiece(string piece) => _piece?.Text = piece;

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
    }
}
