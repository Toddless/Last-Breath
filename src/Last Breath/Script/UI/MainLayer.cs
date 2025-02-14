namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class MainLayer : CanvasLayer
    {
        private enum State { Main, Character, Inventory, Quests, Map, Debug }
        private enum Trigger { ShowMain, ShowCharacter, ShowInventory, ShowQuests, ShowMap, ShowDebug }

        private StateMachine<State, Trigger>? _machine;

        private MainUI? _mainUI;
        private PlayerInventoryUI? _playerInventoryUI;
        private QuestsMenu? _questsUI;
        private CharacterMenu? _characterUI;
        private MapMenu? _mapUI;
        /* private DebugUI _debugUI; */

        public override void _Ready()
        {
            _machine = new(State.Main);
            _mainUI = GetNode<MainUI>(nameof(MainUI));
            _questsUI = GetNode<QuestsMenu>(nameof(QuestsMenu));
            _characterUI = GetNode<CharacterMenu>(nameof(CharacterMenu));
            _mapUI = GetNode<MapMenu>(nameof(MapMenu));
            _playerInventoryUI = GetNode<PlayerInventoryUI>(nameof(PlayerInventoryUI));
            ConfigureStateMachine();
            SetEvents();
        }

        private void SetEvents()
        {
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.HealthComponent!.CurrentHealthChanged += (value) => { _mainUI?.UpdatePlayerHealthBar(Mathf.RoundToInt(value)); _playerInventoryUI?.UpdateCurrentHealth(Mathf.RoundToInt(value)); };
                player.HealthComponent.MaxHealthChanged += (value) => { _mainUI?.UpdateMaxHealthBar(Mathf.RoundToInt(value)); _playerInventoryUI?.UpdateMaxHealth(Mathf.RoundToInt(value)); };
                player.AttackComponent!.CurrentDamageChanged += (min, max) => _playerInventoryUI?.UpdateDamage(Mathf.RoundToInt(min), Mathf.RoundToInt(max));
                player.AttackComponent.CurrentCriticalChanceChanged += (value) => _playerInventoryUI?.UpdateCriticalChance(value);
                player.AttackComponent.CurrentCriticalDamageChanged += (value) => _playerInventoryUI?.UpdateCriticalDamage(value);
                player.AttackComponent.CurrentExtraHitChanged += (value) => _playerInventoryUI?.UpdateExtraHitChance(value);
            }
            _mainUI!.Inventory += () => _machine?.Fire(Trigger.ShowInventory);
            _mainUI.Character += () => _machine?.Fire(Trigger.ShowCharacter);
            _mainUI.Quests += () => _machine?.Fire(Trigger.ShowQuests);
            _mainUI.Map += () => _machine?.Fire(Trigger.ShowMap);
            //_mainUI.Debug += () => _machine?.Fire(Trigger.ShowDebug);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                if (_machine?.State != State.Main)
                {
                    _machine?.Fire(Trigger.ShowMain);
                    GetViewport().SetInputAsHandled();
                }
            }

            if (@event.IsActionPressed(Settings.Inventory))
            {
                if (_machine?.State != State.Inventory)
                {
                    _machine?.Fire(Trigger.ShowInventory);
                }
                else
                {
                    _machine?.Fire(Trigger.ShowMain);
                }
                GetViewport().SetInputAsHandled();
            }

            if (@event.IsActionPressed(Settings.Quests))
            {
                if (_machine?.State != State.Quests)
                {
                    _machine?.Fire(Trigger.ShowQuests);
                }
                else
                {
                    _machine?.Fire(Trigger.ShowMain);
                }
                GetViewport().SetInputAsHandled();
            }

            if (@event.IsActionPressed(Settings.Character))
            {
                if (_machine?.State != State.Character)
                {
                    _machine?.Fire(Trigger.ShowCharacter);
                }
                else
                {
                    _machine?.Fire(Trigger.ShowMain);
                }
            }

            if (@event.IsActionPressed(Settings.Map))
            {
                if (_machine?.State != State.Map)
                {
                    _machine?.Fire(Trigger.ShowMap);
                }
                else
                {
                    _machine?.Fire(Trigger.ShowMain);
                }
            }
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.Main)
                .OnEntry(() =>
                {
                    _mainUI?.Show();
                    _playerInventoryUI?.Hide();
                    _questsUI?.Hide();
                    _characterUI?.Hide();
                    _mapUI?.Hide();
                    /* _debugUI?.Hide();*/
                })
                .OnExit(() => { _mainUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowDebug, State.Debug);

            _machine?.Configure(State.Character)
                .OnEntry(() => { _characterUI?.Show(); })
                .OnExit(() => { _characterUI?.Hide(); })
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowDebug, State.Debug)
                .Permit(Trigger.ShowMain, State.Main);

            _machine?.Configure(State.Inventory)
                .OnEntry(() => { _playerInventoryUI?.Show(); })
                .OnExit(() => { _playerInventoryUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowDebug, State.Debug)
                .Permit(Trigger.ShowMain, State.Main);

            _machine?.Configure(State.Quests)
                .OnEntry(() => { _questsUI?.Show(); })
                .OnExit(() => { _questsUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowDebug, State.Debug)
                .Permit(Trigger.ShowMain, State.Main);

            _machine?.Configure(State.Map)
                .OnEntry(() => { _mapUI?.Show(); })
                .OnExit(() => { _mapUI?.Hide(); })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowDebug, State.Debug)
                .Permit(Trigger.ShowMain, State.Main);

            _machine?.Configure(State.Debug)
                .OnEntry(() => { /*_debugUI?.Show();*/})
                .OnExit(() => { /*_debugUI?.Hide();*/ })
                .Permit(Trigger.ShowCharacter, State.Character)
                .Permit(Trigger.ShowInventory, State.Inventory)
                .Permit(Trigger.ShowQuests, State.Quests)
                .Permit(Trigger.ShowMap, State.Map)
                .Permit(Trigger.ShowMain, State.Main);
        }
    }
}
