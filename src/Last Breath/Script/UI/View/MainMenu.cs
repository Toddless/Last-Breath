namespace Playground.Script
{
    using Godot;
    using Godot.Collections;
    using Playground.Localization;
    using Playground.Script.Helpers;
    using Playground.Script.Helpers.Extensions;
    using Playground.Script.ScenesHandlers;
    using Playground.Script.UI;
    using Stateless;

    public partial class MainMenu : Control
    {
        // only for now, later i need other place for this
        private Dictionary<string, string> _dialoguesPath = new()
        {
            { "res://Resources/Dialogues/GuardianDialogues/guardianDialoguesData.tres", "G:\\Localization\\guardianDialogues.json"},
            {  "res://Resources/Dialogues/PlayerDialogues/playerDialoguesData.tres", "G:\\Localization\\playerDialogues.json"}
        };

        private enum State { Main, Options, SaveLoad }
        private enum Trigger { ShowOptions, ShowSaveLoad, Return }

        private StateMachine<State, Trigger>? _machine;

        private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;
        private OptionsMenu? _optionsMenu;
        private SaveLoadMenu? _saveLoadMenu;
        private MarginContainer? _marginContainer;

        public override void _Ready()
        {
            _machine = new StateMachine<State, Trigger>(State.Main);
            _marginContainer = GetNode<MarginContainer>(nameof(MarginContainer));
            _newGameButton = (Button?)NodeFinder.FindBFSCached(this, "NewGameBtn");
            _optionsButton = (Button?)NodeFinder.FindBFSCached(this, "OptionsBtn");
            _quitButton = (Button?)NodeFinder.FindBFSCached(this, "QuitBtn");
            _loadGameButton = (Button?)NodeFinder.FindBFSCached(this, "LoadGameBtn");

            _optionsMenu = GetNode<OptionsMenu>(nameof(OptionsMenu));
            _saveLoadMenu = GetNode<SaveLoadMenu>(nameof(SaveLoadMenu));
            NodeFinder.ClearCache();
            SetEvents();
            // ScreenResizeExtension.CenterWindow();
            ConfigureStateMachine();
            // only for now
            LoadDialoguesData();
        }

        private void LoadDialoguesData()
        {
            foreach (var dialogue in _dialoguesPath)
            {
                if (!FileAccess.FileExists(dialogue.Key))
                    DialogueDataConverter.LoadDialogueData(dialogue.Value, dialogue.Key);
            }
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
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
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            _loadGameButton.Pressed += LoadGamePressed;
            _newGameButton.Pressed += NewGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += QuitButtonPressed;
            _saveLoadMenu.ReturnPressed += ReturnPressed;
            _optionsMenu.ReturnPressed += ReturnPressed;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
        private void NewGamePressed() => GetTree().ChangeSceneToPacked(Main.InitializeAsPacked());

        public static PackedScene InitializeAsPackedScene() => ResourceLoader.Load<PackedScene>(ScenePath.MainMenu);
    }
}
