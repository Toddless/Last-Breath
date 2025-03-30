namespace Playground.Script.ScenesHandlers
{
    using System;
    using Godot;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Helpers;

    public partial class BattleSceneHandler : ObservableNode2D
    {
        private enum Results { EnemyWon, PlayerWon, PlayerRunAway }

        public event Action<float>? PlayerCurrentHealthChanged;
        public event Action<float>? EnemyCurrentHealthChanged;

        public event Action<float>? PlayerMaxHealthChanged;
        public event Action<float>? EnemyMaxHealthChanged;

        public event Action<IEffect>? PlayerEffectAdded;
        public event Action<IEffect>? EnemyEffectAdded;

        public event Action<IEffect>? PlayerEffectRemoved;
        public event Action<IEffect>? EnemyEffectRemoved;

        private float _chanceToEscape = 0.5f;
        private RandomNumberGenerator _rnd = new();
        private Player? _player;
        private BaseEnemy? _enemy;
        private BattleResult? _battleResult;

        public BattleResult? BattleResult
        {
            get => _battleResult;
            set => SetProperty(ref _battleResult, value);
        }

        public override void _Ready()
        {

        }

        public void Init(BattleContext context)
        {
            _player = (Player)context.Self;
            _enemy = (BaseEnemy)context.Opponent;
            _player.CanMove = false;
            _enemy.EnemyFight = true;
        }

        public void ReturnStats()
        {
            _player!.CanMove = true;
        }

        public void ClearBattleScene()
        {
            _player = null;
            _enemy = null;
        }

        public void PlayerTryingToRunAway()
        {
            if (!(_rnd.RandfRange(0, 1) <= _chanceToEscape)) return;
            // need method to refresh enemy stats
            BattleResult = BattleFinished(Results.PlayerRunAway);
            _player?.OnEnemyKilled(_enemy);
        }

        private BattleResult? BattleFinished(Results results)
        {
            return (results) switch
            {
                Results.EnemyWon => BattleResult = new(_player!, _enemy!, false),
                Results.PlayerWon => BattleResult = new(_player!, _enemy!, true),
                Results.PlayerRunAway => BattleResult = new(_player!, _enemy!, false),
                _ => null
            };
        }
    }
}
