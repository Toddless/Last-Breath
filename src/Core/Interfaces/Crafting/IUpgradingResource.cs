namespace Core.Interfaces.Crafting
{
    using Enums;

    public interface IUpgradingResource : IResource
    {
        EquipmentCategory Category { get; }
    }
}
