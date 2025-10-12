namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;

    public record CreateEquipItemRequest(string EquipItemId, IEnumerable<IMaterialModifier> ResourceModifiers, Dictionary<string, int> UsedResources) : IRequest{}
}
