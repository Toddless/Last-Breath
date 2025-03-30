namespace Playground.Script.UI
{
    using System;
    using System.Linq;
    using Godot;
    using Playground.Resource.Quests;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using Playground.Script.QuestSystem;
    using Playground.Script.UI.View;
    using Stateless;

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
        private Action? _inventoryCloseHandler;
        private Player? _player;

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

        public void ShowCharacter() => _machine?.Fire(Trigger.ShowCharacter);
        public void ShowPlayerInventory() => _machine?.Fire(Trigger.ShowPlayerInventory);
        public void ShowQuests() => _machine?.Fire(Trigger.ShowQuests);
        public void ShowMap() => _machine?.Fire(Trigger.ShowMap);

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
                player.HealthComponent!.MaxHealthChanged += (value) => _playerInventory?.UpdateMaxHealth(Mathf.RoundToInt(value));
                player.HealthComponent.CurrentHealthChanged += (value) => _playerInventory?.UpdateCurrentHealth(Mathf.RoundToInt(value));
                player.AttackComponent!.CurrentDamageChanged += (min, max) => _playerInventory?.UpdateDamage(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
                player.AttackComponent.CurrentCriticalChanceChanged += (value) => _playerInventory?.UpdateCriticalChance(value);
                player.AttackComponent.CurrentCriticalDamageChanged += (value) => _playerInventory?.UpdateCriticalDamage(value);
                player.AttackComponent.CurrentExtraHitChanged += (value) => _playerInventory?.UpdateExtraHitChance(value);
                player.HealthComponent.MaxHealthChanged += (value) => _mainUI?.UpdateMaxHealthBar(Mathf.RoundToInt(value));
                player.HealthComponent!.CurrentHealthChanged += (value) => _mainUI?.UpdatePlayerHealthBar(Mathf.RoundToInt(value));
            }
            _questManager!.QuestAccepted += _questsUI!.AddQuests;
            _questManager.QuestCompleted += OnQuestCompleted;
            _mainUI!.Character += ShowCharacter;
            _mainUI!.Inventory += ShowPlayerInventory;
            _mainUI!.Quests += ShowQuests;
            _mainUI!.Map += ShowMap;
            _inventoryUI!.TakeAll += OnInventoryTakeAll;
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
            _player?.QuestItemsInventory.RemoveItem(quest.QuestObjective.TargetId);
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
                .OnEntry(HideAll)
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

        private void HideAll()
        {
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
