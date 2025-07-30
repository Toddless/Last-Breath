namespace LastBreath.Script.UI
{
    using System;
    using Godot;
    using LastBreath.Script.Helpers;

    public partial class PauseMenu : Control
    {
        public event Action? Continue, SaveLoad, Options, MainMenu, Exit;
        private Button? _continueBtn, _saveLoadBtn, _optionsBtn, _mainMenuBtn, _exitBtn;

        public override void _Ready()
        {
            _continueBtn = (Button?)NodeFinder.FindBFSCached(this, "ContinueBtn");
            _saveLoadBtn = (Button?)NodeFinder.FindBFSCached(this, "SaveLoadBtn");
            _optionsBtn = (Button?)NodeFinder.FindBFSCached(this, "OptionsBtn");
            _mainMenuBtn = (Button?)NodeFinder.FindBFSCached(this, "MainMenuBtn");
            _exitBtn = (Button?)NodeFinder.FindBFSCached(this, "ExitBtn");
            NodeFinder.ClearCache();
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
