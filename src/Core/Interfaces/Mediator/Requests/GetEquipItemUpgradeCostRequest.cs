namespace Core.Interfaces.Mediator.Requests
{
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public record GetEquipItemUpgradeCostRequest(string ItemInstanceId, ItemUpgradeMode Mode) : IRequest<IEnumerable<IResourceRequirement>>
    {
    }
}
