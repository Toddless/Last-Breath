namespace Crafting.Services
{
    using Godot;
    using System;
    using Core.Data;
    using Core.Interfaces.UI;
    using Source.UIElements;
    using Internal.Inventory;
    using System.Collections.Generic;
    using Internal.Layers;

    internal class UiElementManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, IHud> _hudCache = [];
        private readonly Dictionary<Type, IWindow> _windowCache = [];
        private readonly Dictionary<Type, Func<IHud>> _hudFactories = new();

        private readonly Dictionary<Type, Func<IWindow>> _windowFactories = new()
        {
            [typeof(CraftingWindow)] = () => CraftingWindow.Initialize().Instantiate<CraftingWindow>(),
            [typeof(CraftingItems)] = () => CraftingItems.Initialize().Instantiate<CraftingItems>(),
            [typeof(InventoryWindow)] = () => InventoryWindow.Initialize().Instantiate<InventoryWindow>()
        };

        private UILayerManager? _layers;
        private IHud? _currentHud;

        public void Subscribe(Node layer)
        {
            _layers = (UILayerManager)layer;
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

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => throw new NotImplementedException();
        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory) => throw new NotImplementedException();

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
