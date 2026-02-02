namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public interface IMaterial
    {
        IReadOnlyList<IModifier> Modifiers { get; }
        IMaterialCategory? MaterialCategory { get; }
    }
}
