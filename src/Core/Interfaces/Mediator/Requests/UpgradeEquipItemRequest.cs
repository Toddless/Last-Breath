namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;
    using Results;

    public record UpgradeEquipItemRequest(string InstanceId, Dictionary<string, int> Resources) : IRequest<ItemUpgradeResult>
    {
    }
}
