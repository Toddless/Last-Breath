namespace LastBreath.Script.UI
{
    using System;
    using System.Linq;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Inventory;
    using LastBreath.Script.QuestSystem;
    using LastBreath.Resource.Quests;
    using LastBreath.Script.Items;
    using LastBreath.Script.UI.View;
    using Stateless;
    using Contracts.Enums;

    public partial class MainLayer : CanvasLayer
    {
        private enum State { Main, Character, PlayerInventory, Quests, Map, Inventory }
        private enum Trigger { ShowCharacter, ShowPlayerInventory, ShowQuests, ShowMap, ShowInventory, Close }
        private readonly System.Collections.Generic.Dictionary<string, (Trigger Trigger, State State)> _actionTriggers = [];
        private StateMachine<State, Trigger>? _machine;
        private PlayerInventoryUI? _playerInventory;
        private CharacterMenu? _characterUI;
        private QuestsMenu? _questsUI;
        private QuestManager? _questManager;
        private InventoryUI? _inventoryUI;
        private MapMenu? _mapUI;
        private MainUI? _mainUI;
        private BaseOpenableObject? _currentOpenedObj;
        private Player? _player;

        private Action? _inventoryCloseHandler;

        public override void _Ready()
        {
            _mainUI = GetNode<MainUI>(nameof(MainUI));
            _machine = new(State.Main);
            _questsUI = GetNode<QuestsMenu>(nameof(QuestsMenu));
            _characterUI = GetNode<CharacterMenu>(nameof(CharacterMenu));
            _mapUI = GetNode<MapMenu>(nameof(MapMenu));
            _playerInventory = GetNode<PlayerInventoryUI>(nameof(PlayerInventoryUI));
            _inventoryUI = GetNode<InventoryUI>("Inventory");
            _questManager = QuestManager.Instance;
            _player = GameManager.Instance.Player;
            _playerInventory.InitializeInventories(_player.EquipInventory, _player.CraftingInventory, _player.QuestItemsInventory);
            _questManager?.Initialize();
            ConfigureMachine();
            AddActionTriggers();
            SetEvents();
            var body = new BodyArmor(GlobalRarity.Epic, AttributeType.Dexterity);
            var dagger = new Dagger(GlobalRarity.Epic);
            var dexRing = new Ring(GlobalRarity.Rare, AttributeType.Dexterity);
            var strRing = new Ring(GlobalRarity.Rare, AttributeType.Strength);
            var amulet = new Amulet(GlobalRarity.Rare);
            var dexGloves = new Gloves(GlobalRarity.Rare, AttributeType.Dexterity);
            var dexBoots = new Boots(GlobalRarity.Rare, AttributeType.Dexterity);
            var dexHelmet = new Helmet(GlobalRarity.Rare, AttributeType.Dexterity);
            var belt = new Belt(GlobalRarity.Rare);
            var cloak = new Cloak(GlobalRarity.Rare);
            var secondDexRing = new Ring(GlobalRarity.Rare, AttributeType.Dexterity);
            _player.AddItemToInventory(body);
            _player.AddItemToInventory(dagger);
            _player.AddItemToInventory(dexRing);
            _player.AddItemToInventory(strRing);
            _player.AddItemToInventory(amulet);
            _player.AddItemToInventory(dexGloves);
            _player.AddItemToInventory(dexBoots);
            _player.AddItemToInventory(dexHelmet);
            _player.AddItemToInventory(belt);
            _player.AddItemToInventory(cloak);
            _player.AddItemToInventory(secondDexRing);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (HandleCancel(@event)) return;
            foreach (var item in _actionTriggers.Where(x => @event.IsActionPressed(x.Key)))
            {
                _machine?.Fire(_machine.State == item.Value.State ? Trigger.Close : item.Value.Trigger);
                GetViewport().SetInputAsHandled();
                break;
            }
        }



        public void OpenInventory(BaseOpenableObject obj)
        {
            if (_inventoryUI == null)
            {
                // log
                return;
            }

            if (_currentOpenedObj == null)
            {
                if (_inventoryCloseHandler != null) _inventoryUI.Close -= _inventoryCloseHandler;
                _currentOpenedObj = obj;
                // Handler for pressing the "Close" button
                _inventoryCloseHandler = () => _currentOpenedObj?.Close();
                _inventoryUI.Close += _inventoryCloseHandler;
                _currentOpenedObj.CloseObject += ObjectClosing;
            }
            if (_machine?.State == State.Inventory) return;
            _machine?.Fire(Trigger.ShowInventory);
        }

        // Close object if player leaves without pressing Close or TakeAll button
        private void ObjectClosing()
        {
            _inventoryUI?.Hide();
            FireClose();
        }

        public void UpdateItems()
        {
            if (_currentOpenedObj == null) return;
            foreach (var item in _currentOpenedObj.Items)
            {
                _inventoryUI?.AddItem(item);
            }
        }

        private void FireClose() => _machine?.Fire(Trigger.Close);

        private void AddActionTriggers()
        {
            _actionTriggers.Add(Settings.Character, (Trigger.ShowCharacter, State.Character));
            _actionTriggers.Add(Settings.Inventory, (Trigger.ShowPlayerInventory, State.PlayerInventory));
            _actionTriggers.Add(Settings.Quests, (Trigger.ShowQuests, State.Quests));
            _actionTriggers.Add(Settings.Map, (Trigger.ShowMap, State.Map));
        }

        private void SetEvents()
        {
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.Health.MaxHealthChanged += (value) => _mainUI?.UpdateMaxHealthBar(Mathf.RoundToInt(value));
                player.Health!.CurrentHealthChanged += (value) => _mainUI?.UpdatePlayerHealthBar(Mathf.RoundToInt(value));
            }
            _questManager!.QuestAccepted += _questsUI!.AddQuests;
            _questManager.QuestCompleted += OnQuestCompleted;
            _mainUI!.Character += () => _machine?.Fire(Trigger.ShowCharacter);
            _mainUI!.Inventory += () => _machine?.Fire(Trigger.ShowPlayerInventory);
            _mainUI!.Quests += () => _machine?.Fire(Trigger.ShowQuests);
            _mainUI!.Map += () => _machine?.Fire(Trigger.ShowMap);
            _inventoryUI!.TakeAll += OnInventoryTakeAll;
            _playerInventory!.InventorySlotClicked += OnInventorySlotClicked;
            _playerInventory.EquipItemPressed += OnEquipItemPressed;
        }

        private void OnEquipItemPressed(EquipmentSlot slot, MouseButtonPressed pressed)
        {
            switch (pressed)
            {
                case MouseButtonPressed.RightClick:
                    HandleItemUnequip(slot);
                    break;
                default: break;
            }
        }

        private void HandleItemUnequip(EquipmentSlot slot)
        {
            if (_playerInventory == null)
            {
                // TODO : Log
                return;
            }
            // EquipItemPressed event will fire only if EquipItem not null
            _player?.AddItemToInventory(slot.CurrentItem!);
            slot.UnequipItem();
        }

        private void OnInventorySlotClicked(Item itemClicked, MouseButtonPressed pressed, Inventory inventory)
        {
            if (itemClicked is EquipItem item)
            {
                HandleEquipmentItemPressed(item, pressed, inventory);
            }
        }

        private void HandleEquipmentItemPressed(EquipItem item, MouseButtonPressed pressed, Inventory inventory)
        {
            switch (pressed)
            {
                case MouseButtonPressed.CtrRightClick:
                    HandleItemEquipment(item, inventory);
                    break;
                case MouseButtonPressed.CtrLeftClick:
                    // TODO: Action on ctr + left click
                    break;

                default: break;
                    // TODO: Other cases
            }
        }

        private void HandleItemEquipment(EquipItem item, Inventory inventory)
        {
            if (_playerInventory == null)
            {
                // TODO: Log
                return;
            }

            var equipSlot = _playerInventory.GetEquipmentSlot(item.EquipmentPart);
            if (equipSlot == null)
            {
                // TODO Log
                return;
            }

            if (equipSlot.CurrentItem != null)
            {
                HandleItemTransfer(item, equipSlot, inventory);
            }
            else
            {
                equipSlot.EquipItem(item, _player);
                inventory.RemoveItem(item.Id);
            }
        }

        private void HandleItemTransfer(EquipItem newItem, EquipmentSlot equipSlot, Inventory inventory)
        {
            var oldEquipedItem = equipSlot.CurrentItem;
            equipSlot.UnequipItem();
            equipSlot.EquipItem(newItem, _player);
            inventory.RemoveItem(newItem.Id);
            // old item != null, we check this in method above
            inventory.AddItem(oldEquipedItem!);
        }

        private void OnQuestCompleted(Quest quest)
        {
            _questsUI!.RemoveQuest(quest);
            // not sure should i remove quest item or not
            // RemoveQuestItems(quest);
        }

        private void RemoveQuestItems(Quest quest)
        {
            if (quest.QuestObjective == null || quest.QuestObjective.QuestObjectiveType != ObjectiveType.ItemCollection) return;
            _player?.QuestItemsInventory.RemoveItem(quest.QuestObjective.TargetId, quest.QuestObjective.CurrentAmount);
        }

        private void OnInventoryTakeAll()
        {
            if (_currentOpenedObj == null) return;
            var items = _currentOpenedObj.Items.ToList();
            foreach (var item in items)
            {
                AddItemToAppropriateInventory(item);
                _currentOpenedObj.Items.Remove(item);
            }
            // object has no items in it, should delete it
            _currentOpenedObj.SetDeletingTimer();
            // nothing to take, just close it
            _currentOpenedObj.Close();
        }

        private void AddItemToAppropriateInventory(Item item) => _player?.AddItemToInventory(item);

        private void ConfigureMachine()
        {
            _machine?.Configure(State.Main)
                .OnEntry(ShowMainHideAll)
                .OnExit(() => _mainUI?.Hide())
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowPlayerInventory, State.PlayerInventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowInventory, State.Inventory);

            _machine?.Configure(State.Character)
                .OnEntry(() => { _characterUI?.Show(); })
                .OnExit(() => { _characterUI?.Hide(); })
                .Permit(Trigger.ShowPlayerInventory, State.PlayerInventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Main);

            _machine?.Configure(State.PlayerInventory)
                .OnEntry(() => { _playerInventory?.Show(); })
                .OnExit(() => { _playerInventory?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Main);

            _machine?.Configure(State.Quests)
                .OnEntry(() => { _questsUI?.Show(); })
                .OnExit(() => { _questsUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowPlayerInventory, State.PlayerInventory)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Main);

            _machine?.Configure(State.Map)
                .OnEntry(() => { _mapUI?.Show(); })
                .OnExit(() => { _mapUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowPlayerInventory, State.PlayerInventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.Close, State.Main);

            // Rename state
            _machine?.Configure(State.Inventory)
                .OnEntry(() =>
                {
                    UpdateItems();
                    _inventoryUI?.Show();
                })
                .OnExit(Clear)
                .Permit(Trigger.Close, State.Main);
        }

        private void Clear()
        {
            if (_currentOpenedObj != null)
            {
                _currentOpenedObj.CloseObject -= ObjectClosing;
                _currentOpenedObj = null;
            }
            if (_inventoryCloseHandler != null) _inventoryCloseHandler = null;
            _inventoryUI?.Clear();
        }

        private void ShowMainHideAll()
        {
            _mainUI?.Show();
            _characterUI?.Hide();
            _playerInventory?.Hide();
            _questsUI?.Hide();
            _mapUI?.Hide();
        }

        private bool HandleCancel(InputEvent @event)
        {
            if (!@event.IsActionPressed(Settings.Cancel)) return false;
            if (_machine?.State == State.Main) return false;
            if (_machine?.State == State.Inventory)
            {
                // I need this special case so that I can close the inventory window with Esc.
                _currentOpenedObj?.Close();
                GetViewport().SetInputAsHandled();
                return true;
            }
            FireClose();
            GetViewport().SetInputAsHandled();
            return true;
        }
    }
}
