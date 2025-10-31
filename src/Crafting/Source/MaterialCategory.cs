namespace Crafting.Source
{
    using Godot;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public partial class MaterialCategory : Resource, IMaterialCategory
    {
        public string Id { get; set; } = string.Empty;
        public IReadOnlyList<IMaterialModifier>? Modifiers { get; private set; }

        // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
        public MaterialCategory()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="id"></param>
        public MaterialCategory(List<IMaterialModifier> modifiers, string id)
        {
            Modifiers = modifiers;
            Id = id;
        }
    }
}
