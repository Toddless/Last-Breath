namespace LastBreath.Addons.Crafting
{
    using Godot;
    using System;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using LastBreath.Addons.Crafting.Resources.Materials;

    [Tool]
    [GlobalClass]
    public partial class CraftingResource : Resource, ICraftingResource, IItem
    {
        private string _id = string.Empty;
        [Export] private MaterialType? _materialType;
        [Export] public Core.Enums.Rarity Rarity { get; private set; } = Core.Enums.Rarity.Rare;
        /// <summary>
        /// Default value = 1;
        /// </summary>
        [Export] public int MaxStackSize { get; set; } = 9999;
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Texture2D? FullImage { get; set; }
        // TODO: I need to add basic tags for items created with code
        [Export] public string[] Tags { get; private set; } = [];

        [Export]
        public string Id
        {
            get => _id;
            set => _id = value;
        }
        public float Quality { get; set; } = 1;
        public IMaterialType? MaterialType => _materialType;

        public string DisplayName => GetLocalizedName();
        public string Description => GetLocalizedDescription();

        /// <summary>
        /// Default value = 1;
        /// </summary>
        public int Quantity { get; set; } = 1;

        public string InstanceId => throw new NotImplementedException();

        public List<string> GetItemStatsAsStrings() => [];
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public T Copy<T>(bool subresources = false)
        {
            var duplicate = (ICraftingResource)Duplicate(subresources);
            return (T)duplicate;
        }

        private string GetLocalizedName() => TranslationServer.Translate(Id);
        private string GetLocalizedDescription() => TranslationServer.Translate(Id + "_Description");
    }
}
