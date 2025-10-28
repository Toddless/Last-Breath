namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using Utilities;
    using Stateless;
    using System.Linq;
    using Core.Interfaces.Items;
    using LastBreath.Script.Items;
    using LastBreath.Script.UI.View;
    using Core.Interfaces.Inventory;
    using LastBreath.Script.Helpers;
    using LastBreath.Resource.Quests;
    using LastBreath.Script.Inventory;
    using LastBreath.Script.QuestSystem;

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
        private PlayerHUD? _mainUI;
        private BaseOpenableObject? _currentOpenedObj;
        private Player? _player;

        private Action? _inventoryCloseHandler;

        public override void _Ready()
        {
            _mainUI = GetNode<PlayerHUD>(nameof(PlayerHUD));
            _machine = new(State.Main);
            _questsUI = GetNode<QuestsMenu>(nameof(QuestsMenu));
            _characterUI = GetNode<CharacterMenu>(nameof(CharacterMenu));
            _mapUI = GetNode<MapMenu>(nameof(MapMenu));
            _playerInventory = GetNode<PlayerInventoryUI>(nameof(PlayerInventoryUI));
            _inventoryUI = GetNode<InventoryUI>("Inventory");
            _questManager = QuestManager.Instance;
            _player = GameManager.Instance.Player;
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

        public void OpenInventory(BaseOpenableObject obj)
        {
            if (_inventoryUI == null)
            {
                // log
                return;
            }

            if (_currentOpenedObj == null)
            {
                _currentOpenedObj = obj;
                // Handler for pressing the "Close" button
                _inventoryCloseHandler = () => _currentOpenedObj?.Close();
                _currentOpenedObj.CloseObject += ObjectClosing;
            }
            if (_machine?.State == State.Inventory) return;
            _machine?.Fire(Trigger.ShowInventory);
        }

        public void UpdateItems()
        {
        }

        // Close object if player leaves without pressing Close or TakeAll button
        private void ObjectClosing()
        {
            _inventoryUI?.Hide();
            FireClose();
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
        }


        private void HandleItemUnequip(EquipmentSlot slot)
        {
            if (_playerInventory == null)
            {
                // TODO : Log
                return;
            }
            // EquipItemPressed event will fire only if EquipItem not null
          //  _player?.AddItemToInventory(slot.CurrentItem!);
            slot.UnequipItem();
        }

     
        private void HandleItemEquipment(IEquipItem item, IInventory inventory)
        {
            if (_playerInventory == null)
            {
                Tracker.TrackNull(nameof(_playerInventory),this);
                return;
            }

            var equipSlot = _playerInventory.GetEquipmentSlot(item.EquipmentPart);
            if (equipSlot == null)
            {
                Tracker.TrackError($"Inventory has no slot for {item.EquipmentPart}", this);
                return;
            }

            //if (equipSlot.CurrentItem != null)
            //{
            //    HandleItemTransfer(item, equipSlot, inventory);
            //}
            //else
            //{
            //    equipSlot.EquipItem((EquipItem)item, _player);
            //    inventory.RemoveItem(item.Id);
            //}
        }

        private void HandleItemTransfer(IEquipItem newItem, EquipmentSlot equipSlot, IInventory inventory)
        {
            var oldEquipedItem = equipSlot.CurrentItem;
            equipSlot.UnequipItem();
            equipSlot.EquipItem((EquipItem)newItem, _player);
            // old item != null, we check this in method above
         //   inventory.AddItem(oldEquipedItem!);
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
