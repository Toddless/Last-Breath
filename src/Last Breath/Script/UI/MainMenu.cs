namespace Playground.Script
{
    using Godot;
    using Playground.Script.Helpers.Extensions;
    using Playground.Script.UI;
    using Stateless;

    public partial class MainMenu : Control
    {
        private const string Main = "res://Scenes/Main.tscn";
        private enum State { Main, Options, SaveLoad }
        private enum Trigger { ShowOptions, ShowSaveLoad, Return }

        private StateMachine<State, Trigger>? _machine;

        private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;
        private PackedScene? _main;
        private OptionsMenu? _optionsMenu;
        private SaveLoadMenu? _saveLoadMenu;
        private MarginContainer? _marginContainer;

        public override void _Ready()
        {
            // don`t need to call GetParent here.
            _machine = new StateMachine<State, Trigger>(State.Main);
            _marginContainer = GetNode<MarginContainer>(nameof(MarginContainer));
            var root = _marginContainer.GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));
            _newGameButton = root.GetNode<Button>("NewGameBtn");
            _optionsButton = root.GetNode<Button>("OptionsBtn");
            _quitButton = root.GetNode<Button>("QuitBtn");
            _loadGameButton = root.GetNode<Button>("LoadGameBtn");

            _optionsMenu = GetNode<OptionsMenu>("Options");
            _saveLoadMenu = GetNode<SaveLoadMenu>("SaveLoadMenu");
            _main = ResourceLoader.Load<PackedScene>(Main);

            SetEvents();
            ScreenResizeExtension.CenterWindow();
            ConfigureStateMachine();
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                if (_machine!.State == State.Options || _machine.State == State.SaveLoad)
                {
                    _machine.Fire(Trigger.Return);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void SetEvents()
        {
            GetViewport().SizeChanged += OnWindowSizeChanged;
            _loadGameButton.Pressed += LoadGamePressed;
            _newGameButton.Pressed += NewGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += QuitButtonPressed;

            _saveLoadMenu.ReturnPressed += ReturnPressed;
            _optionsMenu.ReturnPressed += ReturnPressed;
        }

        private void ConfigureStateMachine()
        {
            _machine!.Configure(State.Main)
                .OnEntry(() =>
                {
                    _marginContainer?.Show();
                    _optionsMenu?.Hide();
                    _saveLoadMenu?.Hide();
                })
                .Permit(Trigger.ShowOptions, State.Options)
                .Permit(Trigger.ShowSaveLoad, State.SaveLoad);

            _machine.Configure(State.Options)
                .OnEntry(() =>
                {
                    _marginContainer?.Hide();
                    _optionsMenu?.Show();
                    _optionsMenu?.SetProcess(true);
                })
                .Permit(Trigger.Return, State.Main)
                .OnExit(() => _optionsMenu?.SetProcess(false));

            _machine.Configure(State.SaveLoad)
                .OnEntry(() =>
                {
                    _marginContainer?.Hide();
                    _saveLoadMenu?.Show();
                })
                .Permit(Trigger.Return, State.Main);
        }

        private void OnWindowSizeChanged()
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                ScreenResizeExtension.CenterWindow();
        }
        private void LoadGamePressed() => _machine?.Fire(Trigger.ShowSaveLoad);
        private void ReturnPressed() => _machine?.Fire(Trigger.Return);
        private void OptionsButtonPressed() => _machine?.Fire(Trigger.ShowOptions);
        private void QuitButtonPressed() => GetTree().Quit();
        private void NewGamePressed() => GetTree().ChangeSceneToPacked(_main);
    }
}
