namespace Crafting.Services.DI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.UI;
    using Crafting.Source.UIElements;
    using Crafting.TestResources.Layers;
    using Godot;
    using Utilities;

    internal class UIElementProvider : IUIElementProvider
    {
        private const float MOUSE_OFFSET = 15;
        private const float WINDOW_MARGIN = 20f;

        private readonly Dictionary<Type, Control> _singleInstances = [];
        private readonly UIWindowStateStorage _positionStorage = new();
        private readonly Dictionary<Control, List<Control>> _instanceBySource = [];
        private readonly IGameServiceProvider _serviceProvider;
        private UILayerManager? _uiLayer;

        public UIElementProvider()
        {
            _serviceProvider = ServiceProvider.Instance;
        }

        public void Subscribe(Node layer)
        {
            _uiLayer = (UILayerManager)layer;
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

        public T CreateAndShowMainElement<T>()
            where T : Control, IInitializable, IRequireServices
        {
            if (_singleInstances.TryGetValue(typeof(T), out var exist))
                return (T)exist;
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
            instance.Close += _uiLayer.CloseAllWindows;
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

        public T CreateAndShowNotification<T>()
            where T : Control, IInitializable
        {
            var instance = Create<T>();
            _uiLayer?.ShowNotification(instance);
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


        public T CreateAndShowOnUI<T>()
            where T : Control, IInitializable
        {
            var instance = Create<T>();
            _uiLayer?.ShowWindowElement(instance);
            return instance;
        }

        public T CreateClosableForSource<T>(Control source)
            where T : Control, IInitializable, IClosable, IRequireServices
        {
            if (_uiLayer == null) throw new InvalidOperationException();

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
            _uiLayer?.ShowWindowElement(instance);
            return instance;
        }

        private void OnRepositionRequired(Control source)
        {
            ArgumentNullException.ThrowIfNull(_uiLayer);

            if (!_instanceBySource.TryGetValue(source, out var list))
            {
                list = [];
                _instanceBySource[source] = list;
            }

            var screenSize = _uiLayer.GetViewport().GetVisibleRect().Size;
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

            var instance = CreateAndShowOnUI<T>();

            var savedPos = _positionStorage.GetPosition<T>();
            if (instance is IRequireServices require)
                require.InjectServices(_serviceProvider);

            instance.Position = savedPos ?? Vector2.Zero;

            if (instance is DraggableWindow drag)
                drag.PositionChangedExternally += _positionStorage.SavePosition<T>;

            instance.Close += Unload<T>;
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

        public ItemCreatedNotifier CreateItemCreatedNotifier(IItem item)
        {
            var notifier = Create<ItemCreatedNotifier>();
            notifier.SetItemDetails(CreateItemDetails(item));
            return notifier;
        }

        public ItemDetails CreateItemDetails(IItem item, Control source)
        {
            var itemDetails = CreateClosableForSource<ItemDetails>(source);

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return ConfigureItemDetails(itemDetails, item);
        }

        private ItemDetails CreateItemDetails(IItem item)
        {
            var itemDetails = CreateRequireServices<ItemDetails>();

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return ConfigureItemDetails(itemDetails, item);
        }

        private ItemDetails ConfigureItemDetails(ItemDetails itemDetails, IItem item)
        {
            itemDetails.Clear();
            itemDetails.SetItemIcon(item.Icon!);
            itemDetails.SetItemName(item.DisplayName);

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
                var selectable = new InteractiveLabel();
                selectable.SetSelectable(false);
                selectable.SetText(Lokalizator.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new InteractiveLabel();
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

        private Vector2 CalculatePosition(List<Control> existingInstances, Vector2 screenSize, Vector2 windowSize)
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
