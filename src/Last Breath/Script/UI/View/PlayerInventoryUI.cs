namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Script.Enums;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class PlayerInventoryUI : Control
    {
        [Export] private GridContainer? _equipInventory, _craftInventory, _questItemsInventory;
        [Export] private TabBar? _equip, _craft, _quest;
        [Export] private Dictionary<EquipmentPart, EquipmentSlot> _slots = [];

        public event Action<Item, MouseButtonPressed, Inventory>? InventorySlotClicked;
        public event Action<EquipmentSlot, MouseButtonPressed>? EquipItemPressed;

        public override void _Ready()
        {
            SetEvents();
        }

        private void SetEvents()
        {
            foreach (var slot in _slots)
            {
                slot.Value.EquipItemPressed += OnEquipItemPressed;
            }
        }

        private void OnEquipItemPressed(EquipmentSlot slot, MouseButtonPressed pressed) => EquipItemPressed?.Invoke(slot, pressed);

        public void InitializeInventories(Inventory equipInventory, Inventory craftingInventory, Inventory questItemsInventory)
        {
            equipInventory.Initialize(220, _equipInventory!);
            craftingInventory.Initialize(220, _craftInventory!);
            questItemsInventory.Initialize(220, _questItemsInventory!);
            equipInventory.SlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
            craftingInventory.SlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
            questItemsInventory.SlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
        }

        public EquipmentSlot GetEquipmentSlot(EquipmentPart part)
        {
            _slots.TryGetValue(part, out var slot);
            if (slot == null)
            {
                //TODO Log
                slot = new EquipmentSlot();
                _slots[part] = slot;
            }
            return slot;
        }
    }
}
