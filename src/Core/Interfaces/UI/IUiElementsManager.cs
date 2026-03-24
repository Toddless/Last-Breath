namespace Core.Interfaces.UI
{
    using System;
    using Godot;

    public interface IUiElementsManager
    {
        void Subscribe(Node layer);
        IHud ChangeHud(Type hudType);
        IWindow OpenWindow(Type windowType);
        void ClearCache();
        bool RegisterHudFactory(Type hudType, Func<IHud> factory);
        bool RegisterWindowFactory(Type windowType, Func<IWindow> factory);
    }
}
