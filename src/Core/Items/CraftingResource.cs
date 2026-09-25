namespace Core.Items
{
    using System;
    using System.Linq;
    using Constants;
    using Crafting;
    using Enums;
    using Godot;

    public class CraftingResource : ICraftingResource, IItem
    {
        public CraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity)
        {
            Id = id;
            MaxStackSize = maxStackSize;
            Tags = tags;
            Material = material;
            Rarity = rarity;
        }

        // Materials are immutable template data: copies share the material reference.
        private CraftingResource(CraftingResource source)
        {
            Id = source.Id;
            MaxStackSize = source.MaxStackSize;
            Tags = [.. source.Tags];
            Material = source.Material;
            Rarity = source.Rarity;
            BasePrice = source.BasePrice;
            // Icon is intentionally NOT copied: it lazy-loads from AssetPaths.ResourceIcon(Id), so the copy
            // resolves its own texture on first access. Reading source.Icon here would force a ResourceLoader
            // call on every copy — and hard-crash hosts without the Godot runtime (tests, loot simulation).
        }

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        /// <summary>Authored base gold price from the data ("basePrice"); set by the parser.</summary>
        public int BasePrice { get; set; }

        public int MaxStackSize { get; }
        public string[] Tags { get; }
        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>(AssetPaths.ResourceIcon(Id));
                return field;
            }
        }
        public Rarity Rarity { get; set; }
        public IMaterial? Material { get; }
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new CraftingResource(this);
    }
}
