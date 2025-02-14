namespace Playground.Script.UI
{
    using System;
    using Godot;

    public partial class PauseMenu : Control
    {
        public event Action? Continue, SaveLoad, Options, MainMenu, Exit;
        private Button? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));
            _continueBtn = root.GetNode<Button>("ContinueBtn");
            _saveLoadBtn = root.GetNode<Button>("SaveLoadBtn");
            _optionsBtn = root.GetNode<Button>("OptionsBtn");
            _mainMenuBtn = root.GetNode<Button>("MainMenuBtn");
            _exitBtn = root.GetNode<Button>("ExitBtn");
            SetEvents();
        }

        private void SetEvents()
        {
            _continueBtn!.Pressed += () => Continue?.Invoke();
            _saveLoadBtn!.Pressed += () => SaveLoad?.Invoke();
            _optionsBtn!.Pressed += () => Options?.Invoke();
            _mainMenuBtn!.Pressed += () => MainMenu?.Invoke();
            _exitBtn!.Pressed += () => Exit?.Invoke();
        }
    }
}
