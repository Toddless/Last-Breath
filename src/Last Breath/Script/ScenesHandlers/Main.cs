namespace Playground.Script.ScenesHandlers
{
    using System.ComponentModel;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.UI;
    using Stateless;

    public partial class Main : Node2D
    {
        private enum State { World, Battle, Paused }
        private enum Trigger { StartBattle, EndBattle, Pause, Resume }

        private StateMachine<State, Trigger>? _machine;

        private MainWorld? _mainWorld;
        private ManagerUI? _managerUI;

        public override void _Ready()
        {
            _machine = new StateMachine<State, Trigger>(State.World);
            _mainWorld = GetNode<MainWorld>(nameof(MainWorld));

            _managerUI = new(GetNode<MainLayer>(nameof(MainLayer)), GetNode<PauseLayer>(nameof(PauseLayer)), GetNode<BattleLayer>(nameof(BattleLayer)));
            _managerUI.SetReturn(ReturnToMainWorld);
            _managerUI.SetResume(FireResume);
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
                    _machine?.Fire(Trigger.Resume);
                }
                else if (_machine?.State == State.World)
                {
                    _machine.Fire(Trigger.Pause);
                }
            }
        }

        public static PackedScene InitializeAsPacked() => ResourceLoader.Load<PackedScene>(ScenePath.Main);

        private void FireResume() => _machine?.Fire(Trigger.Resume);

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
            _machine?.Configure(State.World)
                .Permit(Trigger.StartBattle, State.Battle)
                .Permit(Trigger.Pause, State.Paused);

            _machine?.Configure(State.Battle)
                .OnEntry(() =>
                {
                    var battleLayer = GetNode<BattleLayer>(nameof(BattleLayer));
                    battleLayer!.BattleContext = _mainWorld!.Fight!;
                    _managerUI?.ShowBattleUI();
                })
                .OnExit(() =>
                {
                    _managerUI?.ShowMainUI();
                    _mainWorld!.Fight = null;
                    _mainWorld.ResetBattleState();
                })
                .Permit(Trigger.EndBattle, State.World);

            _machine?.Configure(State.Paused)
              .OnEntry(() =>
              {
                  _mainWorld!.ProcessMode = ProcessModeEnum.Disabled;
                  _managerUI?.ShowPauseUI();
              })
              .OnExit(() =>
              {
                  _mainWorld!.ProcessMode = ProcessModeEnum.Inherit;
                  _managerUI?.ShowMainUI();
              })
              .Permit(Trigger.Resume, State.World);
        }
    }
}
