namespace Core.Interfaces.Data
{
    using Godot;
    using System;
    using Core.Interfaces.UI;

    public interface IUIElementProvider
    {
        T Create<T>() where T : Control, IInitializable;
        T CreateAndShowMainElement<T>() where T : Control, IInitializable, IRequireServices;
        T CreateAndShowWindowElement<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        T CreateClosableForSource<T>(Control source) where T : Control, IInitializable, IClosable, IRequireServices;
        T CreateRequireServices<T>() where T : Control, IInitializable, IRequireServices;
        T CreateRequireServicesClosable<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        T CreateSingleClosable<T>() where T : Control, IInitializable, IClosable;
        bool IsInstanceTypeExist(Type instanceType, out Control? exist);
        void HideWindowElement<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        void ShowWindowElement<T>() where T : Control, IInitializable, IRequireServices, IClosable;
        void HideMainElement<T>() where T : Control, IInitializable, IRequireServices;
        void ShowMainElement<T>() where T : Control, IInitializable, IRequireServices;
        void Subscribe(Node layer);
        void Unload<T>() where T : Control;
        void ClearSource(Control source);
        T CreateAndShowNotification<T>() where T : Control, IInitializable;
    }
}
