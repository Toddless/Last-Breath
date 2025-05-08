namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class PlayerInventoryUI : Control
    {
        [Export] private GridContainer? _equipInventory, _craftInventory, _questItemsInventory;
        private Label? _currentHealth, _maxHealth, _damage, _criticalChance, _criticalDamage, _dodgeChance, _extraHitChance;
        [Export] private TabBar? _equip, _craft, _quest;
        [Export] private Dictionary<EquipmentPart, EquipmentSlot> _slots = [];

        public event Action<Item, MouseButtonPressed, Inventory>? InventorySlotClicked;
        public event Action<EquipmentSlot, MouseButtonPressed>? EquipItemPressed;

        public override void _Ready()
        {
            _currentHealth = (Label?)NodeFinder.FindBFSCached(this, "CurrentHealth");
            _maxHealth = (Label?)NodeFinder.FindBFSCached(this, "MaxHealth");
            _damage = (Label?)NodeFinder.FindBFSCached(this, "Damage");
            _criticalChance = (Label?)NodeFinder.FindBFSCached(this, "CriticalChance");
            _criticalDamage = (Label?)NodeFinder.FindBFSCached(this, "CriticalDamage");
            _dodgeChance = (Label?)NodeFinder.FindBFSCached(this, "DodgeChance");
            _extraHitChance = (Label?)NodeFinder.FindBFSCached(this, "ExtraHitChance");

            NodeFinder.ClearCache();
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

        #region Stats

        public void UpdateCurrentHealth(int value) => _currentHealth!.Text = $"Health: {value}";

        public void UpdateMaxHealth(int value) => _maxHealth!.Text = $"Max Health: {value}";

        public void UpdateDamage(int minDamage, int maxDamage) => _damage!.Text = $"Damage: {minDamage} - {maxDamage}";

        public void UpdateCriticalChance(float criticalChance) => _criticalChance!.Text = $"Critical Strike Chance: {criticalChance * 100}%";

        public void UpdateCriticalDamage(float criticalDamage) => _criticalDamage!.Text = $"Critical Damage: {criticalDamage * 100}%";

        public void UpdateDodgeChance(float chance) => _dodgeChance!.Text = $"Dodge Chance: {chance * 100}%";

        public void UpdateExtraHitChance(float chance) => _extraHitChance!.Text = $" Extra Hit Chance {chance * 100}%";

        #endregion
    }
}
