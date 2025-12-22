namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;

    public partial class CraftingResource : Resource, ICraftingResource, IItem
    {
        [Export] private MaterialType? _material;
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int MaxStackSize { get; private set; }
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        public IMaterial? Material => _material;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);


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
        /// <param name="maxStackSize"></param>
        /// <param name="tags"></param>
        /// <param name="icon"></param>
        /// <param name="material"></param>
        public CraftingResource(string id, int maxStackSize, string[] tags, Texture2D? icon, IMaterial material, Rarity rarity)
        {
            Id = id;
            MaxStackSize = maxStackSize;
            Tags = tags;
            Icon = icon;
            InstanceId = Guid.NewGuid().ToString();
            Rarity = rarity;
            _material = (MaterialType)material;
        }

        public T Copy<T>()
        {
            var duplicate = (ICraftingResource)DuplicateDeep(DeepDuplicateMode.All);
            return (T)duplicate;
        }
        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
