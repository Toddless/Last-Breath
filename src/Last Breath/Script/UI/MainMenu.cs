namespace Playground.Script
{
    using Godot;
    using Playground.Script.Helpers.Extensions;
    using Playground.Script.UI;
    using Stateless;

    public partial class MainMenu : Control
    {
        private const string Main = "res://Scenes/Main.tscn";
        private enum UIState { Main, Options, SaveLoad }
        private enum Trigger { ShowOptions, ShowSaveLoad, Return }

        private StateMachine<UIState, Trigger>? _stateMachine;

        private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;
        private PackedScene? _main;
        private OptionsMenu? _optionsMenu;
        private SaveLoadMenu? _saveLoadMenu;
        private MarginContainer? _marginContainer;

        public override void _Ready()
        {
            // don`t need to call GetParent here.
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

        private void SetEvents()
        {
            GetViewport().SizeChanged += OnWindowSizeChanged;
            _saveLoadMenu.ReturnPressed += ReturnButtonPressed;
            _loadGameButton.Pressed += LoadGamePressed;
            _newGameButton.Pressed += NewGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += QuitButtonPressed;
            _optionsMenu.ExitPressed += ExitPressed;
        }

        private void ConfigureStateMachine()
        {
            _stateMachine = new StateMachine<UIState, Trigger>(UIState.Main);
            _stateMachine.Configure(UIState.Main)
                .OnEntry(() =>
                {
                    _marginContainer?.Show();
                    _optionsMenu?.Hide();
                    _saveLoadMenu?.Hide();
                })
                .Permit(Trigger.ShowOptions, UIState.Options)
                .Permit(Trigger.ShowSaveLoad, UIState.SaveLoad);

            _stateMachine.Configure(UIState.Options)
                .OnEntry(() =>
                {
                    _marginContainer?.Hide();
                    _optionsMenu?.Show();
                    _optionsMenu?.SetProcess(true);
                })
                .Permit(Trigger.Return, UIState.Main)
                .OnExit(() => _optionsMenu?.SetProcess(false));

            _stateMachine.Configure(UIState.SaveLoad)
                .OnEntry(() =>
                {
                    _marginContainer?.Hide();
                    _saveLoadMenu?.Show();
                })
                .Permit(Trigger.Return, UIState.Main);
        }

        private void OnWindowSizeChanged()
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                ScreenResizeExtension.CenterWindow();
        }

        private void ReturnButtonPressed() => _stateMachine?.Fire(Trigger.Return);
        private void LoadGamePressed() => _stateMachine?.Fire(Trigger.ShowSaveLoad);
        private void ExitPressed() => _stateMachine?.Fire(Trigger.Return);
        private void OptionsButtonPressed() => _stateMachine?.Fire(Trigger.ShowOptions);
        private void QuitButtonPressed() => GetTree().Quit();
        private void NewGamePressed() => GetTree().ChangeSceneToPacked(_main);
    }
}
