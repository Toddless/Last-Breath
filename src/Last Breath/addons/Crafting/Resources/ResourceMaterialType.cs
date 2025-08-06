namespace LastBreath.Addons.Crafting
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;
    using Godot.Collections;
    using LastBreath.Localization;

    [GlobalClass]
    public partial class ResourceMaterialType : Resource, IResourceMaterialType
    {
        [Export] private Array<MaterialModifiers>? _modifiers;
        [Export] private LocalizedString? _materialName;
        public string MaterialName => _materialName?.Text ?? string.Empty;
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
