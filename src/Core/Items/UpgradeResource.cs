namespace Core.Items
{
    using System;
    using System.Linq;
    using Crafting;
    using Enums;
    using Godot;

    public class UpgradeResource(
        string id,
        string[] tags,
        Rarity rarity,
        EquipmentCategory category,
        int maxStackSize)
        : IUpgradingResource, IItem
    {
        private UpgradeResource(UpgradeResource source) : this(source.Id, [.. source.Tags], source.Rarity, source.Category, source.MaxStackSize)
        {
            Icon = source.Icon;
        }

        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public Texture2D? Icon { get; set; }
        public Rarity Rarity { get; set; } = rarity;
        public EquipmentCategory Category { get; } = category;
        public int MaxStackSize { get; } = maxStackSize;
        public string DisplayName => TranslationServer.Translate(Id);
        public string Description => TranslationServer.Translate(Id + "_Description");

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new UpgradeResource(this);
    }
}
