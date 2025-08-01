namespace LastBreath.Script.UI.View
{
    using System;
    using LastBreath.Script.ScenesHandlers;
    using LastBreath.Script.NPC;
    using LastBreath.Script.UI.Layers;
    using Stateless;

    public class ManagerUI(MainLayer mainUI, PauseLayer pauseUI, BattleLayer battleUI, DialogueLayer dialog)
    {
        private enum State { MainUI, PauseUI, BattleUI, DialogUI, DevTools }
        private enum Trigger { ShowPauseUI, ShowBattleUI, ShowMainUI, ShowDialogUI, ShowDevTools }

        private readonly StateMachine<State, Trigger> _machine = new(State.MainUI);
        private readonly MainLayer _mainLayer = mainUI;
        private readonly PauseLayer _pauseLayer = pauseUI;
        private readonly BattleLayer _battleLayer = battleUI;
        private readonly DialogueLayer _dialogLayer = dialog;

        public event Action? ExitedBattle, ExitedPause, ExitedDialogue;

        public void ShowMainUI() => _machine.Fire(Trigger.ShowMainUI);
        public void ShowPauseUI() => _machine?.Fire(Trigger.ShowPauseUI);
        public void OpenInventory(BaseOpenableObject obj) => _mainLayer.OpenInventory(obj);

        public void OpenBattleUI(BattleContext context)
        {
            _battleLayer.Init(context);
            _machine.Fire(Trigger.ShowBattleUI);
        }

        public void OpenMonologue(string firstNode)
        {
            _dialogLayer.InitializeMonologue(firstNode);
            _machine?.Fire(Trigger.ShowDialogUI);
        }

        public void OpenDialogue(BaseSpeakingNPC npc)
        {
            _dialogLayer.InitializeDialogue(npc);
            _machine.Fire(Trigger.ShowDialogUI);
        }

        public void SetEvents()
        {
            _battleLayer.BattleEnds += () => _machine.Fire(Trigger.ShowMainUI);
            _dialogLayer.CloseDialogueWindow += () => _machine.Fire(Trigger.ShowMainUI);
        }

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
                    ExitedBattle?.Invoke();
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
                    ExitedPause?.Invoke();
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
                    ExitedDialogue?.Invoke();
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
            _dialogLayer?.Hide();
        }
    }
}
