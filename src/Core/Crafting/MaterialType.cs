namespace Core.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public class MaterialType(List<IModifierDescriptor> modifiers, IMaterialCategory category) : IMaterial
    {
        public IMaterialCategory? MaterialCategory { get; } = category;

        /// <summary>Combined descriptors: the category's (shared across the category's materials — a category
        /// bonus is more likely the more of that category you use) plus the material's own.</summary>
        public IReadOnlyList<IModifierDescriptor> Modifiers
        {
            get
            {
                if (field != null) return field;

                var combined = new List<IModifierDescriptor>();
                if (MaterialCategory?.Modifiers is { } categoryModifiers)
                    combined.AddRange(categoryModifiers);
                combined.AddRange(modifiers);

                field = combined.AsReadOnly();
                return field;
            }
        }
    }
}
