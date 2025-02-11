namespace Playground.Script.UI
{
    using Godot;
    using Stateless;

    public partial class PlayerUI : Control
    {
        private enum UIState { Main, Inventory, Quests, Map, Debug }
        private enum Trigger { ShowMain, ShowInventory, ShowQuests, ShowMap, ShowDebug }

        private StateMachine<UIState, Trigger>? _stateMachine;
        private Button? _characterBtn, _inventoryBtn, _questsBtn, _mapBtn, _debugBtn;
        private PlayerInventory? _inventory;
        private MarginContainer? _uiBackground;

        public override void _Ready()
        {
            _stateMachine = new StateMachine<UIState, Trigger>(UIState.Main);
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var buttons = root.GetNode<HBoxContainer>("HBoxContainerButtons");
            _inventoryBtn = buttons.GetNode<Button>("Inventory");
            _uiBackground = GetNode<MarginContainer>(nameof(MarginContainer));
            _inventory = GetNode<PlayerInventory>(nameof(PlayerInventory));
            SetEvents();
            ConfigureStateMachine();
            SetProcessUnhandledInput(true);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                if (_stateMachine?.State != UIState.Main)
                {
                    _stateMachine?.Fire(Trigger.ShowMain);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void ConfigureStateMachine()
        {
            _stateMachine?.Configure(UIState.Main)
                .OnEntry(() =>
                {
                    _uiBackground?.Show();
                    _inventory?.Hide();
                    /* _quests?.Hide();
                     * _map?.Hide();
                     */
                })
                .Permit(Trigger.ShowInventory, UIState.Inventory)
                .Permit(Trigger.ShowQuests, UIState.Quests)
                .Permit(Trigger.ShowMap, UIState.Map)
                .Permit(Trigger.ShowDebug, UIState.Debug);

            _stateMachine?.Configure(UIState.Inventory)
                .OnEntry(() =>
                {
                    _inventory?.Show();
                    _uiBackground?.Hide();
                })
                .Permit(Trigger.ShowMain, UIState.Main);
        }

        private void SetEvents()
        {
            _inventoryBtn!.Pressed += () => _stateMachine?.Fire(Trigger.ShowInventory);
        }
    }
}
