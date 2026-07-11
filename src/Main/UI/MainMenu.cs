namespace LastBreath.UI
{
    using System;
    using Battle.Source;
    using Core.Data;
    using Core.Interfaces;
    using Core.Views.UI;
    using Crafting.Source;
    using Godot;
    using Services;

    public partial class MainMenu : Control, IInitializable
    {
        private const string UID = "uid://bd5wylwyowomd";
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        [Export] private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;

        private IUiElementsManager? _uIElementProvider;

        public override void _Ready()
        {
            _uIElementProvider = _provider.GetService<IUiElementsManager>();
            _provider.GetService<ISettingsHandler>().ApplySavedSettings();
            _provider.AddCraftingWindowFactories();
            _provider.AddBattleUiElementsFactory();
            _loadGameButton?.Pressed += LoadGamePressed;
            _optionsButton?.Pressed += OptionsButtonPressed;
            _quitButton?.Pressed += () => GetTree().Quit();
            _newGameButton?.Pressed += StartNewGame;
        }

        /// <summary>The service singletons outlive scene changes — a second "New game" in one
        /// process must not inherit the previous session's facts/quests/inventory/reputation.</summary>
        private void StartNewGame()
        {
            _provider.GetService<Core.Session.ISessionResetService>().ResetSession();
            Engine.TimeScale = 1; // the death fast-forward must not leak through the menu
            GetTree().ChangeSceneToPacked(Main.Initialize());
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void LoadGamePressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var saveLoad = SaveLoadWindow.Initialize().Instantiate<SaveLoadWindow>();
            saveLoad.InjectServices(_provider);
            CallDeferred(Node.MethodName.AddChild, saveLoad);
        }

        private void OptionsButtonPressed()
        {
            ArgumentNullException.ThrowIfNull(_uIElementProvider);
            var options = OptionsWindow.Initialize().Instantiate<OptionsWindow>();
            options.InjectServices(_provider);
            CallDeferred(Node.MethodName.AddChild, options);
        }
    }
}
