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
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string ResultItemId { get; protected set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; private set; }
        [Export] public Rarity Rarity { get; set; }
        [Export] public bool IsOpened { get; private set; } = true;
        [Export] public int MaxStackSize { get; private set; } = 1;
        public string DisplayName => Localizator.Localize(Id);
        public string Description => Localizator.LocalizeDescription(Id);
        public List<IResourceRequirement> MainResource { get; set; } = [];
        public string InstanceId { get; } = Guid.NewGuid().ToString();

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
            MainResource= requirements;
        }
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>()
        {
            var duplicate = (ICraftingRecipe)DuplicateDeep();
            duplicate.MainResource = MainResource;
            return (T)duplicate;
        }
    }
}
