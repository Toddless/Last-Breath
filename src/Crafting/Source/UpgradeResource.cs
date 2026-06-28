namespace Crafting.Source
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Godot;
    using Utilities;

    public partial class UpgradeResource : Resource, IUpgradingResource, IItem
    {
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        [Export] public EquipmentCategory Category { get; private set; }
        [Export] public Texture2D? Icon { get; private set; }
        [Export] public int MaxStackSize { get; private set; } = 1;

        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public UpgradeResource()
        {

        }

        public UpgradeResource(
            string id,
            string[] tags,
            Rarity rarity,
            EquipmentCategory category,
            int maxStackSize)
        {
            Id = id;
            Tags = tags;
            Rarity = rarity;
            Category = category;
            MaxStackSize = maxStackSize;
        }
        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag);
        public T Copy<T>()
        {
            var duplicate = (IUpgradingResource)DuplicateDeep(DeepDuplicateMode.All);
            return (T)duplicate;
        }
    }
}
