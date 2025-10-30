namespace Crafting.TestResources
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class CraftingResource : Resource, ICraftingResource, IItem
    {
        [Export] private MaterialType? _material;
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int MaxStackSize { get; private set; }
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; set; }
        public float Quality { get; set; } = 1;
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);
        public IMaterialType? MaterialType => _material;
        public int Quantity { get; set; } = 1;
        public Rarity Rarity { get; set; } = Rarity.Rare;
        public string InstanceId { get; } = Guid.NewGuid().ToString();


        /// <summary>
        /// Default ctor to create resource within Editor
        /// </summary>
        public CraftingResource()
        {

        }

        /// <summary>
        /// Сtor to create resource within code
        /// </summary>
        /// <param name="id"></param>
        /// <param name="displayName"></param>
        /// <param name="maxStackSize"></param>
        /// <param name="tags"></param>
        /// <param name="icon"></param>
        /// <param name="materialType"></param>
        /// <param name="quality"></param>
        public CraftingResource(string id, int maxStackSize, string[] tags, Texture2D icon, MaterialType materialType, float quality)
        {
            Id = id;
            MaxStackSize = maxStackSize;
            Tags = tags;
            Icon = icon;
            _material = materialType;
            Quality = Mathf.Clamp(quality, 0, 1);
            InstanceId = Guid.NewGuid().ToString();
        }

        public T Copy<T>()
        {
            var duplicate = (ICraftingResource)DuplicateDeep();
            return (T)duplicate;
        }
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
