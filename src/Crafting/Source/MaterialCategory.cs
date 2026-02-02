namespace Crafting.Source
{
    using Godot;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Modifiers;

    public partial class MaterialCategory : Resource, IMaterialCategory
    {
        [Export] private MaterialModifier[] _modifiers = [];
        [Export] public string Id { get; set; } = string.Empty;
        public IReadOnlyList<IModifier> Modifiers => _modifiers;

        // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
        public MaterialCategory()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="id"></param>
        public MaterialCategory(List<IModifier> modifiers, string id)
        {
            _modifiers = [.. modifiers.Cast<MaterialModifier>()];
            Id = id;
        }
    }
}
