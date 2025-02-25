namespace Playground.Script.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.QuestSystem;
    using Stateless;

    public partial class ManagementLayer : CanvasLayer
    {
        private enum State { Management, Character, Inventory, Quests, Map }
        private enum Trigger { ShowCharacter, ShowInventory, ShowQuests, ShowMap, Close }
        private readonly Dictionary<string, (Trigger Trigger, State State)> _actionTriggers = [];
        private StateMachine<State, Trigger>? _machine;
        private PlayerInventoryUI? _inventoryUI;
        private QuestsMenu? _questsUI;
        private QuestManager? _questManager;
        private CharacterMenu? _characterUI;
        private MapMenu? _mapUI;

        public override void _Ready()
        {
            _machine = new(State.Management);
            _questsUI = GetNode<QuestsMenu>(nameof(QuestsMenu));
            _characterUI = GetNode<CharacterMenu>(nameof(CharacterMenu));
            _mapUI = GetNode<MapMenu>(nameof(MapMenu));
            _inventoryUI = GetNode<PlayerInventoryUI>(nameof(PlayerInventoryUI));
            _questManager = DiContainer.GetService<QuestManager>();
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
        public void ShowInventory() => _machine?.Fire(Trigger.ShowInventory);
        public void ShowQuests() => _machine?.Fire(Trigger.ShowQuests);
        public void ShowMap() => _machine?.Fire(Trigger.ShowMap);

        private bool HandleCancel(InputEvent @event)
        {
            if (!@event.IsActionPressed(Settings.Cancel)) return false;
            if (_machine!.State != State.Management)
            {
                _machine?.Fire(Trigger.Close);
                GetViewport().SetInputAsHandled();
            }
            return true;
        }

        private void AddActionTriggers()
        {
            _actionTriggers.Add(Settings.Character, (Trigger.ShowCharacter, State.Character));
            _actionTriggers.Add(Settings.Inventory, (Trigger.ShowInventory, State.Inventory));
            _actionTriggers.Add(Settings.Quests, (Trigger.ShowQuests, State.Quests));
            _actionTriggers.Add(Settings.Map, (Trigger.ShowMap, State.Map));
        }

        private void SetEvents()
        {
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.HealthComponent!.MaxHealthChanged += (value) => _inventoryUI?.UpdateMaxHealth(Mathf.RoundToInt(value));
                player.HealthComponent.CurrentHealthChanged += (value) => _inventoryUI?.UpdateCurrentHealth(Mathf.RoundToInt(value));
                player.AttackComponent!.CurrentDamageChanged += (min, max) => _inventoryUI?.UpdateDamage(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
                player.AttackComponent.CurrentCriticalChanceChanged += (value) => _inventoryUI?.UpdateCriticalChance(value);
                player.AttackComponent.CurrentCriticalDamageChanged += (value) => _inventoryUI?.UpdateCriticalDamage(value);
                player.AttackComponent.CurrentExtraHitChanged += (value) => _inventoryUI?.UpdateExtraHitChance(value);
            }
        }

        private void ConfigureMachine()
        {
            _machine?.Configure(State.Management)
                .OnEntry(HideAll)
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map);

            _machine?.Configure(State.Character)
                .OnEntry(() => { _characterUI?.Show(); })
                .OnExit(() => { _characterUI?.Hide(); })
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Management);

            _machine?.Configure(State.Inventory)
                .OnEntry(() => { _inventoryUI?.Show(); })
                .OnExit(() => { _inventoryUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Management);

            _machine?.Configure(State.Quests)
                .OnEntry(() => { _questsUI?.Show(); })
                .OnExit(() => { _questsUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.Close, State.Management);

            _machine?.Configure(State.Map)
                .OnEntry(() => { _mapUI?.Show(); })
                .OnExit(() => { _mapUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.Close, State.Management);
        }

        private void HideAll()
        {
            _characterUI?.Hide();
            _inventoryUI?.Hide();
            _questsUI?.Hide();
            _mapUI?.Hide();
        }
    }
}
