namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;

    public partial class UpgradeResource : Resource, IUpgradingResource, IItem
    {
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        [Export] public EquipmentCategory Category { get; private set; }
        [Export] public Texture2D? Icon { get; private set; }
        [Export] public int MaxStackSize { get; private set; } = 1;

        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public UpgradeResource()
        {

        }

        public UpgradeResource(
            string id,
            string[] tags,
            Rarity rarity,
            EquipmentCategory category,
            Texture2D? icon,
            int maxStackSize)
        {
            Id = id;
            Tags = tags;
            Rarity = rarity;
            Category = category;
            Icon = icon;
            MaxStackSize = maxStackSize;
        }

        public bool HasTag(string tag) => Tags.Contains(tag);
        public T Copy<T>()
        {
            var duplicate = (IUpgradingResource)DuplicateDeep(DeepDuplicateMode.All);
            return (T)duplicate;
        }
    }
}
