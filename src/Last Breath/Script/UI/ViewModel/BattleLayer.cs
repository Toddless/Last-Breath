namespace Playground.Script.UI
{
    using System;
    using System.ComponentModel;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Scenes;
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

        public BattleContext? BattleContext
        {
            get => _battleContext;
            set => SetProperty(ref _battleContext, value);
        }

        public Action<BattleResult>? ReturnToMainWorld;

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
                .OnEntry(() =>
                {
                    StartBattle();
                    _battleScene?.Init(BattleContext!);
                    Show();
                })
                .Permit(Trigger.Awaiting, State.Await)
                .Permit(Trigger.EndingBattle, State.BattleEnd);

            _machine.Configure(State.BattleEnd)
                .OnEntry(() =>
                {
                    EndBattle(_battleScene?.BattleResult!);
                    _battleScene?.ReturnStats();
                    CleanUp();
                    Hide();
                })
                .Permit(Trigger.Awaiting, State.Await)
                .Permit(Trigger.PreparingBattle, State.BattleStart);
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
        }

        private void SetupEvents()
        {
            _battleScene!.PlayerCurrentHealthChanged += OnPlayerCurrentHealthChange;
            _battleScene.EnemyCurrentHealthChanged += OnEnemyCurrentHealthChange;
            _battleScene.PlayerMaxHealthChanged += OnPlayerMaxHealthChange;
            _battleScene.EnemyMaxHealthChanged += OnEnemyMaxHealthChange;
            _battleUI!.ReturnButton!.Pressed += ReturnButtonPressed;
        }

        private void ReturnButtonPressed() => _battleScene?.PlayerTryingToRunAway();

        private void OnEnemyMaxHealthChange(float obj) => _battleUI?.OnEnemyMaxHealthChanged(obj);
        private void OnPlayerMaxHealthChange(float obj) => _battleUI?.OnPlayerMaxHealthChanged(obj);
        private void OnEnemyCurrentHealthChange(float obj) => _battleUI?.OnEnemyCurrentHealthChanged(obj);
        private void OnPlayerCurrentHealthChange(float obj) => _battleUI?.OnPlayerCurrentHealthChanged(obj);

        private void EndBattle(BattleResult battleResult) => ReturnToMainWorld?.Invoke(battleResult);

        private void CleanUp() => _battleContext = null;

        private void SetNewParent(Node child, Node parent)
        {
            child.GetParent().CallDeferred("remove_child", child);
            parent.CallDeferred("add_child", child);
        }
    }
}
