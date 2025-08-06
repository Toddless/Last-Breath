namespace Core.Interfaces.Data
{
    public interface IItemDataProvider<T, U>
        where T : class
        where U : class
    {
        public void LoadData();
        T GetItemData(U item);
    }
}
