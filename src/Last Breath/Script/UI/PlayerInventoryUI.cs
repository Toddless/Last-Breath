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
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var stats = root.GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<MarginContainer>("PlayerStats").GetNode<VBoxContainer>(nameof(VBoxContainer));
            var box = root.GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<VBoxContainer>("VBoxContainerInventory").GetNode<TabContainer>(nameof(TabContainer));
            _inventoryEquip = new Inventory();
            _inventoryCrafting = new Inventory();
            _craftInventory = box.GetNode<TabBar>("Crafting").GetNode<GridContainer>(nameof(GridContainer));
            _equipInventory = box.GetNode<TabBar>("Equip").GetNode<GridContainer>(nameof(GridContainer));
            _currentHealth = stats.GetNode<Label>("CurrentHealth");
            _maxHealth = stats.GetNode<Label>("MaxHealth");
            _damage = stats.GetNode<Label>("Damage");
            _criticalChance = stats.GetNode<Label>("CriticalChance");
            _criticalDamage = stats.GetNode<Label>("CriticalDamage");
            _dodgeChance = stats.GetNode<Label>("DodgeChance");
            _extraHitChance = stats.GetNode<Label>("ExtraHitChance");
            _inventoryEquip.Initialize(220, ScenePath.InventorySlot, _equipInventory!);
            _inventoryCrafting.Initialize(220, ScenePath.InventorySlot, _craftInventory!);
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
