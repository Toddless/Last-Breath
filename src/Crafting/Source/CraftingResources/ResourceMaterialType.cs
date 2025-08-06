namespace Crafting.Source.CraftingResources
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.CraftingResources;
    using Godot;
    using Godot.Collections;

    [GlobalClass]
    public partial class ResourceMaterialType : Resource, IResourceMaterialType
    {
        [Export] private Array<MaterialModifiers>? _modifiers;
        [Export] public string MaterialName { get; private set; } = string.Empty;
        /// <summary>
        /// Category define base modifiers pool
        /// </summary>
        [Export] public ResourceCategory Category { get; private set; }
        /// <summary>
        /// Additional modifiers
        /// </summary>
        public IReadOnlyList<IMaterialModifiers>? Modifiers => _modifiers?.Cast<IMaterialModifiers>().ToList();
        public float Quality { get; set; }
    }
}
