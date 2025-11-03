namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface IMaterial
    {
        IReadOnlyList<IMaterialModifier>? Modifiers { get; }
        IMaterialCategory? MaterialCategory { get; }
    }
}
