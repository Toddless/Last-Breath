namespace Core.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public class MaterialCategory(List<IModifier> modifiers, string id) : IMaterialCategory
    {
        public string Id { get; set; } = id;
        public IReadOnlyList<IModifier> Modifiers { get; } = modifiers;
    }
}
