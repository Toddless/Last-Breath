namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Components;

    public partial class PlayerInventoryUI : Control
    {
        private GridContainer? _equipInventory, _craftInventory, _questItemsInventory;
        private Label? _currentHealth, _maxHealth, _damage, _criticalChance, _criticalDamage, _dodgeChance, _extraHitChance;
        private TabBar? _equip, _craft, _quest;

        public override void _Ready()
        {
            _equip = (TabBar?)NodeFinder.FindBFSCached(this, "Equip");
            _quest = (TabBar?)NodeFinder.FindBFSCached(this, "QuestItems");
            _craft = (TabBar?)NodeFinder.FindBFSCached(this, "Crafting");
            _craftInventory = (GridContainer?)NodeFinder.FindBFSCached(this, "CraftContainer");
            _equipInventory = (GridContainer?)NodeFinder.FindBFSCached(this, "EquipContainer");
            _questItemsInventory = (GridContainer?)NodeFinder.FindBFSCached(this, "QuestItemsContainer");
            _currentHealth = (Label?)NodeFinder.FindBFSCached(this, "CurrentHealth");
            _maxHealth = (Label?)NodeFinder.FindBFSCached(this, "MaxHealth");
            _damage = (Label?)NodeFinder.FindBFSCached(this, "Damage");
            _criticalChance = (Label?)NodeFinder.FindBFSCached(this, "CriticalChance");
            _criticalDamage = (Label?)NodeFinder.FindBFSCached(this, "CriticalDamage");
            _dodgeChance = (Label?)NodeFinder.FindBFSCached(this, "DodgeChance");
            _extraHitChance = (Label?)NodeFinder.FindBFSCached(this, "ExtraHitChance");
           
            NodeFinder.ClearCache();
        }

        public void InitializeInventories(Inventory equipInventory, Inventory craftingInventory, Inventory questItemsInventory)
        {
            equipInventory.Initialize(220, _equipInventory!);
            craftingInventory.Initialize(220, _craftInventory!);
            questItemsInventory.Initialize(220, _questItemsInventory!);
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
