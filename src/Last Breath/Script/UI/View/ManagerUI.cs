namespace Playground.Script.UI.View
{
    using System;
    using Godot;
    using Stateless;

    public class ManagerUI(CanvasLayer mainUI, PauseLayer pauseUI, BattleLayer battleUI, DevLayer? devLayer)
    {
        private enum State { MainUI, PauseUI, BattleUI, DevTools }
        private enum Trigger { ShowPauseUI, ShowBattleUI, ShowMainUI, ShowDevTools }

        private readonly StateMachine<State, Trigger> _machine = new(State.MainUI);
        private readonly CanvasLayer _mainLayer = mainUI;
        private readonly PauseLayer _pauseLayer = pauseUI;
        private readonly BattleLayer _battleLayer = battleUI;
        private readonly DevLayer? _devLayer = devLayer;

        public void SetResume(Action resume) => _pauseLayer.Resume = resume;
        public void SetReturn(Action<BattleResult> action) => _battleLayer.ReturnToMainWorld = action;
        public void ShowBattleUI() => _machine.Fire(Trigger.ShowBattleUI);
        public void ShowMainUI() => _machine.Fire(Trigger.ShowMainUI);
        public void ShowPauseUI() => _machine?.Fire(Trigger.ShowPauseUI);
#if DEBUG
        public void ShowDevTools() => _devLayer?.Show();
        public void HideDevTools() => _devLayer?.Hide();
#endif
        public void ConfigureStateMachine()
        {
            _machine?.Configure(State.MainUI)
                .OnEntry(() =>
                {
                    _mainLayer.Show();
                    _mainLayer.SetProcessUnhandledInput(true);
                    _pauseLayer.Hide();
                    _battleLayer.Hide();
                    _devLayer?.Hide();
                })
                .OnExit(() =>
                {
                    _mainLayer.SetProcessUnhandledInput(false);
                })
                .Permit(Trigger.ShowPauseUI, State.PauseUI)
                .Permit(Trigger.ShowBattleUI, State.BattleUI);

            _machine?.Configure(State.BattleUI)
                .OnEntry(() =>
                {
                    _battleLayer.SetProcessUnhandledInput(true);
                    _battleLayer.Show();
                })
                .OnExit(() =>
                {
                    _battleLayer.SetProcessUnhandledInput(false);
                    _battleLayer.Hide();
                })
                .Permit(Trigger.ShowMainUI, State.MainUI);

            _machine?.Configure(State.PauseUI)
                .OnEntry(() =>
                {
                    _pauseLayer.SetProcessUnhandledKeyInput(true);
                    _pauseLayer.Show();
                })
                .OnExit(() =>
                {
                    _pauseLayer.SetProcessUnhandledKeyInput(false);
                    _pauseLayer?.Hide();
                })
                .Permit(Trigger.ShowMainUI, State.MainUI);
        }
    }
}
