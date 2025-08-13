namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Core.Enums;

    public interface IMaterialType
    {
        string Id { get; }
        string DisplayName { get; }
        string[] Tags { get; }
        MaterialCategory Category { get; }
        IReadOnlyList<IMaterialModifier>? Modifiers { get; }
    }
}
