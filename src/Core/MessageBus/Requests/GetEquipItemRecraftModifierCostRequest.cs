namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Interfaces;

    public record GetEquipItemRecraftModifierCostRequest(string ItemInstanceId) : IRequest<IEnumerable<IRequirement>>
    {
    }
}
