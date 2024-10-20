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
        private HealthComponent? _health;
        private AttackComponent? _attack;
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Panel? _inventoryWindow;
        private Button? _closeButton;
        private Button? _takeAllButton;
        private TextureRect? _goldIcon;
        private Label? _goldLabel;
        private int _size = 25;
        private Area2D? _area;
        private int _gold;

        [Signal]
        public delegate void EnemyInventoryClosedEventHandler();
        [Signal]
        public delegate void PlayerTakedAllItemsEventHandler();

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

        public InventoryComponent? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        public override void _Ready()
        {
            _inventory = GetNode<InventoryComponent>("/root/MainScene/Enemy/InventoryWindow/InventoryContainer/InventoryComponent");
            _inventoryContainer = GetNode<GridContainer>("/root/MainScene/Enemy/InventoryWindow/InventoryContainer");
            _takeAllButton = GetNode<Button>("/root/MainScene/Enemy/InventoryWindow/TakeAllButton");
            _goldLabel = GetNode<Label>("/root/MainScene/Enemy/InventoryWindow/TextureRect/Label");
            _goldIcon = GetNode<TextureRect>("/root/MainScene/Enemy/InventoryWindow/TextureRect");
            _closeButton = GetNode<Button>("/root/MainScene/Enemy/InventoryWindow/CloseButton");
            _attack = GetNode<AttackComponent>("/root/MainScene/Enemy/AttackComponent");
            _health = GetNode<HealthComponent>("/root/MainScene/Enemy/HealthComponent");
            _inventoryWindow = GetNode<Panel>("/root/MainScene/Enemy/InventoryWindow");
            _area = GetNode<Area2D>("/root/MainScene/Enemy/Area2D");
            _closeButton.Pressed += InventoryClosed;
            _takeAllButton.Pressed += TakeAllButton;
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _health.OnCharacterDied += EnemiesDeath;
            _area.BodyEntered += PlayerEntered;
            _inventoryWindow.Visible = false;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();
            _inventory.Inititalize(_size, SceneParh.InventorySlot, _inventoryContainer);
            SpawnItemsInInventory();
            //_health.IncreasedMaximumHealth(1000);
            _health.RefreshHealth();
            _goldLabel.Text = _gold.ToString();
        }

        private void TakeAllButton()
        {
            EmitSignal(SignalName.PlayerTakedAllItems);
        }
        private void InventoryClosed()
        {
            _inventoryWindow!.Visible = false;
            EmitSignal(SignalName.EnemyInventoryClosed);
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
