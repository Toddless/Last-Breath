namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;

    public record GetEquipItemUpgradeCostRequest(string ItemInstanceId) : IRequest<IEnumerable<IRequirement>>
    {
    }
}
