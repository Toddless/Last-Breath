namespace LastBreath.UI.View
{
    using Core.Constants;
    using Core.Data;
    using Core.Events;
    using Core.MessageBus;
    using Core.Views.UI;
    using Godot;

    public partial class PauseMenu : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://b03h1pcqbp3iw";
        [Export] private Button? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        private IGameMessageBus? _messageBus;

        public override void _Ready()
        {
            _continueBtn?.Pressed += OnContinueBtnPressed;
            _saveLoadBtn?.Pressed += OnSaveLoadBtnPressed;
            _optionsBtn?.Pressed += OnOptionsBtnPressed;
            _mainMenuBtn?.Pressed += OnMainMenuBtnPressed;
            _exitBtn?.Pressed += () => GetTree().Quit();
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
            _messageBus = provider.GetService<IGameMessageBus>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMainMenuBtnPressed()
        {
            UnpauseGame();
            GetTree().ChangeSceneToPacked(MainMenu.Initialize());
        }

        private void OnOptionsBtnPressed() => _messageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(OptionsWindow)));

        private void OnSaveLoadBtnPressed()=> _messageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(SaveLoadWindow)));

        private void OnContinueBtnPressed() => UnpauseGame();

        private void UnpauseGame()
        {
            var tree = Engine.GetMainLoop();
            if (tree is SceneTree scene)
                scene.Paused = false;
        }
    }
}
