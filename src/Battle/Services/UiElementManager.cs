namespace Battle.Services
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.UI;
    using Godot;
    using Source.UIElements;

    internal class UiElementManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, IHud> _hudCache = [];
        private readonly Dictionary<Type, IWindow> _windowCache = [];
        private UiLayerManager? _layers;
        private IHud? _currentHud;


        public void Subscribe(Node layer)
        {
            _layers = (UiLayerManager)layer;
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
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            window.InjectServices(provider);
            return window;
        }

        private IHud CreateHudInstance(Type type)
        {
            IHud hud = type switch
            {
                _ when type == typeof(BattleHud) => BattleHud.Initialize().Instantiate<BattleHud>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            hud.InjectServices(provider);
            return hud;
        }
    }
}
