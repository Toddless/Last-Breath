namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Core.Enums;

    public interface IResourceMaterialType
    {
        ResourceCategory Category { get; }
        IReadOnlyList<IMaterialModifiers>? Modifiers { get; }
        string MaterialName { get; }
        float Quality { get; set; }
    }
}
