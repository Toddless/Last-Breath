namespace LastBreath.Services
{
    using Godot;
    using System;
    using UI.View;
    using Inventory;
    using Core.Data;
    using Source.UI.Layers;
    using Core.Interfaces.UI;
    using System.Collections.Generic;

    public class UiElementsManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, IHud> _hudCache = [];
        private readonly Dictionary<Type, IWindow> _windowCache = [];
        private readonly Dictionary<Type, Func<IHud>> _hudFactories = new() { [typeof(PlayerHud)] = () => PlayerHud.Initialize().Instantiate<PlayerHud>() };
        private readonly Dictionary<Type, Func<IWindow>> _windowFactories = new() { [typeof(InventoryWindow)] = () => InventoryWindow.Initialize().Instantiate<InventoryWindow>() };
        private UILayersManager? _layers;
        private IHud? _currentHud;

        public void Subscribe(Node layer)
        {
            _layers = (UILayersManager)layer;
        }

        public IHud ChangeHud(Type hudType)
        {
            if (!_hudCache.TryGetValue(hudType, out var hud))
            {
                hud = CreateHudInstance(hudType);
                _hudCache[hudType] = hud;
            }

            _currentHud?.Remove();
            _layers?.ShowHud(hud);
            _currentHud = hud;
            return hud;
        }

        public IWindow OpenWindow(Type windowType)
        {
            if (!_windowCache.TryGetValue(windowType, out var window))
            {
                window = CreateWindowInstance(windowType);
                _windowCache[windowType] = window;
            }

            if (window.IsAlreadyVisible) window.Close();
            else _layers?.ShowWindow(window);
            return window;
        }

        public void ClearCache()
        {
            _hudCache.Clear();
            _windowCache.Clear();
        }

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => _hudFactories.TryAdd(hudType, factory);
        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory) => _windowFactories.TryAdd(windowType, factory);

        private IWindow CreateWindowInstance(Type type)
        {
            if (!_windowFactories.TryGetValue(type, out var factory))
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            var window = factory();
            window.InjectServices(provider);
            return window;
        }

        private IHud CreateHudInstance(Type type)
        {
            if (!_hudFactories.TryGetValue(type, out var factory))
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            var hud = factory();
            hud.InjectServices(provider);
            return hud;
        }
    }
}
