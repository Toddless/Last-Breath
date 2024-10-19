namespace Playground
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class EnemyAI : Node2D
    {
        private string _inventorySlotPath = SceneParh.InventorySlot;
        private BasedOnRarityLootTable _lootTable = new();
        private RandomNumberGenerator _rnd = new();
        private GridContainer? _inventoryContainer;
        private InventoryComponent? _inventory;
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Panel? _inventoryWindow;
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private int _size = 25;
        private Area2D? _area;
        private int _gold;


        public override void _Ready()
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(_inventorySlotPath);
            _inventory = GetNode<InventoryComponent>("/root/MainScene/Enemy/InventoryComponent");
            _inventoryContainer = GetNode<GridContainer>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow/InventoryContainer");
            _health = GetNode<HealthComponent>("/root/MainScene/Enemy/HealthComponent");
            _attack = GetNode<AttackComponent>("/root/MainScene/Enemy/AttackComponent");
            _inventoryWindow = GetNode<Panel>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow");
            _health.OnCharacterDied += EnemiesDeath;
            _area = GetNode<Area2D>("/root/MainScene/Enemy/Area2D");
            _inventoryWindow.Visible = false;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();
            GD.Print($"Enemies health: {_health.CurrentHealth}");
        }


        public void EnemiesDeath()
        {
            _inventoryWindow!.Visible = true;
        }


        public void SpawnItemsInInventory()
        {
            _gold = _rnd.RandiRange(0, 4500);
            _inventory!.AddGoldToInventory(_gold);
            var item = _lootTable.GetRandomItem();
            if (item != null)
            {
                _inventory.AddItem(item);
            }
        }
    }
}
