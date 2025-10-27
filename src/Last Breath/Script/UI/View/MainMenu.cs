namespace LastBreath.Script
{
    using Godot;
    using System;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.UI;
    using LastBreath.DIComponents;
    using LastBreath.Script.ScenesHandlers;
    using LastBreath.Script.Helpers.Extensions;

    public partial class MainMenu : Control, IInitializable
    {
        private const string UID = "uid://bd5wylwyowomd";
        [Export] private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;

        private IUIElementProvider? _uIElementProvider;

        public override void _Ready()
        {
            _uIElementProvider = GameServiceProvider.Instance.GetService<IUIElementProvider>();
            GameServiceProvider.Instance.GetService<ISettingsHandler>().ApplySavedSettings();

            _loadGameButton.Pressed += LoadGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += () => GetTree().Quit();
            _newGameButton.Pressed += () => GetTree().ChangeSceneToPacked(Main.InitializeAsPacked());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnWindowSizeChanged()
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                ScreenResizeExtension.CenterWindow();
        }

        private void LoadGamePressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var saveLoad = _uIElementProvider.CreateRequireServicesClosable<SaveLoadMenu>();
            CallDeferred(MethodName.AddChild, saveLoad);
        }

        private void OptionsButtonPressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var options = _uIElementProvider.CreateRequireServicesClosable<OptionsMenu>();
            CallDeferred(MethodName.AddChild, options);
        }
    }
}
