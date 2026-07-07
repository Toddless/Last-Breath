namespace Core.Crafting
{
    using Results;
    using Items;

    public interface IItemAscender
    {
        bool CanAscend(IEquipItem item);
        AscensionResult TryAscendItem(IEquipItem item);
    }
}
