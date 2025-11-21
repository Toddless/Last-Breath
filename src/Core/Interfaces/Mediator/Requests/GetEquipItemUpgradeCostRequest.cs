namespace Core.Interfaces.Mediator.Requests
{
    using Enums;
    using Crafting;
    using System.Collections.Generic;

    public record GetEquipItemUpgradeCostRequest(string ItemInstanceId, ItemUpgradeMode Mode) : IRequest<IEnumerable<IResourceRequirement>>
    {
    }
}
