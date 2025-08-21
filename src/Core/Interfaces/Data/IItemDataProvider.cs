namespace Core.Interfaces.Data
{
    public interface IItemDataProvider<T>
        where T : class
    {
        public void LoadData();
        T GetItemData(string id);
    }
}
