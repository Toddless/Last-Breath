namespace Core.Interfaces.UI
{
    using Data;

    public interface IWindow : IInitializable, IRequireServices
    {
        bool IsAlreadyVisible { get; }

        void Close();
    }
}
