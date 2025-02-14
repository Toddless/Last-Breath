namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Stateless;
    using System.ComponentModel;

    public partial class MainUI : Control
    {
        private enum UIState { Main, Inventory, Quests, Map, Debug }
        private enum Trigger { ShowMain, ShowInventory, ShowQuests, ShowMap, ShowDebug }

        private StateMachine<UIState, Trigger>? _stateMachine;
        private Button? _characterBtn, _inventoryBtn, _questsBtn, _mapBtn, _debugBtn;
        private PlayerInventoryUI? _inventory;
        private MarginContainer? _uiBackground;
        private TextureProgressBar? _playerHealth;
        private GridContainer? _playerEffects;

        public override void _Ready()
        {
            _stateMachine = new StateMachine<UIState, Trigger>(UIState.Main);
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var buttons = root.GetNode<HBoxContainer>("HBoxContainerButtons");
            _playerHealth = root.GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<TextureProgressBar>("Health");
            _playerEffects = root.GetNode<VBoxContainer>(nameof(VBoxContainer)).GetNode<GridContainer>(nameof(GridContainer));
            _inventoryBtn = buttons.GetNode<Button>("Inventory");
            _uiBackground = GetNode<MarginContainer>(nameof(MarginContainer));
            _inventory = GetNode<PlayerInventoryUI>(nameof(PlayerInventoryUI));

            _playerHealth.MaxValue = GameManager.Instance.Player!.HealthComponent!.MaxHealth;
            _playerHealth.Value = GameManager.Instance.Player!.HealthComponent.CurrentHealth;
            GameManager.Instance.Player.HealthComponent.PropertyChanged += OnHealthChanged;
            SetEvents();
            ConfigureStateMachine();
            SetProcessUnhandledInput(true);
        }

        private void OnHealthChanged(object? sender, PropertyChangedEventArgs e) => _playerHealth!.Value = GameManager.Instance.Player!.HealthComponent!.CurrentHealth;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(KeyBindings.Cancel))
            {
                if (_stateMachine?.State != UIState.Main)
                {
                    _stateMachine?.Fire(Trigger.ShowMain);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed(KeyBindings.Inventory))
            {
                if (_stateMachine?.State != UIState.Inventory)
                {
                    _stateMachine?.Fire(Trigger.ShowInventory);
                    GetViewport().SetInputAsHandled();
                }
                else
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
            _inventoryBtn!.Pressed += InventoryButtonPressed;
        }

        private void InventoryButtonPressed() => _stateMachine?.Fire(Trigger.ShowInventory);
    }
}
