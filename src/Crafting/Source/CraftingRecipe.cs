namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public partial class CraftingRecipe : Resource, ICraftingRecipe, IItem
    {
        public string Id { get; private set; } = string.Empty;
        public string ResultItemId { get; protected set; } = string.Empty;
        public string[] Tags { get; private set; } = [];
        public Texture2D? Icon { get; private set; }
        public Rarity Rarity { get; set; }
        public bool IsOpened { get; private set; } = true;
        public string DisplayName => Localizator.Localize(Id);
        public string Description => Localizator.LocalizeDescription(Id);
        public List<IResourceRequirement> MainResource { get; private set; } = [];
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public int MaxStackSize => 1;

        public CraftingRecipe()
        {

        }

        public CraftingRecipe(string id, string resultItemId, string[] tags, Texture2D? icon, Rarity rarity, List<IResourceRequirement> requirements, bool isOpened = false)
        {
            Id = id;
            ResultItemId = resultItemId;
            Tags = tags;
            Icon = icon;
            Rarity = rarity;
            IsOpened = isOpened;
            MainResource = requirements;
        }
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>()
        {
            var duplicate = (ICraftingRecipe)DuplicateDeep();
            return (T)duplicate;
        }
    }
}
