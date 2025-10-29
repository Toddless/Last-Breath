namespace LastBreath.DIComponents.Services
{
    using Godot;
    using System;
    using System.Linq;
    using Core.Interfaces.UI;
    using LastBreath.Script.UI;
    using Core.Interfaces.Data;
    using System.Collections.Generic;
    using LastBreath.Script.UI.Layers;

    internal class UIElementProvider : IUIElementProvider
    {
        private const float MOUSE_OFFSET = 15;
        private const float WINDOW_MARGIN = 20f;

        private readonly Dictionary<Type, Control> _singleInstances = [];
        private readonly IUIWindowPositionStorage _positionStorage = new UIWindowPositionStorage();
        private readonly Dictionary<Control, List<Control>> _instanceBySource = [];
        private readonly IGameServiceProvider _serviceProvider;
        private UILayersManager? _uiLayers;

        public UIElementProvider()
        {
            _serviceProvider = GameServiceProvider.Instance;
        }

        public void Subscribe(Node layer)
        {
            _uiLayers = (UILayersManager)layer;
        }

        public bool IsInstanceTypeExist(Type instanceType, out Control? exist) => _singleInstances.TryGetValue(instanceType, out exist);

        public T Create<T>()
           where T : Control, IInitializable
        {
            var instance = T.Initialize().Instantiate<T>();
            return instance;
        }

        public T CreateRequireServices<T>()
            where T : Control, IInitializable, IRequireServices
        {
            var instance = Create<T>();
            instance.InjectServices(_serviceProvider);
            return instance;
        }

        public T CreateRequireServicesClosable<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            var instance = CreateRequireServices<T>();
            instance.Close += Close;

            void Close()
            {
                if (!instance.IsQueuedForDeletion())
                    instance.QueueFree();
                instance.Close -= Close;
            }
            return instance;
        }

        public T CreateAndShowMainElement<T>()
            where T : Control, IInitializable, IRequireServices
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;
            var instance = CreateRequireServices<T>();
            _singleInstances.TryAdd(typeof(T), instance);
            _uiLayers?.ShowMainElement(instance);
            return instance;
        }

        public void HideMainElement<T>()
            where T : Control, IInitializable, IRequireServices
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayers?.RemoveMainElement(exist);
        }

        public void ShowMainElement<T>()
             where T : Control, IInitializable, IRequireServices
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayers?.ShowMainElement(exist);
        }

        public T CreateAndShowWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            ArgumentNullException.ThrowIfNull(_uiLayers);

            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;
            var instance = CreateRequireServices<T>();
            _singleInstances.TryAdd(typeof(T), instance);
            instance.Close += _uiLayers.CloseAllWindows;
            _uiLayers.ShowWindowElement(instance);
            return instance;
        }

        public void HideWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayers?.RemoveWindowElement(exist);
        }

        public void ShowWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayers?.ShowWindowElement(exist);
        }

        public T CreateAndShowNotification<T>()
            where T : Control, IInitializable
        {
            var instance = Create<T>();
            _uiLayers?.ShowNotification(instance);
            return instance;
        }

        public T CreateClosableForSource<T>(Control source)
            where T : Control, IInitializable, IClosable, IRequireServices
        {
            if (!_instanceBySource.TryGetValue(source, out var list))
            {
                list = [];
                _instanceBySource[source] = list;
            }

            var exist = list.OfType<T>().FirstOrDefault();
            if (exist != null) return exist;

            var instance = CreateRequireServices<T>();
            if (instance is IRequireReposition reposition)
                reposition.Reposition += OnRepositionRequired;

            var screenSize = _uiLayers.GetViewport().GetVisibleRect().Size;
            var windowSize = instance.GetCombinedMinimumSize();
            if (windowSize == Vector2.Zero)
                windowSize = instance.Size;

            instance.Position = CalculatePosition(list, screenSize, windowSize);

            instance.Close += UnloadForSource;

            void UnloadForSource()
            {
                list.Remove(instance);
                if (list.Count == 0) _instanceBySource.Remove(source);
                instance.Close -= UnloadForSource;
                if (instance is IRequireReposition repo) repo.Reposition -= OnRepositionRequired;
                if (!instance.IsQueuedForDeletion())
                    instance.QueueFree();
            }

            list.Add(instance);
            _uiLayers?.ShowWindowElement(instance);
            return instance;
        }
        private void OnRepositionRequired(Control source)
        {
            ArgumentNullException.ThrowIfNull(_uiLayers);

            if (!_instanceBySource.TryGetValue(source, out var list))
            {
                list = [];
                _instanceBySource[source] = list;
            }

            var screenSize = _uiLayers.GetViewport().GetVisibleRect().Size;
            var windowSize = source.GetCombinedMinimumSize();
            if (windowSize == Vector2.Zero)
                windowSize = source.Size;

            source.Position = CalculatePosition(list, screenSize, windowSize);
        }
        public void ClearSource(Control source)
        {
            if (!_instanceBySource.TryGetValue(source, out var existList)) return;

            foreach (var ui in existList ?? [])
                ui.QueueFree();
        }
        public T CreateSingleClosable<T>()
            where T : Control, IInitializable, IClosable
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;

            var instance = Create<T>();

            var savedPos = _positionStorage.GetPosition<T>();
            if (instance is IRequireServices require)
                require.InjectServices(_serviceProvider);

            instance.Position = savedPos ?? Vector2.Zero;

            instance.Close += Unload<T>;
            _singleInstances[typeof(T)] = instance;
            return instance;
        }
        public void Unload<T>()
            where T : Control
        {
            if (_singleInstances.TryGetValue(typeof(T), out var control))
            {
                if (control is IClosable close)
                    close.Close -= Unload<T>;
                control.QueueFree();
                _singleInstances.Remove(typeof(T));
            }
        }

        private Vector2 CalculatePosition(List<Control> existingInstances, Vector2 screenSize, Vector2 windowSize)
        {
            if (existingInstances.Count == 0)
            {
                var mousePosition = _uiLayers?.GetViewport().GetMousePosition() ?? Vector2.Zero;

                var position = mousePosition + new Vector2(MOUSE_OFFSET, -windowSize.Y - MOUSE_OFFSET);
                if (position.Y < WINDOW_MARGIN)
                    position = mousePosition + new Vector2(MOUSE_OFFSET, MOUSE_OFFSET);
                return ClampToScreen(position, windowSize, screenSize);
            }

            var lastWindow = existingInstances[^1];
            var positionBelow = lastWindow.Position + new Vector2(0, lastWindow.Size.Y + WINDOW_MARGIN);

            if (positionBelow.Y + windowSize.Y <= screenSize.Y - WINDOW_MARGIN)
                return ClampToScreen(positionBelow, windowSize, screenSize);

            var firstWindow = existingInstances[0];
            var positionRight = firstWindow.Position + new Vector2(firstWindow.Size.X + WINDOW_MARGIN, 0);
            if (positionRight.X + windowSize.X <= screenSize.X - WINDOW_MARGIN)
                return ClampToScreen(positionRight, windowSize, screenSize);

            var positionLeft = firstWindow.Position - new Vector2(windowSize.X + WINDOW_MARGIN, 0);
            if (positionLeft.X >= WINDOW_MARGIN)
                return ClampToScreen(positionLeft, windowSize, screenSize);

            return ClampToScreen(new Vector2(WINDOW_MARGIN, WINDOW_MARGIN), windowSize, screenSize);
        }

        private Vector2 ClampToScreen(Vector2 pos, Vector2 size, Vector2 screen)
        {
            float x = Mathf.Clamp(pos.X, WINDOW_MARGIN, screen.X - size.X - WINDOW_MARGIN);
            float y = Mathf.Clamp(pos.Y, WINDOW_MARGIN, screen.Y - size.Y - WINDOW_MARGIN);
            return new Vector2(x, y);
        }
    }
}
