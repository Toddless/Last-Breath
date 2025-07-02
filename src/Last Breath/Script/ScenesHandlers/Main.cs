namespace Playground.Script.ScenesHandlers
{
    using System.Linq;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.NPC;
    using Playground.Script.UI;
    using Playground.Script.UI.Layers;
    using Playground.Script.UI.View;
    using Stateless;

    public partial class Main : Node2D
    {
#if DEBUG
        private bool _devOpened = false;
#endif
        private enum State { Play, Paused, Dialog, Monologue }
        private enum Trigger { Resume, Pause, Dialog, Monologue, Close }

        private StateMachine<State, Trigger>? _machine;
        private StateMachine<State, Trigger>.TriggerWithParameters<BaseSpeakingNPC>? _showDialogue;
        private StateMachine<State, Trigger>.TriggerWithParameters<string>? _showMonologue;

        private MainWorld? _mainWorld;
        private ManagerUI? _managerUI;

        public override void _Ready()
        {
            _machine = new(State.Play);
            _mainWorld = GetNode<MainWorld>(nameof(MainWorld));

            _managerUI = new(GetNode<MainLayer>(nameof(MainLayer)),
                GetNode<PauseLayer>(nameof(PauseLayer)),
                GetNode<BattleLayer>(nameof(BattleLayer)),
#if !DEBUG
                null,
#else
                GetNode<DevLayer>(nameof(DevLayer)),
#endif
                GetNode<DialogueLayer>(nameof(DialogueLayer)));
            _managerUI.SetResume(FireResume);
            _managerUI.SetClose(Close);
            _managerUI.ConfigureStateMachine();
            _managerUI.SetEvents();
            ConfigureStateMachine();
            SetEvents();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                if (_machine?.State == State.Paused)
                {
                    _machine.Fire(Trigger.Resume);
                }
                else if (_machine?.State == State.Play)
                {
                    _machine.Fire(Trigger.Pause);
                }
                GetViewport().SetInputAsHandled();
            }

            if (@event.IsActionPressed(Settings.Dialog))
            {
                if (_mainWorld!.NPCs?.Any(x => x.IsPlayerNearby()) != true) return;
                if (_machine?.State == State.Play)
                {
                    _machine.Fire(_showDialogue!, _mainWorld!.NPCs?.First(x => x.IsPlayerNearby() == true));
                    GetViewport().SetInputAsHandled();
                }
            }
#if DEBUG
            if (@event.IsActionPressed(Settings.Dev))
            {
                if (!_devOpened)
                {
                    _managerUI?.ShowDevTools();
                    _devOpened = true;
                }
                else
                {
                    _managerUI?.HideDevTools();
                    _devOpened = false;
                }
            }
#endif
        }

        public override void _ExitTree()
        {
            Unsubscribe();
            base._ExitTree();
        }

        public static PackedScene InitializeAsPacked() => ResourceLoader.Load<PackedScene>(ScenePath.Main);

        private void SetEvents()
        {
            if (_mainWorld == null) return;
            _mainWorld.CutScene += (t) => _machine?.Fire(_showMonologue, t);
            _mainWorld.InitializeFight += StartFight;
            _managerUI!.ExitedBattle += _mainWorld.ResetBattleState;

            foreach (var obj in _mainWorld.OpenableObjects)
            {
                obj.OpenObject += ObjectOpen;
            }
        }

        private void ConfigureStateMachine()
        {
            _showDialogue = _machine?.SetTriggerParameters<BaseSpeakingNPC>(Trigger.Dialog);
            _showMonologue = _machine?.SetTriggerParameters<string>(Trigger.Monologue);

            _machine?.Configure(State.Play)
                .OnEntry(() =>
                {
                    _managerUI?.ShowMainUI();
                })
                .Permit(Trigger.Pause, State.Paused)
                .Permit(Trigger.Monologue, State.Monologue)
                .Permit(Trigger.Dialog, State.Dialog);

            _machine?.Configure(State.Paused)
              .OnEntry(() =>
              {
                  _mainWorld!.ProcessMode = ProcessModeEnum.Disabled;
                  _managerUI?.ShowPauseUI();
              })
              .OnExit(() => { _mainWorld!.ProcessMode = ProcessModeEnum.Inherit; })
              .Permit(Trigger.Resume, State.Play);

            _machine?.Configure(State.Dialog)
                .OnEntryFrom(_showDialogue, OpenDialogue)
                .Permit(Trigger.Close, State.Play);

            _machine?.Configure(State.Monologue)
                .OnEntryFrom(_showMonologue, OpenMonologue)
                .Permit(Trigger.Close, State.Play);
        }

        private void FireResume() => _machine?.Fire(Trigger.Resume);
        private void Close() => _machine?.Fire(Trigger.Close);
        private void ObjectOpen(BaseOpenableObject obj) => _managerUI?.OpenInventory(obj);
        private void StartFight(BattleContext context) => _managerUI?.OpenBattleUI(context);
        private void OpenMonologue(string firstNode) => _managerUI?.OpenMonologue(firstNode);
        private void OpenDialogue(BaseSpeakingNPC npc) => _managerUI?.OpenDialogue(npc);
        private void Unsubscribe()
        {
            if (_mainWorld == null) return;
            _mainWorld.CutScene -= (t) => _machine?.Fire(_showMonologue, t);
        }
    }
}
