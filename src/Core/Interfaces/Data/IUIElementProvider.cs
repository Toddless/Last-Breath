namespace Core.Interfaces.Data
{
    using Godot;
    using Core.Interfaces.UI;
    using System;

    public interface IUIElementProvider
    {
        void ClearSource(Control source);
        T Create<T>() where T : Control, IInitializable;
        T CreateAndShowMainElement<T>() where T : Control, IInitializable, IRequireServices;
        T CreateAndShowOnUI<T>() where T : Control, IInitializable;
        T CreateAndShowWindowElement<T>() where T : Control, IInitializable, IRequireServices;
        T CreateClosableForSource<T>(Control source) where T : Control, IInitializable, IClosable, IRequireServices;
        T CreateRequireServices<T>() where T : Control, IInitializable, IRequireServices;
        T CreateRequireServicesClosable<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        T CreateSingleClosable<T>() where T : Control, IInitializable, IClosable;
        bool IsInstanceTypeExist(Type instanceType, out Control? instance);
        void Subscribe(Node layer);
        void Unload<T>() where T : Control;
    }
}
