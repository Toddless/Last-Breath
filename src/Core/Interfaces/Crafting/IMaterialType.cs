namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface IMaterialType : IIdentifiable
    {
        IReadOnlyList<IMaterialModifier>? Modifiers { get; }
        IMaterialCategory? MaterialCategory { get; }
    }
}
