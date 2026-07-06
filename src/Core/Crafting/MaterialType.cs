namespace Core.Crafting
{
    using System.Collections.Generic;
    using Interfaces.Crafting;
    using Modifiers;

    public class MaterialType(List<IModifier> modifiers, IMaterialCategory category) : IMaterial
    {
        private IReadOnlyList<IModifier>? _cached;

        public IMaterialCategory? MaterialCategory { get; } = category;

        /// <summary>Combined modifiers: category modifiers plus the material's own.</summary>
        public IReadOnlyList<IModifier> Modifiers
        {
            get
            {
                if (_cached != null) return _cached;

                var combined = new List<IModifier>();
                if (MaterialCategory?.Modifiers is { } categoryModifiers)
                    combined.AddRange(categoryModifiers);
                combined.AddRange(modifiers);

                _cached = combined.AsReadOnly();
                return _cached;
            }
        }
    }
}
