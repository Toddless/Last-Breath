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
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices, ICameraFocus
    {
        private const string UID = "uid://dph8vnuwipwoc";
        private readonly RandomNumberGenerator _rnd = new();
        private readonly CombatScheduler _combatScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;
        private List<IEntity> _entities = [];
        private int _entitiesCount;
        [Export] private Array<EntitySpot> _spots = [];
        [Export] private EntitySpot? _playerSpot;
        private Player? _player;
        private IEntity? _currentFighter;
        private bool _fightEnds;

        private TaskCompletionSource<IEntity>? _playerTargetTcs;

        public override void _Ready()
        {
            _rnd.Randomize();
            _queueScheduler.QueueEmpty += OnQueueEmpty;
            _fightEnds = false;
            foreach (EntitySpot enemySpot in _spots)
                enemySpot.EntityClicked += OnEntityClicked;
        }

        public override void _ExitTree()
        {
            _battleEventBus?.Unsubscribe<PlayerDiedGameEvent>(OnPlayerDead);
            _battleEventBus?.Unsubscribe<EntityDiedGameEvent>(OnEntityDead);
            _battleEventBus = null;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerDiedGameEvent>(OnPlayerDead);
            _battleEventBus.Subscribe<EntityDiedGameEvent>(OnEntityDead);
        }

        public void SetPlayer(IEntity player)
        {
            if (_playerSpot == null) return;
            if (player is not Player p) return;
            _playerSpot.SetEntity(player);
            _player = p;
        }

        public async Task StartFight(List<IEntity> fighters)
        {
            int amount = fighters.Count;

            for (int i = 0; i < amount; i++)
            {
                var enemy = fighters[i];
                _entities.Add(enemy);
                _spots[i].SetEntity(enemy);
            }

            _entitiesCount = amount;

            if (_player != null)
                fighters.Add(_player);
            _entities = fighters;

            var entities = _queueScheduler.AddFighters(fighters);
            _battleEventBus?.Publish<BattleQueueDefinedGameEvent>(new(entities));
            await ProcessTurnsAsync();
        }

        public void RemoveAliveEntitiesFromArena()
        {
            foreach (var spot in _spots)
                spot.RemoveEntityFromSpot();
        }

        public void RemovePlayerFromArena()
        {
            _playerSpot?.RemoveEntityFromSpot();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async Task ProcessTurnsAsync()
        {
            while (!_fightEnds)
            {
                if (_entitiesCount == 0) break;
                _currentFighter = _queueScheduler.GetCurrentFighter();
                if (_currentFighter is not { IsAlive: true }) continue;

                _currentFighter.OnTurnStart();
                if (_currentFighter is Player)
                {
                    _playerTargetTcs = new TaskCompletionSource<IEntity>();

                    var target = await _playerTargetTcs.Task;

                    if (target is not { IsAlive: true }) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _combatScheduler.Schedule(context);
                }
                else
                {
                    var target = GetEntityTarget();
                    if (target == null) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _combatScheduler.Schedule(context);
                }

                await _combatScheduler.RunQueue();

                _currentFighter.OnTurnEnd();
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

            var entities = _queueScheduler.AddFighters(_entities);
            _battleEventBus?.Publish<BattleQueueDefinedGameEvent>(new(entities));
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

        private void OnEntityDead(EntityDiedGameEvent obj)
        {
            _entitiesCount--;
            _combatScheduler.CancelQueue();
            _entities.Remove(obj.Entity);
        }

        private void OnPlayerDead(PlayerDiedGameEvent evnt)
        {
            _fightEnds = true;
            _combatScheduler.CancelQueue();
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
