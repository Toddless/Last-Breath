namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Enums;
    using Godot.Collections;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Inventory;

    public partial class PlayerInventoryUI : Control
    {
        [Export] private GridContainer? _equipInventory, _craftInventory, _questItemsInventory;
        [Export] private TabBar? _equip, _craft, _quest;
        private Dictionary<EquipmentType, EquipmentSlot> _slots = [];
        [Export] private Array<EquipmentSlot> _ringSlots = [];


        public override void _Ready()
        {
            FillTheDictionary();
        }

        public void InitializeInventories(Inventory equipInventory, Inventory craftingInventory, Inventory questItemsInventory)
        {
            equipInventory.Initialize(220, _equipInventory!);
            craftingInventory.Initialize(220, _craftInventory!);
            questItemsInventory.Initialize(220, _questItemsInventory!);
          //  equipInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
           // craftingInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
           // questItemsInventory.ItemSlotClicked += (t, e, x) => InventorySlotClicked?.Invoke(t, e, x);
        }

        public EquipmentSlot? GetEquipmentSlot(EquipmentType part)
        {
            if (part == EquipmentType.Ring)
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

        private EquipmentSlot GetFreeRingSlotOrDefault() => _ringSlots[0].CurrentItem == null ? _ringSlots[0] : _ringSlots[1];

        private void FillTheDictionary()
        {
            // Adding slot via [Export] always random, slots will be added like this instead
            _slots.Add(EquipmentType.BodyArmor, FindEquipmentSlot("BodyArmorSlot"));
            _slots.Add(EquipmentType.Amulet, FindEquipmentSlot("AmuletSlot"));
            _slots.Add(EquipmentType.Boots, FindEquipmentSlot("BootsSlot"));
            _slots.Add(EquipmentType.Gloves, FindEquipmentSlot("GlovesSlot"));
            _slots.Add(EquipmentType.Cloak, FindEquipmentSlot("CloakSlot"));
            _slots.Add(EquipmentType.Helmet, FindEquipmentSlot("HelmetSlot"));
            _slots.Add(EquipmentType.Belt, FindEquipmentSlot("BeltSlot"));
            _slots.Add(EquipmentType.Weapon, FindEquipmentSlot("WeaponSlot"));
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

    }
}
