namespace Core.Data
{
    using Core.Interfaces;

    public interface IItemDataProvider<T>
        where T : class
    {
        public void LoadData();
        T GetItemData(IEquipItem item);
    }
}
