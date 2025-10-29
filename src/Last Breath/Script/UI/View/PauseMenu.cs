namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.Helpers;

    public partial class PauseMenu : Control, IInitializable, IRequireServices, IClosable
    {
        private const string UID = "uid://b03h1pcqbp3iw";
        [Export] private Button? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        private IUIElementProvider? _uiElementProvider;
        public event Action? Close;

        public override void _Ready()
        {
            _continueBtn.Pressed += OnContinueBtnPressed;
            _saveLoadBtn.Pressed += OnSaveLoadBtnPressed;
            _optionsBtn.Pressed += OnOptionsBtnPressed;
            _mainMenuBtn.Pressed += OnMainMenuBtnPressed;
            _exitBtn.Pressed += () => GetTree().Quit();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                UnpauseGame();
                AcceptEvent();
            }
        }

        public override void _ExitTree() => UnpauseGame();

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElementProvider = provider.GetService<IUIElementProvider>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMainMenuBtnPressed() => GetTree().ChangeSceneToPacked(MainMenu.Initialize());

        private void OnOptionsBtnPressed()
        {
            ArgumentNullException.ThrowIfNull(_uiElementProvider);
            var saveLoad = _uiElementProvider.CreateRequireServicesClosable<SaveLoadWindow>();
            CallDeferred(MethodName.AddChild, saveLoad);
        }

        private void OnSaveLoadBtnPressed()
        {
            ArgumentNullException.ThrowIfNull(_uiElementProvider);
            var options = _uiElementProvider.CreateRequireServicesClosable<OptionsWindow>();
            CallDeferred(MethodName.AddChild, options);
        }

        private void OnContinueBtnPressed() => UnpauseGame();

        private void UnpauseGame()
        {
            var tree = Engine.GetMainLoop();
            if (tree != null && tree is SceneTree scene)
                scene.Paused = false;
            Close?.Invoke();
        }
    }
}
