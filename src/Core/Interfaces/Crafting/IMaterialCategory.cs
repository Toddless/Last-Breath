namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;

    public interface IMaterialCategory
    {
        string Id { get; set; }
        IReadOnlyList<IMaterialModifier>? Modifiers { get; }
    }
}
