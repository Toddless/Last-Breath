namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.ScenesHandlers;
    using Stateless;

    public partial class PauseMenu : Control
    {
        private enum State { Options, SaveLoad, Continue, NewGame, MainMenu, PauseMenu }
        private enum Trigger { OptionsOpened, SaveLoadOpened, ContinueClicked, NewGameClicked, MainMenuOpened, Cancel }

        private StateMachine<State, Trigger>? _machine;
        private const string Main = "res://Scenes/Main.tscn";
        private const string PathToMainMenu = "res://Script/UI/MainMenu.tscn";
        private PackedScene? _mainMenu;
        private PackedScene? _mainGame;
        private OptionsMenu? _optionsMenu;
        private SaveLoadMenu? _saveLoadMenu;
        private Main? _main;
        private Button? _continueBtn, _newGameBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;
        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));
            _main = (Main)GetOwner();
            _continueBtn = root.GetNode<Button>("ContinueBtn");
            _newGameBtn = root.GetNode<Button>("NewGameBtn");
            _saveLoadBtn = root.GetNode<Button>("SaveLoadBtn");
            _optionsBtn = root.GetNode<Button>("OptionsBtn");
            _mainMenuBtn = root.GetNode<Button>("MainMenuBtn");
            _exitBtn = root.GetNode<Button>("ExitBtn");
            _machine = new StateMachine<State, Trigger>(State.PauseMenu);
            _mainMenu = ResourceLoader.Load<PackedScene>(PathToMainMenu);
            _mainGame = ResourceLoader.Load<PackedScene>(Main);
            _saveLoadMenu = GetNode<SaveLoadMenu>(nameof(SaveLoadMenu));
            _optionsMenu = GetNode<OptionsMenu>("Options");
            SetEvents();
            ConfigureStateMachine();
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                if (_machine!.State == State.Options || _machine.State == State.SaveLoad)
                {
                    _machine.Fire(Trigger.Cancel);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void SetEvents()
        {
            _exitBtn.Pressed += OnExitPressed;
            _newGameBtn.Pressed += OnNewGamePressed;
            _mainMenuBtn.Pressed += OnMainMenuPressed;
            _continueBtn.Pressed += OnContinuePressed;
            _optionsBtn.Pressed += OnOptionsPressed;
            _saveLoadBtn.Pressed += OnSaveLoadPressed;

            _optionsMenu.ReturnPressed += ReturnPressed;
            _saveLoadMenu.ReturnPressed += ReturnPressed;
        }

        private void ReturnPressed() => _machine?.Fire(Trigger.Cancel);
        private void OnSaveLoadPressed() => _machine?.Fire(Trigger.SaveLoadOpened);
        private void OnOptionsPressed() => _machine?.Fire(Trigger.OptionsOpened);
        private void OnContinuePressed() => _main?.FireResume();
        private void OnMainMenuPressed()
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToPacked(_mainMenu);
            QueueFree();
        }
        private void OnExitPressed() => GetTree().Quit();

        private void OnNewGamePressed()
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToPacked(_mainGame);
            QueueFree();
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.PauseMenu)
                .OnEntry(() =>
                {
                    _optionsMenu?.Hide();
                    _saveLoadMenu?.Hide();
                    Show();
                })
                .Permit(Trigger.OptionsOpened, State.Options)
                .Permit(Trigger.SaveLoadOpened, State.SaveLoad);

            _machine?.Configure(State.Options)
                .OnEntry(() =>
                {
                    _optionsMenu?.Show();
                })
                .OnExit(() => _optionsMenu?.Hide())
                .Permit(Trigger.Cancel, State.PauseMenu);

            _machine?.Configure(State.SaveLoad)
                .OnEntry(() =>
                {
                    _saveLoadMenu?.Show();
                })
                .OnExit(() => _saveLoadMenu?.Hide())
                .Permit(Trigger.Cancel, State.PauseMenu);
        }
    }
}
