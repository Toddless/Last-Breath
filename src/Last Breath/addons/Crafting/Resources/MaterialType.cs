namespace LastBreath.Addons.Crafting
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;
    using Godot.Collections;

    [Tool]
    [GlobalClass]
    public partial class MaterialType : Resource, IMaterialType
    {
        private IReadOnlyList<IMaterialModifier>? _cached;
        [Export] private Array<MaterialModifier> _modifiers = [];

        [Export] public string Id { get; set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];

        /// <summary>
        /// Category define base modifiers pool
        /// </summary>
        [Export] public MaterialCategory Category { get; private set; }

        public string DisplayName => GetLocalizedMaterialName();

        /// <summary>
        /// Additional modifiers
        /// </summary>
        public IReadOnlyList<IMaterialModifier>? Modifiers => _cached ??= _modifiers?.Cast<IMaterialModifier>().ToList();

        private string GetLocalizedMaterialName() => TranslationServer.Translate(Id);
    }
}
