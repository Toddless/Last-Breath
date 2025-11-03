namespace Core.Interfaces.Mediator.Requests
{
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public record GetEquipItemRecraftModifierCostRequest(string ItemInstanceId) : IRequest<IEnumerable<IResourceRequirement>>
    {
    }
}
