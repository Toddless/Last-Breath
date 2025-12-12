namespace Battle
{
    using Godot;
    using Source;
    using TestData;
    using Core.Interfaces;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices
    {
        private const string UID = "uid://dph8vnuwipwoc";
        private readonly RandomNumberGenerator _rnd = new();
        private readonly CombatScheduler _combatScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private readonly Vector2 _playerPosition = new(320, 560);
        private IGameEventBus? _gameEventBus;
        private IGameEventBus? _battleEventBus;
        private List<IEntity> _entities = [];
        private int _entitiesCount;
        [Export] private Array<EnemySpot> _spots = [];
        private Player? _player;
        private IEntity? _currentFighter;
        private bool _fightEnds;

        private TaskCompletionSource<IEntity>? _playerTargetTcs;

        public override void _Ready()
        {
            _rnd.Randomize();
            _queueScheduler.QueueEmpty += OnQueueEmpty;
            _fightEnds = false;
            foreach (EnemySpot enemySpot in _spots)
                enemySpot.EntityClicked += OnEntityClicked;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
        }

        public void SetupEventBus(IGameEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
        }

        public void SetPlayer(IEntity player)
        {
            if (player is not Player p) return;
            var asNode = player as CharacterBody2D;
            CallDeferred(Node.MethodName.AddChild, asNode);
            _player = p;
            _player.Dead += OnPlayerDead;
        }

        public async Task StartFight(List<IEntity> fighters)
        {
            int amount = fighters.Count;

            for (int i = 0; i < amount; i++)
            {
                var enemy = fighters[i];
                _entities.Add(enemy);
                _spots[i].SetEntity(enemy);
                enemy.Dead += OnFighterDead;
            }

            _entitiesCount = amount;

            if (_player != null)
                fighters.Add(_player);
            _entities = fighters;

            _queueScheduler.AddFighters(fighters);
            await ProcessTurnsAsync();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async Task ProcessTurnsAsync()
        {
            while (!_fightEnds)
            {
                if (_entitiesCount == 0) break;
                _currentFighter = _queueScheduler.GetCurrentFighter();
                if (_currentFighter is not { IsAlive: true }) continue;

                _gameEventBus?.Publish<TurnStartGameEvent>(new());
                _currentFighter.OnTurnStart();
                if (_currentFighter is Player)
                {
                    GD.Print("Player turn");
                    _playerTargetTcs = new TaskCompletionSource<IEntity>();

                    var target = await _playerTargetTcs.Task;

                    if (target is not { IsAlive: true }) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _combatScheduler.Schedule(context);
                }
                else
                {
                    GD.Print("Enemy turn");
                    var target = GetEntityTarget();
                    if (target == null) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _combatScheduler.Schedule(context);
                }

                await _combatScheduler.RunQueue();

                _currentFighter.OnTurnEnd();
                _gameEventBus?.Publish<TurnEndGameEvent>(new());
            }
        }

        private IEntity? GetEntityTarget() => _currentFighter?.ChoseTarget(_entities);

        private void OnQueueEmpty()
        {
            if (_entitiesCount == 0)
            {
                _fightEnds = true;
                return;
            }

            _queueScheduler.AddFighters(_entities);
        }

        private void OnEntityClicked(CharacterBody2D body)
        {
            if (_currentFighter is not Player) return;
            if (body is not IEntity entity) return;
            if (_playerTargetTcs == null || _playerTargetTcs.Task.IsCompleted) return;

            _playerTargetTcs.TrySetResult(entity);
        }

        private IAttackContext CreateAttackContext(IEntity currentFighter, IEntity target) => new AttackContext(currentFighter, target,
            currentFighter.Parameters.Damage * _rnd.RandfRange(0.9f, 1.1f), new RndGodot(), _combatScheduler);

        private void OnFighterDead(IFightable obj)
        {
            _entitiesCount--;
            _combatScheduler.CancelQueue();
        }

        private void OnPlayerDead(IFightable obj)
        {
            _fightEnds = true;
        }
    }
}
