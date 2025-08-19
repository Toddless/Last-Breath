namespace LastBreath.Script.UI
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Godot;
    using Godot.Collections;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Inventory;

    public partial class PlayerInventoryUI : Control
    {
        [Export] private GridContainer? _equipInventory, _craftInventory, _questItemsInventory;
        [Export] private TabBar? _equip, _craft, _quest;
        private Dictionary<EquipmentPart, EquipmentSlot> _slots = [];
        [Export] private Array<EquipmentSlot> _ringSlots = [];

        public event Action<IItem, MouseButtonPressed, IInventory>? InventorySlotClicked;
        public event Action<EquipmentSlot, MouseButtonPressed>? EquipmentSlotPressed;

        public override void _Ready()
        {
            FillTheDictionary();
            SetEvents();
        }

        public void InitializeInventories(Inventory equipInventory, Inventory craftingInventory, Inventory questItemsInventory)
        {
            equipInventory.Initialize(220, _equipInventory!);
            craftingInventory.Initialize(220, _craftInventory!);
            questItemsInventory.Initialize(220, _questItemsInventory!);
            equipInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
            craftingInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
            questItemsInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
        }

        public EquipmentSlot? GetEquipmentSlot(EquipmentPart part)
        {
            if (part == EquipmentPart.Ring)
            {
                return GetFreeRingSlotOrDefault();
            }
            else
            {
                _slots.TryGetValue(part, out var slot);
                if (slot == null)
                {
                    //TODO Log
                }
                return slot;
            }
        }

        private void OnEquipItemPressed(EquipmentSlot slot, MouseButtonPressed pressed) => EquipmentSlotPressed?.Invoke(slot, pressed);

        private EquipmentSlot GetFreeRingSlotOrDefault() => _ringSlots[0].CurrentItem == null ? _ringSlots[0] : _ringSlots[1];

        private void FillTheDictionary()
        {
            // Adding slot via [Export] always random, slots will be added like this instead
            _slots.Add(EquipmentPart.BodyArmor, FindEquipmentSlot("BodyArmorSlot"));
            _slots.Add(EquipmentPart.Amulet, FindEquipmentSlot("AmuletSlot"));
            _slots.Add(EquipmentPart.Boots, FindEquipmentSlot("BootsSlot"));
            _slots.Add(EquipmentPart.Gloves, FindEquipmentSlot("GlovesSlot"));
            _slots.Add(EquipmentPart.Cloak, FindEquipmentSlot("CloakSlot"));
            _slots.Add(EquipmentPart.Helmet, FindEquipmentSlot("HelmetSlot"));
            _slots.Add(EquipmentPart.Belt, FindEquipmentSlot("BeltSlot"));
            _slots.Add(EquipmentPart.Weapon, FindEquipmentSlot("WeaponSlot"));
            NodeFinder.ClearCache();
        }

        private EquipmentSlot FindEquipmentSlot(string name)
        {
            var slot = (EquipmentSlot?)NodeFinder.FindBFSCached(this, name);
            if (slot == null)
            {
                // TODO Log
                // can cause item disappearing on equip. Change it later
                slot = new();
            }

            return slot;
        }

        private void SetEvents()
        {
            foreach (var slot in _slots)
            {
                slot.Value.EquipItemPressed += OnEquipItemPressed;
            }

            foreach (var ringSlot in _ringSlots)
            {
                ringSlot.EquipItemPressed += OnEquipItemPressed;
            }
        }
    }
}
