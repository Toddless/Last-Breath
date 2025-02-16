namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class MainLayer : CanvasLayer
    {
        private enum State { Main, Management }
        private enum Trigger { OpenManagement, CloseManagement }

        private StateMachine<State, Trigger>? _machine;

        private ManagementLayer? _management;
        private MainUI? _mainUI;

        public override void _Ready()
        {
            _machine = new(State.Main);
            _mainUI = GetNode<MainUI>(nameof(MainUI));
            _management = GetNode<ManagementLayer>(nameof(ManagementLayer));
            ConfigureStateMachine();
            SetEvents();
        }

        private void SetEvents()
        {
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.HealthComponent!.CurrentHealthChanged += (value) => _mainUI?.UpdatePlayerHealthBar(Mathf.RoundToInt(value)); ;
                player.HealthComponent.MaxHealthChanged += (value) => _mainUI?.UpdateMaxHealthBar(Mathf.RoundToInt(value)); ;
            }

            _mainUI!.Character += () => _management?.ShowCharacter();
            _mainUI!.Inventory += () => _management?.ShowInventory();
            _mainUI!.Quests += () => _management?.ShowQuests();
            _mainUI!.Map += () => _management?.ShowMap();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                if (_machine?.State == State.Management)
                {
                    _machine?.Fire(Trigger.CloseManagement);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.Main)
                .Permit(Trigger.OpenManagement, State.Management);

            _machine?.Configure(State.Management)
                .Permit(Trigger.CloseManagement, State.Main);
        }
    }
}
