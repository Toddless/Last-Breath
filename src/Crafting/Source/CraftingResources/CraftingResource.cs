namespace Crafting.Source.CraftingResources
{
    using Godot;
    using Core.Interfaces;
    using System.Collections.Generic;
    using Crafting.Source.Localization;

    [GlobalClass]
    public partial class CraftingResource : Resource, ICraftingResource, IItem
    {
        private string? _id;
        [Export] private LocalizedString? _name;
        [Export] private LocalizedString? _description;
        [Export] private Texture2D? _icon;
        [Export] private Texture2D? _fullImage;
        [Export] public ResourceType Type { get; private set; }
        [Export] public Core.Enums.Rarity Rarity { get; private set; } = Core.Enums.Rarity.Rare;
        public Texture2D? Icon => _icon;
        public Texture2D? FullImage => _fullImage;
        /// <summary>
        /// Default value = 1;
        /// </summary>
        [Export] public int MaxStackSize { get; set; } = 1;

        public string Name => _name?.Text ?? string.Empty;
        public string Description => _description?.Text ?? string.Empty;
        public string Id => _id ?? SetId();
        public ResourceQuality Quality { get; set; }
        /// <summary>
        /// Default value = 1;
        /// </summary>
        public int Quantity { get; set; } = 1;

        public List<string> GetItemStatsAsStrings() => [];
        public IItem Copy(bool subresources = false) => Clone(subresources);
        ICraftingResource ICraftingResource.Copy(bool subresources) => Clone(subresources);

        private string SetId() => $"{_name?.Key}_{Quality}";

        private CraftingResource Clone(bool subresources)
        => (CraftingResource)Duplicate(subresources);
    }
}
