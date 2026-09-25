namespace Core.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public interface IMaterial
    {
        IReadOnlyList<IModifierDescriptor> Modifiers { get; }
        IMaterialCategory? MaterialCategory { get; }
    }
}
