namespace Core.Interfaces.Inventory
{
    using Core.Interfaces.Items;

    public interface IBaseSlot<T> where T : class, IItem
    {
        T? CurrentItem { get; }
    }
}
