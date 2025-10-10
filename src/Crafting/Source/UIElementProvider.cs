namespace Crafting.Source
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using System.Collections.Generic;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Data;
    using Crafting.TestResources.DI;

    public class UIElementProvider
    {
        private readonly Dictionary<Type, Control> _singleInstances = [];
        private readonly List<Control> _instances = [];
        private readonly UIWindowStateStorage _positionStorage = new();

        public T CreateSingleClosableOrGet<T>(Node parent)
            where T : Control, IInitializable, IClosable
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;

            var instance = T.Initialize().Instantiate<T>();

            var savedPos = _positionStorage.GetPosition<T>();

            instance.Position = savedPos == null ? parent.GetViewport().GetVisibleRect().Position : savedPos.Value;

            if (instance is DraggableWindow drag)
                drag.PositionChangedExternally += _positionStorage.SavePosition<T>;
            if (instance is IRequireServices require)
                require.InjectServices(ServiceProvider.Instance);

            instance.Close += Unload<T>;
            parent.AddChild(instance);
            _singleInstances[typeof(T)] = instance;

            return instance;
        }

        public void Unload<T>()
            where T : Control
        {
            if (_singleInstances.TryGetValue(typeof(T), out var control))
            {
                if (control is DraggableWindow draggable)
                    draggable.PositionChangedExternally -= _positionStorage.SavePosition<T>;
                if (control is IClosable close)
                    close.Close -= Unload<T>;
                control.QueueFree();
                _singleInstances.Remove(typeof(T));
            }
        }

    }
}
