namespace LastBreath.Script
{
    using System;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using DIComponents;
    using Godot;
    using ScenesHandlers;
    using UI;

    public partial class MainMenu : Control, IInitializable
    {
        private const string UID = "uid://bd5wylwyowomd";

        [Export] private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;

        private IUiElementProvider? _uIElementProvider;
        private IGameMessageBus? _gameMessageBus;

        public override void _Ready()
        {
            var provider = GameServiceProvider.Instance;
            _uIElementProvider = provider.GetService<IUiElementProvider>();
            _gameMessageBus = provider.GetService<IGameMessageBus>();
            provider.GetService<ISettingsHandler>().ApplySavedSettings();
            provider.GetService<IItemDataProvider>().LoadData();

            _loadGameButton.Pressed += LoadGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += () => GetTree().Quit();
            _newGameButton.Pressed += () => GetTree().ChangeSceneToPacked(Main.Initialize());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void LoadGamePressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var saveLoad = _uIElementProvider.CreateRequireServicesClosable<SaveLoadWindow>();
            CallDeferred(MethodName.AddChild, saveLoad);
        }

        private void OptionsButtonPressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var options = _uIElementProvider.CreateRequireServicesClosable<OptionsWindow>();
            CallDeferred(MethodName.AddChild, options);
        }
    }
}
