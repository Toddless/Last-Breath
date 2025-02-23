namespace Playground.Script.ScenesHandlers
{
    using Godot;
    using Stateless;
    using System.Linq;
    using Playground.Script.UI;
    using System.ComponentModel;
    using Playground.Script.NPC;
    using Playground.Script.UI.View;
    using Playground.Script.Helpers;
    using Playground.Script.UI.Layers;

    public partial class Main : Node2D
    {
#if DEBUG
        private bool _devOpened = false;
#endif
        private enum State { World, Battle, Paused, Dialog }
        private enum Trigger { StartBattle, EndBattle, Pause, Resume, Dialog, Close }

        private StateMachine<State, Trigger>? _machine;
        private StateMachine<State, Trigger>.TriggerWithParameters<BaseNPC>? _showDialog;

        private MainWorld? _mainWorld;
        private ManagerUI? _managerUI;

        public override void _Ready()
        {
            _machine = new(State.World);
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
            _managerUI.SetReturn(ReturnToMainWorld);
            _managerUI.SetResume(FireResume);
            _managerUI.SetClose(Close);
            _managerUI.ConfigureStateMachine();

            _mainWorld.PropertyChanged += NewBattleContextCreated;
            ConfigureStateMachine();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel))
            {
                if (_machine?.State == State.Paused)
                {
                    _machine.Fire(Trigger.Resume);
                }
                else if (_machine?.State == State.World)
                {
                    _machine.Fire(Trigger.Pause);
                }
                GetViewport().SetInputAsHandled();
            }

            if (@event.IsActionPressed(Settings.Dialog))
            {
                if (_mainWorld!.NPCs?.Any(x => x.IsPlayerNearby()) != true) return;
                if (_machine?.State == State.World)
                {
                    _machine.Fire(_showDialog!, _mainWorld!.NPCs?.First(x => x.IsPlayerNearby() == true));
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

        public static PackedScene InitializeAsPacked() => ResourceLoader.Load<PackedScene>(ScenePath.Main);

        private void FireResume() => _machine?.Fire(Trigger.Resume);

        private void Close() => _machine?.Fire(Trigger.Close);

        private void NewBattleContextCreated(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWorld.Fight) && _mainWorld?.Fight != null)
                _machine?.Fire(Trigger.StartBattle);
        }

        private void ReturnToMainWorld(BattleResult result)
        {
            var battleScene = GetNode<BattleLayer>(nameof(BattleLayer)).GetNode<BattleSceneHandler>("BattleScene");
            if (result.EnemyKilled)
            {
                _mainWorld?.Enemies?.Remove(result.Enemy);
            }
            else
            {
                result.Enemy.Position += new Vector2(30, 30);
                battleScene?.CallDeferred("remove_child", result.Enemy);
                _mainWorld?.CallDeferred("add_child", result.Enemy);
            }

            battleScene!.CallDeferred("remove_child", result.Player);
            _mainWorld!.CallDeferred("add_child", result.Player);
            _machine?.Fire(Trigger.EndBattle);
        }

        private void ConfigureStateMachine()
        {
            _showDialog = _machine?.SetTriggerParameters<BaseNPC>(Trigger.Dialog);

            _machine?.Configure(State.Dialog)
                .OnEntryFrom(_showDialog, OpenDialog)
                .Permit(Trigger.Close, State.World);

            _machine?.Configure(State.World)
                .OnEntry(() => _managerUI?.ShowMainUI())
                .Permit(Trigger.StartBattle, State.Battle)
                .Permit(Trigger.Pause, State.Paused)
                .Permit(Trigger.Dialog, State.Dialog);

            _machine?.Configure(State.Battle)
                .OnEntry(() =>
                {
                    PrepareBattle();
                    _managerUI?.ShowBattleUI();
                })
                .OnExit(AfterBattle)
                .Permit(Trigger.EndBattle, State.World);

            _machine?.Configure(State.Paused)
              .OnEntry(() =>
              {
                  _mainWorld!.ProcessMode = ProcessModeEnum.Disabled;
                  _managerUI?.ShowPauseUI();
              })
              .OnExit(() => { _mainWorld!.ProcessMode = ProcessModeEnum.Inherit; })
              .Permit(Trigger.Resume, State.World);
        }

        private void OpenDialog(BaseNPC npc)
        {
            _managerUI?.ShowDialog(npc);
        }

        private void AfterBattle()
        {
            _mainWorld!.Fight = null;
            _mainWorld.ResetBattleState();
        }

        private void PrepareBattle()
        {
            var battleLayer = GetNode<BattleLayer>(nameof(BattleLayer));
            battleLayer!.BattleContext = _mainWorld!.Fight!;
        }
    }
}
