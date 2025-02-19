namespace Playground.Script.UI
{
    using Godot;
    using Playground.Components;
    using Playground.Script.Helpers;

    public partial class PlayerInventoryUI : Control
    {
        private GridContainer? _equipInventory, _craftInventory;
        private Inventory? _inventoryEquip, _inventoryCrafting;
        private Label? _currentHealth, _maxHealth, _damage, _criticalChance, _criticalDamage, _dodgeChance, _extraHitChance;

        public override void _Ready()
        {
            _inventoryEquip = new Inventory();
            _inventoryCrafting = new Inventory();
            _craftInventory = (GridContainer?)NodeFinder.FindBFSCached(this, "CraftContainer");
            _equipInventory = (GridContainer?)NodeFinder.FindBFSCached(this, "EquipContainer");
            _currentHealth = (Label?)NodeFinder.FindBFSCached(this, "CurrentHealth");
            _maxHealth = (Label?)NodeFinder.FindBFSCached(this, "MaxHealth");
            _damage = (Label?)NodeFinder.FindBFSCached(this, "Damage");
            _criticalChance = (Label?)NodeFinder.FindBFSCached(this, "CriticalChance");
            _criticalDamage = (Label?)NodeFinder.FindBFSCached(this, "CriticalDamage");
            _dodgeChance = (Label?)NodeFinder.FindBFSCached(this, "DodgeChance");
            _extraHitChance = (Label?)NodeFinder.FindBFSCached(this, "ExtraHitChance");
            _inventoryEquip.Initialize(220, ScenePath.InventorySlot, _equipInventory!);
            _inventoryCrafting.Initialize(220, ScenePath.InventorySlot, _craftInventory!);
            NodeFinder.ClearCache();
        }

        public void UpdateCurrentHealth(int value) => _currentHealth!.Text = $"Health: {value}";

        public void UpdateMaxHealth(int value) => _maxHealth!.Text = $"Max Health: {value}";

        public void UpdateDamage(int minDamage, int maxDamage) => _damage!.Text = $"Damage: {minDamage} - {maxDamage}";

        public void UpdateCriticalChance(float criticalChance) => _criticalChance!.Text = $"Critical Strike Chance: {criticalChance * 100}%";

        public void UpdateCriticalDamage(float criticalDamage) => _criticalDamage!.Text = $"Critical Damage: {criticalDamage * 100}%";

        public void UpdateDodgeChance(float chance) => _dodgeChance!.Text = $"Dodge Chance: {chance * 100}%";

        public void UpdateExtraHitChance(float chance) => _extraHitChance!.Text = $" Extra Hit Chance {chance * 100}%";
    }
}
