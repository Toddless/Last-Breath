namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Godot;
    using Interfaces.UI;

    /// <inheritdoc cref="IUiElementsManager"/>
    public class UiElementsManager(IGameServiceProvider provider) : IUiElementsManager
    {
        private readonly Dictionary<Type, Func<IHud>> _hudFactories = [];
        private readonly Dictionary<Type, Func<IWindow>> _windowFactories = [];
        private readonly Dictionary<Type, IWindow> _openWindows = [];
        private ILayerManager? _layers;
        private IHud? _currentHud;

        public void Subscribe(ILayerManager layers) => _layers = layers;

        public IHud ChangeHud(Type hudType)
        {
            RemoveCurrentHud();
            var hud = CreateElement(_hudFactories, hudType);
            _layers?.ShowHud(hud);
            _currentHud = hud;
            return hud;
        }

        public IWindow OpenWindow(Type windowType)
        {
            if (TryGetOpenWindow(windowType, out var openWindow))
            {
                CloseWindow(windowType, openWindow);
                return openWindow;
            }

            var window = CreateElement(_windowFactories, windowType);
            _openWindows[windowType] = window;
            _layers?.ShowWindow(window);
            return window;
        }

        public bool RegisterHudFactory(Type hudType, Func<IHud> factory) => _hudFactories.TryAdd(hudType, factory);

        public bool RegisterWindowFactory(Type windowType, Func<IWindow> factory) => _windowFactories.TryAdd(windowType, factory);

        private TElement CreateElement<TElement>(Dictionary<Type, Func<TElement>> factories, Type type)
            where TElement : IRequireServices
        {
            if (!factories.TryGetValue(type, out var factory))
                throw new KeyNotFoundException(
                    $"No factory registered for UI element '{type.Name}'. Register it in the project's bootstrap.");

            var element = factory();
            element.InjectServices(provider);
            return element;
        }

        private void RemoveCurrentHud()
        {
            _currentHud?.Remove();
            FreeIfValid(_currentHud);
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
            if (window is Node node && GodotObject.IsInstanceValid(node) && node.IsInsideTree()) return true;

            _openWindows.Remove(windowType);
            return false;
        }
    }
}
