namespace LastBreath.addons.Crafting.Resources.Materials
{
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Godot;
    using Godot.Collections;
    using LastBreath.Addons.Crafting.Resources.Materials;

    [Tool]
    [GlobalClass]
    public partial class MaterialCategory : Resource, IMaterialCategory
    {
        [Export] private Array<MaterialModifier> _modifiers = [];

        [Export] public string Id { get; set; } = string.Empty;

        public IReadOnlyList<IMaterialModifier>? Modifiers => _modifiers;

        // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
        public MaterialCategory()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="id"></param>
        public MaterialCategory(Array<MaterialModifier> modifiers, string id)
        {
            _modifiers = modifiers;
            Id = id;
        }
    }
}
