namespace Core.Views.UI
{
    using Data;

    public interface IWindow : IInitializable, IRequireServices
    {
        bool IsAlreadyVisible { get; }

        void Close();
    }
}
