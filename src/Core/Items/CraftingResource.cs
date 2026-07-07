namespace Core.Items
{
    using System;
    using System.Linq;
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
            Icon = source.Icon;
        }

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public int MaxStackSize { get; }
        public string[] Tags { get; }
        public Texture2D? Icon { get; set; }
        public Rarity Rarity { get; set; }
        public IMaterial? Material { get; }
        public string DisplayName => TranslationServer.Translate(Id);
        public string Description => TranslationServer.Translate(Id + "_Description");

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new CraftingResource(this);
    }
}
