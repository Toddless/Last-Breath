namespace Core.Interfaces.UI
{
    using Data;

    public interface IHud : IInitializable, IRequireServices
    {
        void Remove();
    }
}
