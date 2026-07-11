namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Godot;
    using Localization;
    using Views;
    using Views.UI;

    /// <inheritdoc cref="IUiElementsManager"/>
    public class UiElementsManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, Func<IHud>> _hudFactories = [];
        private readonly Dictionary<Type, Func<IWindow>> _windowFactories = [];
        private readonly Dictionary<Type, Func<IPopup>> _popupFactories = [];
        private readonly Dictionary<Type, IWindow> _openWindows = [];
        private readonly Dictionary<Type, IPopup> _openPopups = [];
        private ILayerManager? _layers;
        private IHud? _currentHud;

        public void Subscribe(ILayerManager? layers) => _layers = layers;

        public IHud ChangeHud(Type hudType)
        {
            RemoveCurrentHud();
            var hud = CreateElement(_hudFactories, hudType);
            _layers?.ShowHud(hud);
            _currentHud = hud;
            return hud;
        }

        public IWindow ToggleWindow(Type windowType)
        {
            if (!TryGetOpenWindow(windowType, out var openWindow)) return OpenFreshWindow(windowType);

            CloseWindow(windowType, openWindow);
            return openWindow;
        }

        public IWindow OpenWindow(Type windowType) =>
            TryGetOpenWindow(windowType, out var openWindow) ? openWindow : OpenFreshWindow(windowType);

        public IPopup ShowPopup(Type popupType)
        {
            if (_openPopups.Remove(popupType, out var previous))
                FreeIfValid(previous);

            var popup = CreateElement(_popupFactories, popupType);
            _openPopups[popupType] = popup;
            _layers?.ShowOverlay(popup);
            return popup;
        }

        public void ShowKeyword(KeywordTooltipView view, Vector2 globalPosition)
        {
            var popup = ShowPopup(typeof(IKeywordTooltipPopup)) as IKeywordTooltipPopup;
            popup?.ShowKeyword(view, globalPosition);
        }

        public bool HandleEscape()
        {
            if (CloseOverlayLayer()) return true;
            return CloseDismissableWindows();
        }

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => _hudFactories.TryAdd(hudType, factory);

        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory) => _windowFactories.TryAdd(windowType, factory);

        public bool RegisterPopupFactory(Type popupType, Func<IPopup> factory) => _popupFactories.TryAdd(popupType, factory);

        private IWindow OpenFreshWindow(Type windowType)
        {
            var window = CreateElement(_windowFactories, windowType);
            _openWindows[windowType] = window;
            _layers?.ShowWindow(window);
            return window;
        }

        private bool CloseOverlayLayer()
        {
            _openPopups.Clear(); // the layer frees the nodes below
            return _layers?.CloseOverlays() ?? false;
        }

        private bool CloseDismissableWindows()
        {
            bool closedAny = false;
            foreach ((Type type, IWindow window) in _openWindows.ToList())
            {
                if (!TryGetOpenWindow(type, out _)) continue; // stale entry, already gone
                if (!window.IsDismissable) continue;
                CloseWindow(type, window);
                closedAny = true;
            }

            return closedAny;
        }

        private TElement CreateElement<TElement>(Dictionary<Type, Func<TElement>> factories, Type type)
        {
            if (!factories.TryGetValue(type, out var factory))
                throw new KeyNotFoundException(
                    $"No factory registered for UI element '{type.Name}'. Register it in the project's bootstrap.");

            var element = factory();
            if (element is IRequireServices requireServices)
                requireServices.InjectServices(provider);
            return element;
        }

        private void RemoveCurrentHud()
        {
            // A scene reload frees the HUD together with the old tree while this manager
            // survives as a service — calling into the disposed instance would crash here.
            if (_currentHud is Node node && GodotObject.IsInstanceValid(node))
            {
                _currentHud.Remove();
                node.QueueFree();
            }

            _currentHud = null;
        }

        private void CloseWindow(Type windowType, IWindow window)
        {
            _openWindows.Remove(windowType);
            window.Close();
            FreeIfValid(window);
        }

        /// <summary>Fresh-instance policy: closed UI is freed. An element may have freed itself already.</summary>
        private static void FreeIfValid(object? element)
        {
            if (element is Node node && GodotObject.IsInstanceValid(node))
                node.QueueFree();
        }

        /// <summary>Windows can close themselves (close button), leaving a stale tracking entry behind.</summary>
        private bool TryGetOpenWindow(Type windowType, out IWindow window)
        {
            if (!_openWindows.TryGetValue(windowType, out window!)) return false;
            if (window is Node node && GodotObject.IsInstanceValid(node)) return true;

            _openWindows.Remove(windowType);
            return false;
        }
    }
}
