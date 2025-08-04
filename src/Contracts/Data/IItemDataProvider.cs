namespace Contracts.Data
{
    using Contracts.Interfaces;

    public interface IItemDataProvider<T>
        where T : class
    {
        public void LoadData();
        T GetItemData(IEquipItem item);
    }
}
