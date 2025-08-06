namespace Crafting.Source.CraftingResources
{
    using Godot;
    using System.Collections.Generic;
    using Crafting.Source.Localization;
    using Core.Interfaces.Items;
    using Core.Interfaces.CraftingResources;

    [GlobalClass]
    public partial class CraftingResource : Resource, ICraftingResource, IItem
    {
        private string? _id;
        [Export] private LocalizedString? _name;
        [Export] private LocalizedString? _description;
        [Export] private ResourceMaterialType? _materialType;
        [Export] public Core.Enums.Rarity Rarity { get; private set; } = Core.Enums.Rarity.Rare;
        public IResourceMaterialType? MaterialType => _materialType;

        /// <summary>
        /// Default value = 1;
        /// </summary>
        [Export] public int MaxStackSize { get; set; } = 1;

        public Texture2D? Icon { get; set; }
        public Texture2D? FullImage { get; set; }

        public string Name => _name?.Text ?? string.Empty;
        public string Description => _description?.Text ?? string.Empty;
        public string Id => _id ?? SetId();

        /// <summary>
        /// Default value = 1;
        /// </summary>
        public int Quantity { get; set; } = 1;

        public List<string> GetItemStatsAsStrings() => [];
        public IItem Copy(bool subresources = false) => Clone(subresources);
        ICraftingResource ICraftingResource.Copy(bool subresources) => Clone(subresources);

        private string SetId() => $"{_name?.Key}";

        private CraftingResource Clone(bool subresources)
        => (CraftingResource)Duplicate(subresources);
    }
}
