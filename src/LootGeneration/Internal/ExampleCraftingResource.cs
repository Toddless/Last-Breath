namespace LootGeneration.Internal
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Godot;
    using Utilities;

    internal partial class ExampleCraftingResource  : Resource, ICraftingResource, IItem
    {
        [Export] private ExampleMaterialType? _material;
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int MaxStackSize { get; private set; }
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        public IMaterial? Material => _material;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);


        /// <summary>
        /// Default ctor to create resource within Editor
        /// </summary>
        public ExampleCraftingResource()
        {
        }

        /// <summary>
        /// Сtor to create resource within code
        /// </summary>
        /// <param name="id"></param>
        /// <param name="maxStackSize"></param>
        /// <param name="tags"></param>
        /// <param name="material"></param>
        /// <param name="rarity"></param>
        public ExampleCraftingResource(
            string id,
            int maxStackSize,
            string[] tags,
            IMaterial material,
            Rarity rarity)
        {
            Id = id;
            MaxStackSize = maxStackSize;
            Tags = tags;
            InstanceId = Guid.NewGuid().ToString();
            Rarity = rarity;
            _material = (ExampleMaterialType)material;
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
