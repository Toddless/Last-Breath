namespace Playground.Script
{
    using Godot;
    using Playground.Script.Helpers.Extensions;
    using Playground.Script.UI;

    public partial class MainMenu : Control
    {
        private Button? _newGameButton, _optionsButton, _quitButton, _loadGameButton;
        private PackedScene? _mainScene;
        private OptionsMenu? _optionsMenu;
        private SaveLoadMenu? _saveLoadMenu;
        private MarginContainer? _marginContainer;
        private const string MainScenePath = "res://Scenes/MainScene.tscn";

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

            _mainScene = ResourceLoader.Load<PackedScene>(MainScenePath);

            _saveLoadMenu.ReturnPressed += ReturnButtonPressed;
            _loadGameButton.Pressed += LoadGamePressed;
            _newGameButton.Pressed += NewGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += QuitButtonPressed;
            _optionsMenu.ExitPressed += ExitPressed;
            ScreenResizeExtension.CenterWindow();
            GetViewport().SizeChanged += OnWindowSizeChanged;
        }

        private void OnWindowSizeChanged()
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                ScreenResizeExtension.CenterWindow();
        }

        private void ReturnButtonPressed()
        {
            _saveLoadMenu?.Hide();
            _marginContainer?.Show();
        }

        private void LoadGamePressed()
        {
            _marginContainer?.Hide();
            _saveLoadMenu?.Show();
        }

        private void ExitPressed()
        {
            _optionsMenu?.Hide();
            _optionsMenu?.SetProcess(false);
            _marginContainer?.Show();
        }
        private void OptionsButtonPressed()
        {
            _marginContainer?.Hide();
            _optionsMenu?.SetProcess(true);
            _optionsMenu?.Show();
        }
        private void QuitButtonPressed() => GetTree().Quit();
        private void NewGamePressed() => GetTree().ChangeSceneToPacked(_mainScene);
    }
}
