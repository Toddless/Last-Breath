namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crafting;
    using Enums;
    using Godot;
    using Interfaces;

    public class CraftingRecipe : ICraftingRecipe, IItem
    {
        public CraftingRecipe(string id, string resultItemId, string[] tags, Rarity rarity, List<IRequirement> requirements, ItemType itemType,
            string[] optionalResourceCategories, bool isOpened = false)
        {
            Id = id;
            ResultItemId = resultItemId;
            Tags = tags;
            Rarity = rarity;
            Requirements = requirements;
            ItemType = itemType;
            OptionalResourceCategories = optionalResourceCategories;
            IsOpened = isOpened;
        }

        private CraftingRecipe(CraftingRecipe source)
        {
            Id = source.Id;
            ResultItemId = source.ResultItemId;
            Tags = [.. source.Tags];
            Rarity = source.Rarity;
            Requirements = source.Requirements;
            ItemType = source.ItemType;
            OptionalResourceCategories = [.. source.OptionalResourceCategories];
            IsOpened = source.IsOpened;
            Icon = source.Icon;
        }

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string ResultItemId { get; }
        public string[] Tags { get; }
        public Texture2D? Icon { get; set; }
        public Rarity Rarity { get; set; }
        public bool IsOpened { get; }
        public int MaxStackSize => 1;
        public ItemType ItemType { get; }
        public string[] OptionalResourceCategories { get; }
        public List<IRequirement> Requirements { get; set; }
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new CraftingRecipe(this);
    }
}
