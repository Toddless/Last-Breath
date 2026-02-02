namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Modifiers;

    public interface IMaterialCategory
    {
        string Id { get; set; }
        IReadOnlyList<IModifier> Modifiers { get; }
    }
}
