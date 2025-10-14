namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Crafting.Source.UIElements;
    using System.Collections.Generic;
    using Crafting.TestResources.DI;
    using Crafting.Source.UIElements.Layers;
    using System.Linq;

    public class UIElementProvider
    {
        private const float MOUSE_OFFSET = 10;
        private const float WINDOW_MARGIN = 10f;

        private readonly Dictionary<Type, Control> _singleInstances = [];
        private readonly UIWindowStateStorage _positionStorage = new();
        private readonly Dictionary<Control, List<Control>> _instanceBySource = [];
        private readonly Core.Interfaces.Data.IServiceProvider _serviceProvider;
        private UILayer? _uiLayer;

        public UIElementProvider()
        {
            _serviceProvider = ServiceProvider.Instance;
        }

        public void Subscribe(CanvasLayer layer) => _uiLayer = (UILayer)layer;

        public T Create<T>()
            where T : Control, IInitializable, IClosable
        {
            var instance = T.Initialize().Instantiate<T>();
            if (instance is IRequireServices require)
                require.InjectServices(_serviceProvider);
            instance.Close += instance.QueueFree;
            return instance;
        }

        public T Create<T>(Action<T> configure)
            where T : Control, IInitializable, IClosable
        {
            var instance = Create<T>();
            configure?.Invoke(instance);
            return instance;
        }

        public T Create<T>(Control source)
            where T : Control, IInitializable, IClosable
        {
            if (_uiLayer == null) throw new InvalidOperationException();

            if (!_instanceBySource.TryGetValue(source, out var list))
            {
                list = [];
                _instanceBySource[source] = list;
            }

            var exist = list.OfType<T>().FirstOrDefault();
            if (exist != null) return exist;

            var instance = Create<T>();

            var screenSize = _uiLayer.GetViewport().GetVisibleRect().Size;
            var windowSize = instance.GetCombinedMinimumSize();
            if (windowSize == Vector2.Zero)
                windowSize = instance.Size;

            instance.Position = CalculatePosition(source, instance, list, screenSize, windowSize);

            instance.Close += UnloadForSource;

            void UnloadForSource()
            {
                list.Remove(instance);
                if (list.Count == 0) _instanceBySource.Remove(source);
                instance.Close -= UnloadForSource;
                instance.QueueFree();
            }


            list.Add(instance);
            _uiLayer?.ShowWindow(instance);
            return instance;
        }

        public void ClearSource(Control source)
        {
            if (!_instanceBySource.TryGetValue(source, out var existList)) return;

            foreach (var ui in existList ?? [])
                ui.QueueFree();
            _instanceBySource.Remove(source);
        }

        public T CreateSingleClosable<T>()
            where T : Control, IInitializable, IClosable
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;

            var instance = Create<T>();

            var savedPos = _positionStorage.GetPosition<T>();

            instance.Position = savedPos ?? Vector2.Zero;

            if (instance is DraggableWindow drag)
                drag.PositionChangedExternally += _positionStorage.SavePosition<T>;

            instance.Close += Unload<T>;
            _singleInstances[typeof(T)] = instance;
            _uiLayer?.ShowWindow(instance);
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

        public ItemCreatedNotifier CreateItemCreatedNotifier(IItem item)
        {
            var details = CreateItemDetails(item);
            var notifier = Create<ItemCreatedNotifier>(X => X.SetItemDetails(details));
            _uiLayer?.ShowWindow(notifier);
            return notifier;
        }

        public ItemDetails CreateItemDetails(IItem item, Control source)
        {
            var itemDetails = Create<ItemDetails>(source);

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return itemDetails;
        }

        private ItemDetails CreateItemDetails(IItem item)
        {
            var itemDetails = Create<ItemDetails>();

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return itemDetails;
        }

        private ItemDetails ConfigureEquipItemDetails(ItemDetails itemDetails, IEquipItem equip)
        {
            itemDetails.Clear();
            itemDetails.SetItemIcon(equip.Icon!);
            itemDetails.SetItemName(equip.DisplayName);
            itemDetails.SetItemUpdateLevel(equip.UpdateLevel);
            foreach (var item in equip.BaseModifiers)
            {
                var selectable = new SelectableItem();
                selectable.SetSelectable(false);
                selectable.SetText(Lokalizator.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new SelectableItem();
                stat.SetText(Lokalizator.Format(modifier));
                stat.SetSelectable(false);
                itemDetails.SetItemAdditionalStats(stat);
            }

            if (equip.Skill != null)
            {
                var skill = equip.Skill;
                var skillDescription = SkillDescription.Initialize().Instantiate<SkillDescription>();
                skillDescription.SetSkillName(skill.DisplayName);
                skillDescription.SetSkillDescription(skill.Description);
                itemDetails.SetSkillDescription(skillDescription);
            }

            return itemDetails;
        }


        private Vector2 CalculatePosition(Control source, Control instance, List<Control> existingInstances, Vector2 screenSize, Vector2 windowSize)
        {
            if (existingInstances.Count == 0)
            {
                var mousePosition = _uiLayer?.GetViewport().GetMousePosition() ?? Vector2.Zero;

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
