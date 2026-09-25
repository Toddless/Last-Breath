namespace Core.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public class MaterialCategory(List<IModifierDescriptor> modifiers, string id) : IMaterialCategory
    {
        public string Id { get; set; } = id;
        public IReadOnlyList<IModifierDescriptor> Modifiers { get; } = modifiers;
    }
}
