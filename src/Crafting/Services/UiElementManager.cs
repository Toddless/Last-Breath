namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.UI;
    using Source.UIElements;
    using TestResources.Layers;
    using Godot;
    using Utilities;

    internal class UiElementManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, IHud> _hudCache = [];
        private readonly Dictionary<Type, IWindow> _windowCache = [];
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

        public void ClearCache() => throw new NotImplementedException();

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => throw new NotImplementedException();

        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory) => throw new NotImplementedException();


        // TODO: Simplify it later?

        private IWindow CreateWindowInstance(Type type)
        {
            IWindow window = type switch
            {
                _ when type == typeof(CraftingWindow) => CraftingWindow.Initialize().Instantiate<CraftingWindow>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            window.InjectServices(provider);
            return window;
        }

        private IHud CreateHudInstance(Type type)
        {
            IHud hud = type switch
            {
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            hud.InjectServices(provider);
            return hud;
        }
    }
}
