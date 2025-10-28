namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Enums;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Inventory;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Inventory;

    public partial class InventoryWindow : Window, IInitializable
    {
        private const string UID = "uid://boa2lmvm2an46";
        [Export] private GridContainer? _inventoryContainer;
        private Dictionary<EquipmentType, EquipmentSlot> _slots = [];
        [Export] private Array<EquipmentSlot> _ringSlots = [];

        private IInventory? _inventory;

        public override void _Ready()
        {
            _inventory?.Initialize(286,_inventoryContainer);
        }

        public override void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        public EquipmentSlot? GetEquipmentSlot(EquipmentType part)
        {
            return null;
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
            return slot;
        }
    }
}
