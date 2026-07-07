namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Interfaces;

    public record GetEquipItemUpgradeCostRequest(string ItemInstanceId) : IRequest<IEnumerable<IRequirement>>
    {
    }
}
