namespace Playground.Script.ScenesHandlers
{
    using System.ComponentModel;
    using Godot;
    using Playground.Script.UI;
    using Stateless;

    public partial class Main : Node2D
    {
        private enum State { World, Battle, Paused }
        private enum Trigger { StartBattle, EndBattle, Pause, Resume }

        private StateMachine<State, Trigger>? _machine;

        private MainWorld? _mainWorld;
        private CanvasLayer? _mainUI;
        private BattleLayer? _battleLayer;
        private BattleSceneHandler? _battleScene;

        public override void _Ready()
        {
            _machine = new StateMachine<State, Trigger>(State.World);
            _mainWorld = GetNode<MainWorld>("MainWorld");
            _mainUI = GetNode<CanvasLayer>("MainUILayer");
            _battleLayer = GetNode<BattleLayer>(nameof(BattleLayer));
            _battleScene = _battleLayer.GetNode<BattleSceneHandler>("BattleScene");
            _mainWorld.PropertyChanged += NewBattleContextCreated;
            _battleLayer.ReturnToMainWorld = ReturnToMainWorld;
            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
            _machine?.Configure(State.World)
                .Permit(Trigger.StartBattle, State.Battle)
                .Permit(Trigger.Pause, State.Paused);

            _machine?.Configure(State.Battle)
                .OnEntry(() =>
                {
                    _battleLayer!.BattleContext = _mainWorld!.Fight!;
                    _mainUI?.Hide();
                })
                .OnExit(() =>
                {
                    _mainUI?.Show();
                    _mainWorld!.Fight = null;
                    _mainWorld.ResetBattleState();
                })
                .Permit(Trigger.EndBattle, State.World);

            _machine?.Configure(State.Paused)
              .OnEntry(() =>
              {
                  _mainWorld!.GetTree().Paused = true;
              })
              .OnExit(() => _mainWorld!.GetTree().Paused = false)
              .Permit(Trigger.Resume, State.World);
        }

        private void NewBattleContextCreated(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWorld.Fight) && _mainWorld?.Fight != null)
                _machine?.Fire(Trigger.StartBattle);
        }

        private void ReturnToMainWorld(BattleResult result)
        {
            if (result.EnemyKilled)
            {
                _mainWorld?.Enemies?.Remove(result.Enemy);
            }
            else
            {
                result.Enemy.Position += new Vector2(30, 30);
                _battleScene?.CallDeferred("remove_child", result.Enemy);
                _mainWorld?.CallDeferred("add_child", result.Enemy);
            }

            _battleScene!.CallDeferred("remove_child", result.Player);
            _mainWorld!.CallDeferred("add_child", result.Player);
            _machine?.Fire(Trigger.EndBattle);
        }
    }
}
