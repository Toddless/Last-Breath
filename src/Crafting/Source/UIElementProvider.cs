namespace Crafting.Source
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Crafting.TestResources.DI;
    using Crafting.Source.UIElements;
    using System.Collections.Generic;
    using Crafting.Source.UIElements.Layers;

    public class UIElementProvider
    {
        private readonly Dictionary<Type, Control> _singleInstances = [];
        private readonly UIWindowStateStorage _positionStorage = new();
        private UILayer? _uiLayer;

        public void Subscribe(CanvasLayer layer) => _uiLayer = (UILayer)layer;

        public T Create<T>()
            where T : Control, IInitializable
        {
            var instance = T.Initialize().Instantiate<T>();
            if (instance is IRequireServices require)
                require.InjectServices(ServiceProvider.Instance);
            return instance;
        }

        public T Create<T>(Action<T> configure)
            where T : Control, IInitializable
        {
            var instance = Create<T>();
            configure?.Invoke(instance);
            return instance;
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

        public ItemDetails CreateItemDetails(IItem item)
        {
            var itemDetails = Create<ItemDetails>();

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return itemDetails;
        }

        private ItemDetails ConfigureEquipItemDetails(ItemDetails itemDetails, IEquipItem equip)
        {
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
    }
}
