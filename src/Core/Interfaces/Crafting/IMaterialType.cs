namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface IMaterialType
    {
        string Id { get; }
        string DisplayName { get; }
        string[] Tags { get; }
        IReadOnlyList<IMaterialModifier>? Modifiers { get; }
        IMaterialCategory? MaterialCategory { get; }
    }
}
