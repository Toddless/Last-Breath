namespace Core.Interfaces.CraftingResources
{
    using System.Collections.Generic;
    using Core.Enums;

    public interface IResourceMaterialType
    {
        ResourceCategory Category { get; }
        string MaterialName { get; }
        IReadOnlyList<IMaterialModifiers>? Modifiers { get; }
        float Quality { get; set; }
    }
}
