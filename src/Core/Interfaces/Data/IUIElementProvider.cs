namespace Core.Interfaces.Data
{
    using Godot;
    using Core.Interfaces.UI;

    public interface IUIElementProvider
    {
        void ClearSource(Control source);
        T Create<T>() where T : Control, IInitializable;
        T CreateAndShowOnUI<T>() where T : Control, IInitializable;
        T CreateClosableForSource<T>(Control source) where T : Control, IInitializable, IClosable, IRequireServices;
        T CreateRequireServices<T>() where T : Control, IInitializable, IRequireServices;
        T CreateRequireServicesClosable<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        T CreateSingleClosable<T>() where T : Control, IInitializable, IClosable;
        void Subscribe(CanvasLayer layer);
        void Unload<T>() where T : Control;
    }
}
