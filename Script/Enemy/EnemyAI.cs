namespace Playground
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class EnemyAI : Node2D
    {
        private BasedOnRarityLootTable _lootTable = new();
        private RandomNumberGenerator _rnd = new();
        private GlobalSignals? _globalSignals;
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

        public AttackComponent? EnemyAttack
        {
            get => _attack;
            set => _attack = value;
        }

        public HealthComponent? Health
        {
            get => _health;
            set => _health = value;
        }

        public override void _Ready()
        {
            _inventoryContainer = GetNode<GridContainer>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow/InventoryContainer");
            _inventoryWindow = GetNode<Panel>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow");
            _inventory = GetNode<InventoryComponent>("/root/MainScene/Enemy/InventoryComponent");
            _health = GetNode<HealthComponent>("/root/MainScene/Enemy/HealthComponent");
            _attack = GetNode<AttackComponent>("/root/MainScene/Enemy/AttackComponent");
            _area = GetNode<Area2D>("/root/MainScene/Enemy/Area2D");
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _health.OnCharacterDied += EnemiesDeath;
            _area.BodyEntered += PlayerEntered;
            _inventoryWindow.Visible = false;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();
            _inventory.Inititalize(10, SceneParh.InventorySlot, _inventoryContainer);
            SpawnItemsInInventory();
            _health.IncreasedMaximumHealth(1000);
            _health.RefreshHealth();
        }

        private void PlayerEntered(Node2D body)
        {
            if (body is Player)
            {
                _globalSignals!.EmitSignal(GlobalSignals.SignalName.PlayerEncounted);
                ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn").Instantiate();
            }
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
                if (item is Weapon w)
                {
                    _attack!.BaseMinDamage += w.MinDamage;
                    _attack.BaseMaxDamage += w.MaxDamage;
                    _attack.CriticalStrikeChance = w.CriticalStrikeChance;
                }
                else if (item is BodyArmor b)
                {
                    _health!.MaxHealth += b.BonusHealth;
                    _health.Defence += b.Defence;
                }
            }
        }
    }
}
