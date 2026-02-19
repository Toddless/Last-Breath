namespace Crafting.Source
{
    using Godot;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Modifiers;

    public partial class MaterialType : Resource, IMaterial
    {
        private IReadOnlyList<IModifier>? _cached;
        [Export] private MaterialCategory? _category;
        [Export] private MaterialModifier[] _modifiers = [];

        public IMaterialCategory? MaterialCategory => _category;

        /// <summary>
        /// Combined modifiers
        /// </summary>
        public IReadOnlyList<IModifier> Modifiers
        {
            get
            {
                if (_cached != null) return _cached;
                var list = new List<IModifier>();

                if (MaterialCategory?.Modifiers is IReadOnlyList<IModifier> mods)
                    list.AddRange(mods);

                if (_modifiers.Length > 0)
                {
                    foreach (var item in _modifiers)
                    {
                        if (item != null)
                            list.Add(item);
                    }
                }
                _cached = list.AsReadOnly();

                return _cached;
            }
        }


        // we need to create a default, parameterless constructor in order to create a resource from within the Godot Editor.
        public MaterialType()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="category"></param>
        public MaterialType(List<IModifier> modifiers, IMaterialCategory category)
        {
            _modifiers = [.. modifiers.Cast<MaterialModifier>()];
            _category = (MaterialCategory)category;
        }
    }
}
