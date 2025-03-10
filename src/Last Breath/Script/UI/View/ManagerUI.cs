namespace Playground.Script.UI.View
{
    using System;
    using Playground.Script.NPC;
    using Playground.Script.UI.Layers;
    using Stateless;

    public class ManagerUI(MainLayer mainUI, PauseLayer pauseUI, BattleLayer battleUI, DevLayer? devLayer, DialogueLayer dialog)
    {
        private enum State { MainUI, PauseUI, BattleUI, DialogUI, DevTools }
        private enum Trigger { ShowPauseUI, ShowBattleUI, ShowMainUI, ShowDialogUI, ShowDevTools }

        private readonly StateMachine<State, Trigger> _machine = new(State.MainUI);
        private readonly MainLayer _mainLayer = mainUI;
        private readonly PauseLayer _pauseLayer = pauseUI;
        private readonly BattleLayer _battleLayer = battleUI;
        private readonly DevLayer? _devLayer = devLayer;
        private readonly DialogueLayer _dialogLayer = dialog;

        public void SetResume(Action resume) => _pauseLayer.Resume = resume;
        public void SetReturn(Action<BattleResult> action) => _battleLayer.ReturnToMainWorld = action;
        public void SetClose(Action close) => _dialogLayer.DialogueEnded = close;
        public void ShowBattleUI() => _machine.Fire(Trigger.ShowBattleUI);
        public void ShowMainUI() => _machine.Fire(Trigger.ShowMainUI);
        public void ShowPauseUI() => _machine?.Fire(Trigger.ShowPauseUI);

        public void ShowCutScene(string firstNode)
        {
            _dialogLayer.InitializeCutScene(firstNode);
            _machine?.Fire(Trigger.ShowDialogUI);
        }

        public void ShowDialog(BaseSpeakingNPC npc)
        {
            _dialogLayer.InitializeDialogue(npc);
            _machine.Fire(Trigger.ShowDialogUI);
        }
        public void OpenInventory(BaseOpenableObject obj) => _mainLayer.OpenInventory(obj);
#if DEBUG
        public void ShowDevTools() => _devLayer?.Show();
        public void HideDevTools() => _devLayer?.Hide();
#endif

        public void ConfigureStateMachine()
        {
            _machine?.Configure(State.MainUI)
                .OnEntry(HideAllExceptMain)
                .OnExit(() => { _mainLayer.SetProcessUnhandledInput(false); })
                .Permit(Trigger.ShowPauseUI, State.PauseUI)
                .Permit(Trigger.ShowBattleUI, State.BattleUI)
                .Permit(Trigger.ShowDialogUI, State.DialogUI);

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

            _machine?.Configure(State.DialogUI)
                .OnEntry(() =>
                {
                    _dialogLayer.SetProcessUnhandledInput(true);
                    _dialogLayer.Show();
                })
                .OnExit(() =>
                {
                    _dialogLayer.SetProcessUnhandledInput(false);
                    _dialogLayer?.Hide();
                })
                .Permit(Trigger.ShowMainUI, State.MainUI);
        }

        private void HideAllExceptMain()
        {
            _mainLayer.Show();
            _mainLayer.SetProcessUnhandledInput(true);
            _pauseLayer.Hide();
            _battleLayer.Hide();
            _devLayer?.Hide();
            _dialogLayer?.Hide();
        }
    }
}
