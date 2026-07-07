namespace Core.Crafting
{
    using Enums;

    public interface IUpgradingResource : IResource
    {
        EquipmentCategory Category { get; }
    }
}
