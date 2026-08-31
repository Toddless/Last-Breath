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
        private readonly Dictionary<Type, UiContext> _windowContexts = [];
        private readonly Dictionary<Type, IWindow> _openWindows = [];
        private readonly Dictionary<Type, IPopup> _openPopups = [];
        private ILayerManager? _layers;
        private IHud? _currentHud;
        private IUiContextService? _context;
        private bool _contextResolved;

        public void Subscribe(ILayerManager? layers) => _layers = layers;

        public IHud ChangeHud(Type hudType)
        {
            RemoveCurrentHud();
            var hud = CreateElement(_hudFactories, hudType);
            _layers?.ShowHud(hud);
            _currentHud = hud;
            return hud;
        }

        public IWindow? ToggleWindow(Type windowType)
        {
            if (!TryGetOpenWindow(windowType, out var openWindow)) return OpenFreshWindow(windowType);

            CloseWindow(windowType, openWindow);
            return openWindow;
        }

        public IWindow? OpenWindow(Type windowType) =>
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

        public bool HasMovementBlockingWindow =>
            _openWindows.ToList().Any(pair => TryGetOpenWindow(pair.Key, out var window) && window.BlocksMovement);

        public bool HandleEscape()
        {
            if (CloseOverlayLayer()) return true;
            return CloseDismissableWindows();
        }

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => _hudFactories.TryAdd(hudType, factory);

        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory, UiContext allowedIn = UiContext.All)
        {
            EnsureContextService(); // registration happens at bootstrap — the context tracker must live from the start
            if (!_windowFactories.TryAdd(windowType, factory)) return false;
            _windowContexts[windowType] = allowedIn;
            return true;
        }

        public bool RegisterPopupFactory(Type popupType, Func<IPopup> factory) => _popupFactories.TryAdd(popupType, factory);

        public bool HasPopupFactory(Type popupType) => _popupFactories.ContainsKey(popupType);

        private IWindow? OpenFreshWindow(Type windowType)
        {
            if (!IsAllowedNow(windowType)) return null; // silent by design (Todd)

            var window = CreateElement(_windowFactories, windowType);
            _openWindows[windowType] = window;
            _layers?.ShowWindow(window);
            return window;
        }

        private bool IsAllowedNow(Type windowType)
        {
            EnsureContextService();
            if (_context == null) return true; // a project without the context tracker gates nothing
            return (_windowContexts.GetValueOrDefault(windowType, UiContext.All) & _context.Current) != 0;
        }

        /// <summary>Resolved lazily — the manager is constructed while the container is still being built.</summary>
        private void EnsureContextService()
        {
            if (_contextResolved) return;
            _contextResolved = true;
            _context = provider.GetServices<IUiContextService>().FirstOrDefault();
            if (_context != null) _context.ContextChanged += CloseDisallowedWindows;
        }

        /// <summary>A context switch (battle starts, dialogue opens...) slams the windows that don't belong in it.</summary>
        private void CloseDisallowedWindows(UiContext context)
        {
            foreach ((Type type, IWindow window) in _openWindows.ToList())
            {
                if (!TryGetOpenWindow(type, out _)) continue;
                if ((_windowContexts.GetValueOrDefault(type, UiContext.All) & context) != 0) continue;
                CloseWindow(type, window);
            }
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
            // The disposed-instance check only applies to nodes (non-node windows exist in tests).
            if (window is not Node node) return true;
            if (GodotObject.IsInstanceValid(node)) return true;

            _openWindows.Remove(windowType);
            return false;
        }
    }
}
