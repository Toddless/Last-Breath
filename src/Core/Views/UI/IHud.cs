namespace Core.Views.UI
{
    using Data;

    public interface IHud : IInitializable, IRequireServices
    {
        void Remove();
    }
}
