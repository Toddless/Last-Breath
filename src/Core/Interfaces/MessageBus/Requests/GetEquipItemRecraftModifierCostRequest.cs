namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;
    using Crafting;

    public record GetEquipItemRecraftModifierCostRequest(string ItemInstanceId) : IRequest<IEnumerable<IResourceRequirement>>
    {
    }
}
