namespace LastBreath.UI
{
    using Core.Constants;
    using Core.Data;
    using Core.Views.UI;
    using Godot;

    public partial class PauseMenu : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://b03h1pcqbp3iw";
        [Export] private Button? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        private IUiElementsManager? _uiElements;

        public override void _Ready()
        {
            _continueBtn?.Pressed += OnContinueBtnPressed;
            _saveLoadBtn?.Pressed += OnSaveLoadBtnPressed;
            _optionsBtn?.Pressed += OnOptionsBtnPressed;
            _mainMenuBtn?.Pressed += OnMainMenuBtnPressed;
            // Through the close-request pipeline, not Quit(): Main winds an active battle down
            // first (tracker #66/#130 — quitting mid-battle crashed with a native AV).
            _exitBtn?.Pressed += () => GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
        }


        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                UnpauseGame();
                AcceptEvent();
            }
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElements = provider.GetService<IUiElementsManager>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMainMenuBtnPressed()
        {
            UnpauseGame();
            GetTree().ChangeSceneToPacked(MainMenu.Initialize());
        }

        // Straight through the manager: the OpenWindowMessage handler is gone, published
        // messages went nowhere.
        private void OnOptionsBtnPressed() => _uiElements?.ToggleWindow(typeof(OptionsWindow));

        private void OnSaveLoadBtnPressed() => _uiElements?.ToggleWindow(typeof(SaveLoadWindow));

        private void OnContinueBtnPressed() => UnpauseGame();

        private void UnpauseGame()
        {
            var tree = Engine.GetMainLoop();
            if (tree is SceneTree scene)
                scene.Paused = false;
        }
    }
}
