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
    public partial class ResourceMaterialType : Resource, IResourceMaterialType
    {
        private string _id = string.Empty;
        private IReadOnlyList<IMaterialModifiers>? _cached;
        [Export] private Array<MaterialModifiers>? _modifiers;

        [Export]
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string MaterialName => GetLocalizedMaterialName();

        /// <summary>
        /// Category define base modifiers pool
        /// </summary>
        [Export] public ResourceCategory Category { get; private set; }

        /// <summary>
        /// Additional modifiers
        /// </summary>
        public IReadOnlyList<IMaterialModifiers>? Modifiers => _cached ??= _modifiers?.Cast<IMaterialModifiers>().ToList();
        public float Quality { get; set; }

        private string GetLocalizedMaterialName() => TranslationServer.Translate(Id);
    }
}
