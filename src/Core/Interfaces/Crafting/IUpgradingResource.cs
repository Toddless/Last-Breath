namespace Core.Interfaces.Crafting
{
    using Core.Enums;

    public interface IUpgradingResource : IResource
    {
        EquipmentCategory Category { get; }
    }
}
