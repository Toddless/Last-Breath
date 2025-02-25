namespace Playground.Script.UI
{
    using System;
    using System.ComponentModel;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.ScenesHandlers;
    using Stateless;

    public partial class BattleLayer : ObservableLayer
    {
        private enum State { BattleStart, BattleEnd, Await }
        private enum Trigger { PreparingBattle, EndingBattle, Awaiting }

        private StateMachine<State, Trigger>? _machine;
        private BattleSceneHandler? _battleScene;
        private BattleUI? _battleUI;
        private BattleContext? _battleContext;

        public Action<BattleResult>? ReturnToMainWorld;

        public BattleContext? BattleContext
        {
            get => _battleContext;
            set => SetProperty(ref _battleContext, value);
        }

        public override void _Ready()
        {
            _machine = new StateMachine<State, Trigger>(State.Await);
            _battleScene = GetNode<BattleSceneHandler>("BattleScene");
            _battleUI = GetNode<BattleUI>(nameof(BattleUI));
            this.PropertyChanged += GettingNewContext;
            _battleScene.PropertyChanged += BattleFinished;
            SetupEvents();
            ConfigureStateMachine();
        }

        private void ConfigureStateMachine()
        {
            _machine!.Configure(State.Await)
                .OnEntry(Hide)
                .Permit(Trigger.EndingBattle, State.BattleEnd)
                .Permit(Trigger.PreparingBattle, State.BattleStart);

            _machine.Configure(State.BattleStart)
                .OnEntry(StartBattle)
                .Permit(Trigger.Awaiting, State.Await)
                .Permit(Trigger.EndingBattle, State.BattleEnd);

            _machine.Configure(State.BattleEnd)
                .OnEntry(EndBattle)
                .Permit(Trigger.Awaiting, State.Await)
                .Permit(Trigger.PreparingBattle, State.BattleStart);
        }

        private void EndBattle()
        {
            ReturnToMainWorld?.Invoke(_battleScene?.BattleResult!);
            _battleScene?.ReturnStats();
            _battleContext = null;
            Hide();
        }

        private void GettingNewContext(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BattleContext) && BattleContext != null)
                _machine?.Fire(Trigger.PreparingBattle);
        }

        private void BattleFinished(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BattleSceneHandler.BattleResult) && _battleScene?.BattleResult != null)
                _machine?.Fire(Trigger.EndingBattle);
        }

        private void StartBattle()
        {
            var player = (Player)BattleContext!.Self;
            var enemy = (BaseEnemy)BattleContext.Opponent;
            SetNewParent(player, _battleScene!);
            SetNewParent(enemy, _battleScene!);
            _battleUI?.InitialSetup(player, enemy);
            _battleScene?.Init(BattleContext!);
            Show();
        }

        private void SetupEvents()
        {
            _battleScene!.PlayerCurrentHealthChanged += (t) => _battleUI?.OnPlayerCurrentHealthChanged(t);
            _battleScene.EnemyCurrentHealthChanged += (t) => _battleUI?.OnEnemyCurrentHealthChanged(t);
            _battleScene.PlayerMaxHealthChanged += (t) => _battleUI?.OnPlayerMaxHealthChanged(t);
            _battleScene.EnemyMaxHealthChanged += (t) => _battleUI?.OnEnemyMaxHealthChanged(t);
            _battleUI!.Return = _battleScene.PlayerTryingToRunAway;
        }

        private void SetNewParent(Node child, Node parent)
        {
            child.GetParent().CallDeferred("remove_child", child);
            parent.CallDeferred("add_child", child);
        }
    }
}
