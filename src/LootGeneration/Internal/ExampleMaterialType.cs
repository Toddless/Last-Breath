namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using Core.Modifiers;
    using Godot;

    internal partial class ExampleMaterialType: Resource, IMaterial
    {
        private IReadOnlyList<IModifier>? _cached;
        [Export] private ExampleMaterialCategory? _category;
        [Export] private ExampleMaterialModifier[] _modifiers = [];

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
        public ExampleMaterialType()
        {

        }
        /// <summary>
        /// Constructor to create a resource within code
        /// </summary>
        /// <param name="modifiers"></param>
        /// <param name="category"></param>
        public ExampleMaterialType(List<IModifier> modifiers, IMaterialCategory category)
        {
            _modifiers = [.. modifiers.Cast<ExampleMaterialModifier>()];
            _category = (ExampleMaterialCategory)category;
        }
    }
}
