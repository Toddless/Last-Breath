namespace Core.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public interface IMaterialCategory
    {
        string Id { get; set; }
        IReadOnlyList<IModifierDescriptor> Modifiers { get; }
    }
}
