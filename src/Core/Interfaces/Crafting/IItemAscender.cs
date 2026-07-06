namespace Core.Interfaces.Crafting
{
    using Items;
    using Results;

    public interface IItemAscender
    {
        bool CanAscend(IEquipItem item);
        AscensionResult TryAscendItem(IEquipItem item);
    }
}
