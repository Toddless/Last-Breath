namespace Core.Items
{
    using System;
    using System.Linq;
    using Constants;
    using Crafting;
    using Enums;
    using Godot;

    public class UpgradeResource(
        string id,
        string[] tags,
        Rarity rarity,
        EquipmentCategory? category,
        int maxStackSize)
        : IUpgradingResource, IItem
    {
        // Icon is intentionally NOT copied: it lazy-loads from AssetPaths.ResourceIcon(Id), so the copy
        // resolves its own texture on first access. Reading source.Icon here would force a ResourceLoader
        // call on every copy — and hard-crash hosts without the Godot runtime (tests, loot simulation).
        private UpgradeResource(UpgradeResource source) : this(source.Id, [.. source.Tags], source.Rarity, source.Category, source.MaxStackSize)
        {
        }

        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>(AssetPaths.ResourceIcon(Id));
                return field;
            }
        }
        public Rarity Rarity { get; set; } = rarity;
        public EquipmentCategory? Category { get; } = category;
        public int MaxStackSize { get; } = maxStackSize;
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new UpgradeResource(this);
    }
}
