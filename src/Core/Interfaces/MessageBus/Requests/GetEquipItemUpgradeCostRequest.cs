namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;
    using Enums;
    using Crafting;

    public record GetEquipItemUpgradeCostRequest(string ItemInstanceId, ItemUpgradeMode Mode) : IRequest<IEnumerable<IResourceRequirement>>
    {
    }
}
