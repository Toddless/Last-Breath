namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Godot.Collections;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    [Tool]
    [GlobalClass]
    public partial class CraftingRecipe : Resource, ICraftingRecipe
    {
        [Export] private Array<ResourceRequirement> _mainResources = [];
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string ResultItemId { get; protected set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        [Export] public Texture2D? Icon { get; private set; }
        [Export] public Texture2D? FullImage { get; private set; }
        [Export] public Rarity Rarity { get; private set; }
        [Export] public bool IsOpened { get; private set; } = true;
        public string DisplayName => Lokalizator.Lokalize(Id);
        public string Description => Lokalizator.LokalizeDescription(Id);
        public List<IResourceRequirement> MainResource => [.. _mainResources];
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public int MaxStackSize => 1;

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public T Copy<T>()
        {
            var duplicate = (ICraftingRecipe)DuplicateDeep();
            return (T)duplicate;
        }
    }
}
