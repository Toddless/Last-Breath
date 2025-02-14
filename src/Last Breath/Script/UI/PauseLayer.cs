namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Stateless;

    public partial class PauseLayer : CanvasLayer
    {
        private enum State { Options, SaveLoad, PauseMenu }
        private enum Trigger { ToOptions, ToSaveLoad, Back }

        private StateMachine<State, Trigger>? _machine;

        private PauseMenu? _menu;
        private OptionsMenu? _options;
        private SaveLoadMenu? _saveLoad;

        public Action? Resume;

        public override void _Ready()
        {
            _machine = new StateMachine<State, Trigger>(State.PauseMenu);
            _menu = GetNode<PauseMenu>(nameof(PauseMenu));
            _options = GetNode<OptionsMenu>(nameof(OptionsMenu));
            _saveLoad = GetNode<SaveLoadMenu>(nameof(SaveLoadMenu));
            SetEvents();
            ConfigureStateMachine();
        }

        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                if (_machine!.State == State.Options || _machine.State == State.SaveLoad)
                {
                    _machine.Fire(Trigger.Back);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void SetEvents()
        {
            _menu!.Continue += () => Resume?.Invoke();
            _menu.SaveLoad += () => _machine?.Fire(Trigger.ToSaveLoad);
            _menu.Options += () => _machine?.Fire(Trigger.ToOptions);
            _menu.MainMenu += () => GetTree().ChangeSceneToPacked(MainMenu.InitializeAsPackedScene());
            _menu.Exit += () => GetTree().Quit();
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.PauseMenu)
                .OnEntry(() =>
                {
                    _menu?.Show();
                    _options?.Hide();
                    _saveLoad?.Hide();
                })
                .Permit(Trigger.ToOptions, State.Options)
                .Permit(Trigger.ToSaveLoad, State.SaveLoad);

            _machine?.Configure(State.Options)
                .OnEntry(() =>
                {
                    _options?.Show();
                    _saveLoad?.Hide();
                })
                .Permit(Trigger.Back, State.PauseMenu);

            _machine?.Configure(State.SaveLoad)
                .OnEntry(() =>
                {
                    _options?.Hide();
                    _saveLoad?.Show();
                })
                .Permit(Trigger.Back, State.PauseMenu);
        }
    }
}
