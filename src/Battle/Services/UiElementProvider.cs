namespace Battle.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Data;
    using Core.Interfaces.UI;
    using Godot;

    internal class UiElementProvider : IUIElementProvider
    {
        private const float MouseOffset = 15;
        private const float WindowMargin = 20f;

        private readonly Dictionary<Type, Control> _singleInstances = [];

        private readonly IUIWindowPositionStorage _positionStorage = new UiWindowPositionStorage();
        private readonly Dictionary<Control, List<Control>> _instanceBySource = [];
        private readonly IGameServiceProvider _serviceProvider = GameServiceProvider.Instance;
        private UiLayerManager? _uiLayer;

        public void Subscribe(Node layer)
        {
            _uiLayer = (UiLayerManager)layer;
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
            {
                _uiLayer?.ShowMainElement(exist);
                return (T)exist;
            }

            var instance = CreateRequireServices<T>();
            _singleInstances.TryAdd(typeof(T), instance);
            _uiLayer?.ShowMainElement(instance);
            return instance;
        }

        public void HideMainElement<T>()
            where T : Control, IInitializable, IRequireServices
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayer?.RemoveMainElement(exist);
        }

        public void ShowMainElement<T>()
            where T : Control, IInitializable, IRequireServices
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayer?.ShowMainElement(exist);
        }

        public T CreateAndShowWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            ArgumentNullException.ThrowIfNull(_uiLayer);

            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;
            var instance = CreateRequireServices<T>();
            _singleInstances.TryAdd(typeof(T), instance);
            instance.Close += OnClose;

            void OnClose()
            {
                _uiLayer.RemoveWindowElement(instance);
                if (instance.IsQueuedForDeletion())
                    instance.Close -= OnClose;
            }

            _uiLayer.ShowWindowElement(instance);
            return instance;
        }

        public void HideWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayer?.RemoveWindowElement(exist);
        }

        public void ShowWindowElement<T>()
            where T : Control, IInitializable, IRequireServices, IClosable
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var exist))
                ArgumentNullException.ThrowIfNull(exist);

            _uiLayer?.ShowWindowElement(exist);
        }

        public T CreateAndShowTooltip<T>()
            where T : Control, IInitializable, IRequireServices, IClosable, IRequireReposition
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;

            var instance = CreateRequireServices<T>();
            instance.Reposition += OnRepositionRequired;
            //instance.Close += OnClose;

            //void OnClose()
            //{
            //    instance.Reposition -= OnRepositionRequired;
            //    instance.Close -= OnClose;
            //}

            _singleInstances.Add(typeof(T), instance);
            _uiLayer?.ShowTooltipElement(instance);
            return instance;
        }

        public T CreateAndShowNotification<T>()
            where T : Control, IInitializable
        {
            var instance = Create<T>();
            _uiLayer?.ShowNotification(instance);
            return instance;
        }

        public void RemoveAllInstances()
        {
            ArgumentNullException.ThrowIfNull(_uiLayer);
            foreach (var instance in _singleInstances)
            {
                if (instance.Value is IClosable closable)
                    closable.Close -= _uiLayer.CloseAllWindows;
                instance.Value.QueueFree();
            }

            _singleInstances.Clear();
        }

        public T CreateClosableForSource<T>(Control source)
            where T : Control, IInitializable, IClosable, IRequireServices
        {
            ArgumentNullException.ThrowIfNull(_uiLayer);
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

            var screenSize = _uiLayer.GetViewport().GetVisibleRect().Size;
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
            _uiLayer.ShowWindowElement(instance);
            return instance;
        }

        private void OnRepositionRequired(Control source)
        {
            ArgumentNullException.ThrowIfNull(_uiLayer);

            var screenSize = _uiLayer.GetViewport().GetVisibleRect().Size;
            var windowSize = source.GetCombinedMinimumSize();
            if (windowSize == Vector2.Zero)
                windowSize = source.Size;

            var mousePosition = _uiLayer.GetViewport().GetMousePosition();
            var position = mousePosition + new Vector2(MouseOffset, -windowSize.Y - MouseOffset);
            if (position.Y < WindowMargin)
                position = mousePosition + new Vector2(MouseOffset, MouseOffset);

            source.Position = ClampToScreen(position, windowSize, screenSize);
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
                var mousePosition = _uiLayer?.GetViewport().GetMousePosition() ?? Vector2.Zero;

                var position = mousePosition + new Vector2(MouseOffset, -windowSize.Y - MouseOffset);
                if (position.Y < WindowMargin)
                    position = mousePosition + new Vector2(MouseOffset, MouseOffset);
                return ClampToScreen(position, windowSize, screenSize);
            }

            var lastWindow = existingInstances[^1];
            var positionBelow = lastWindow.Position + new Vector2(0, lastWindow.Size.Y + WindowMargin);

            if (positionBelow.Y + windowSize.Y <= screenSize.Y - WindowMargin)
                return ClampToScreen(positionBelow, windowSize, screenSize);

            var firstWindow = existingInstances[0];
            var positionRight = firstWindow.Position + new Vector2(firstWindow.Size.X + WindowMargin, 0);
            if (positionRight.X + windowSize.X <= screenSize.X - WindowMargin)
                return ClampToScreen(positionRight, windowSize, screenSize);

            var positionLeft = firstWindow.Position - new Vector2(windowSize.X + WindowMargin, 0);
            if (positionLeft.X >= WindowMargin)
                return ClampToScreen(positionLeft, windowSize, screenSize);

            return ClampToScreen(new Vector2(WindowMargin, WindowMargin), windowSize, screenSize);
        }

        private Vector2 ClampToScreen(Vector2 pos, Vector2 size, Vector2 screen)
        {
            float x = Mathf.Clamp(pos.X, WindowMargin, screen.X - size.X - WindowMargin);
            float y = Mathf.Clamp(pos.Y, WindowMargin, screen.Y - size.Y - WindowMargin);
            return new Vector2(x, y);
        }
    }
}
