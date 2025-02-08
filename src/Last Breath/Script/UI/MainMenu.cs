namespace Playground.Script
{
    using Godot;
    public partial class MainMenu : Control
    {
        private Button? _newGameButton, _optionsButton, _quitButton;
        private PackedScene? _mainScene;
        private OptionsMenu? _optionsMenu;
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
            _optionsMenu = GetNode<OptionsMenu>("Options");
            _mainScene = ResourceLoader.Load<PackedScene>(MainScenePath);

            _newGameButton.Pressed += NewGamePressed;
            _optionsButton.Pressed += OptionsButtonPressed;
            _quitButton.Pressed += QuitButtonPressed;
            _optionsMenu.ExitPressed += ExitPressed;
        }

        private void SetSettings()
        {

        }

        private void ExitPressed()
        {
            _optionsMenu?.Hide();
            _marginContainer?.Show();
        }
        private void OptionsButtonPressed()
        {
            _marginContainer?.Hide();
            // TODO: why?
            _optionsMenu?.SetProcess(true);
            _optionsMenu?.Show();
        }
        private void QuitButtonPressed() => GetTree().Quit();
        private void NewGamePressed() => GetTree().ChangeSceneToPacked(_mainScene);
    }
}
