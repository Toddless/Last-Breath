namespace LastBreath.Script.UI
{
    using Godot;
    using System;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.Helpers;
    using Core.Interfaces.Mediator;

    public partial class PauseMenu : Control, IInitializable, IRequireServices, IClosable
    {
        private const string UID = "uid://b03h1pcqbp3iw";
        [Export] private LocalizableButton? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        private IUIElementProvider? _uiElementProvider;
        private IUiMediator? _uiMediator;
        public event Action? Close;

        public override void _Ready()
        {
            _continueBtn.Pressed += OnContinueBtnPressed;
            _saveLoadBtn.Pressed += OnSaveLoadBtnPressed;
            _optionsBtn.Pressed += OnOptionsBtnPressed;
            _mainMenuBtn.Pressed += OnMainMenuBtnPressed;
            _exitBtn.Pressed += () => GetTree().Quit();
        }

        private void UpdateUI()
        {
            _continueBtn?.UpdateButtonText();
            _saveLoadBtn?.UpdateButtonText();
            _optionsBtn?.UpdateButtonText();
            _mainMenuBtn?.UpdateButtonText();
            _exitBtn?.UpdateButtonText();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                UnpauseGame();
                AcceptEvent();
            }
        }

        public override void _ExitTree()
        {
            UnpauseGame();
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateUI;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElementProvider = provider.GetService<IUIElementProvider>();
            _uiMediator = provider.GetService<IUiMediator>();
            _uiMediator.UpdateUi += UpdateUI;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnMainMenuBtnPressed()
        {
            UnpauseGame();
            GetTree().ChangeSceneToPacked(MainMenu.Initialize());
        }

        private void OnOptionsBtnPressed()
        {
            ArgumentNullException.ThrowIfNull(_uiElementProvider);
            var options = _uiElementProvider.CreateRequireServicesClosable<OptionsWindow>();
            CallDeferred(MethodName.AddChild, options);
        }

        private void OnSaveLoadBtnPressed()
        {
            ArgumentNullException.ThrowIfNull(_uiElementProvider);
            var saveLoad = _uiElementProvider.CreateRequireServicesClosable<SaveLoadWindow>();
            CallDeferred(MethodName.AddChild, saveLoad);
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
