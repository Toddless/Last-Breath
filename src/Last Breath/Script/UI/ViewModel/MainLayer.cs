namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class MainLayer : CanvasLayer
    {
        private enum State { Main, Management }
        private enum Trigger { Open, Close }

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
            if (HandleCancel(@event)) return;
        }

        private bool HandleCancel(InputEvent @event)
        {
            if (!@event.IsActionPressed(Settings.Cancel)) return false;
            if (_machine!.State == State.Management)
            {
                _machine?.Fire(Trigger.Close);
                GetViewport().SetInputAsHandled();
            }
            return true;
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.Main)
                .Permit(Trigger.Open, State.Management);

            _machine?.Configure(State.Management)
                .Permit(Trigger.Close, State.Main);
        }
    }
}
