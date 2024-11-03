namespace Playground.Script
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using System.Linq;
    using Godot;
    using System.Collections.Generic;

    public partial class EnemyInventory : Node
    {
        private BasedOnRarityLootTable _lootTable = BasedOnRarityLootTable.Instance;
        private RandomNumberGenerator _rnd = new();
        private GridContainer? _inventoryContainer;
        private List<InventorySlot> _slots = [];
        private MainScene? _mainScene;
        private PackedScene? _inventorySlot;
        private Panel? _inventoryWindow;
        private Button? _takeAllButton;
        private TextureRect? _goldIcon;
        private Button? _closeButton;
        private Label? _goldAmount;
        private EnemyAI? _enemyAI;
        private Item? _item;

        public Button? TakeAllButton
        {
            get => _takeAllButton;
            set => _takeAllButton = value;
        }

        public Button? CloseButton
        {
            get => _closeButton;
            set => _closeButton = value;
        }

        public List<InventorySlot>? Slots
        {
            get => _slots;
        }


        public override async void _Ready()
        {
            _mainScene = GetNode<MainScene>("/root/MainScene");
            await ToSignal(_mainScene!, "EnemyInitialized");
            _inventoryContainer = GetNode<GridContainer>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow/InventoryContainer");
            _takeAllButton = GetNode<Button>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow/TakeAllButton");
            _goldAmount = GetNode<Label>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow/TextureRect/Label");
            _goldIcon = GetNode<TextureRect>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow/TextureRect");
            _closeButton = GetNode<Button>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow/CloseButton");
            _inventoryWindow = GetNode<Panel>("/root/MainScene/GlobalEnemyIntentory/InventoryWindow");
            _inventorySlot = ResourceLoader.Load<PackedScene>(SceneParh.InventorySlot);
            _enemyAI = _mainScene.EnemyAI;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();

            for (int i = 0; i < 25; i++)
            {
                InventorySlot inventorySlot = _inventorySlot!.Instantiate<InventorySlot>();
                _inventoryContainer.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }

            _enemyAI!.EnemyDied += OnDeathSpawnItem;
            InventoryVisible(false);
            GD.Print("Instantiate: EnemyInventory");
        }

        public void InventoryVisible(bool visible)
        {
            _inventoryWindow!.Visible = visible;
        }

        public void OnDeathSpawnItem(int rarity)
        {
            Item? item;
            do
            {
                item = _lootTable.GetRandomItem();
            } while (((int)item!.Rarity) <= rarity);

            if (item == null)
            {
                return;
            }

            this.AddItem(item);
            _goldAmount!.Text = _rnd.RandiRange(0, rarity * 1000).ToString();
            _inventoryWindow!.Visible = true;
        }

        public List<Item> GivePlayerItems()
        {
            return _slots.Where(x => x.InventoryItem != null).Select(x => x.InventoryItem!).ToList();
        }

        public void ClearInventory()
        {
            InventoryVisible(false);
            _slots.ForEach(item => item.InventoryItem = null);
        }

        public void AddItem(Item item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                GD.Print("No slot available.");
                return;
            }

            if (slot.InventoryItem == null)
            {
                slot.SetItem(item);
            }
            else if (slot.InventoryItem.Guid == item.Guid)
            {
                slot.AddItem(item);
            }
        }

        public void RemoveItem(Item? item)
        {
            var slot = GetSlotToRemove(item);

            if (slot == null)
            {
                return;
            }
            slot.RemoveItem(item);
        }

        public InventorySlot? GetSlotToAdd(Item item)
        {
            return _slots.FirstOrDefault(x => x.InventoryItem == null || (x.InventoryItem.Guid == item.Guid && x.InventoryItem.Quantity < item.MaxStackSize));
        }

        public InventorySlot? GetSlotToRemove(Item? item)
        {
            // this method work correctly without first condition only if i equip or remove an item from right to left
            // cause if an item is removed from left to right, then after first cycle method return null
            // and NullReferenceException is thrown
            return _slots.FirstOrDefault(x => x.InventoryItem != null && x.InventoryItem.Guid == item?.Guid);
        }
    }
}
