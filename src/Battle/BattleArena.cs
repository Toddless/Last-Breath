namespace Battle
{
    using System;
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
        private readonly AttackContextScheduler _attackContextScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private IBattleEventBus? _battleEventBus;
        private List<IEntity> _fighters = [];
        private int _playerEnemiesCount;
        [Export] private Array<EntitySpot> _spots = [];
        [Export] private EntitySpot? _playerSpot;
        private Player? _player;
        private IEntity? _currentFighter;
        private bool _fightEnds;

        private TaskCompletionSource<IEntity?>? _playerTargetTcs;

        public override void _Ready()
        {
            _rnd.Randomize();
            _queueScheduler.QueueContainLessThenTwoFighters += OnQueueContainLessThenTwoFighters;
            _fightEnds = false;
        }

        public override void _ExitTree()
        {
            _battleEventBus = null;
            foreach (EntitySpot entitySpot in _spots)
                entitySpot.RemoveBattleEventBus();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerDiedEvent>(OnPlayerDead);
            _battleEventBus.Subscribe<EntityDiedEvent>(OnEntityDead);
            _battleEventBus.Subscribe<AttackTargetSelectedEvent>(OnAttackTargetSelected);
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
            ArgumentNullException.ThrowIfNull(_battleEventBus);
            int enemiesCount = fighters.Count;

            for (int i = 0; i < enemiesCount; i++)
            {
                var npc = fighters[i];
                _fighters.Add(npc);
                _spots[i].SetEntity(npc);
            }

            _playerEnemiesCount = enemiesCount;

            if (_player != null)
                fighters.Add(_player);
            _fighters = fighters;

            foreach (var spot in _spots)
            {
                if(!spot.HasEntityInit()) continue;
                spot.SetBattleEventBus(_battleEventBus);
            }
            _playerSpot?.SetBattleEventBus(_battleEventBus);

            var fightersQueue = _queueScheduler.AddFighters(fighters);
            _battleEventBus.Publish<BattleQueueDefinedEvent>(new(fightersQueue));
            await ProcessTurnsAsync();
        }

        public void RemoveAliveEntitiesFromArena()
        {
            foreach (var spot in _spots)
                spot.RemoveEntityFromSpot();
        }

        public void RemovePlayerFromArena() => _playerSpot?.RemoveEntityFromSpot();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async Task ProcessTurnsAsync()
        {
            while (!_fightEnds && _playerEnemiesCount > 0)
            {
                if (!_queueScheduler.TryGetNextFighter(out _currentFighter)) break;

                if (_currentFighter is not { IsAlive: true }) continue;

                _currentFighter.OnTurnStart();
                if (_currentFighter is Player)
                {
                    _playerTargetTcs = new TaskCompletionSource<IEntity?>();

                    var target = await _playerTargetTcs.Task;

                    if (target is not { IsAlive: true }) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _attackContextScheduler.Schedule(context);
                }
                else
                {
                    var target = GetEntityTarget();
                    if (target is not { IsAlive: true }) continue;
                    var context = CreateAttackContext(_currentFighter, target);
                    _attackContextScheduler.Schedule(context);
                }

                await _attackContextScheduler.RunQueue();

                _currentFighter.OnTurnEnd();

                var queue = _queueScheduler.RefillIfEmpty(_fighters);
                if (queue.Count > 1) _battleEventBus?.Publish<BattleQueueDefinedEvent>(new(queue));
            }
        }

        private IEntity? GetEntityTarget() => _currentFighter?.ChoseTarget(_fighters);

        private void OnQueueContainLessThenTwoFighters()
        {
            _fightEnds = true;
        }

        private void OnAttackTargetSelected(AttackTargetSelectedEvent obj)
        {
            if (_currentFighter is not Player) return;
            if (_playerTargetTcs == null || _playerTargetTcs.Task.IsCompleted) return;

            _playerTargetTcs.SetResult(obj.Target);
        }

        private IAttackContext CreateAttackContext(IEntity currentFighter, IEntity target) => new AttackContext(currentFighter, target,
            currentFighter.GetDamage(), new RndGodot(), _attackContextScheduler);

        private void OnEntityDead(EntityDiedEvent obj)
        {
            _playerEnemiesCount--;
            _fighters.Remove(obj.Entity);
            if (_playerEnemiesCount <= 0 && _currentFighter is Player && _playerTargetTcs is { Task.IsCompleted: false })
            {
                _playerTargetTcs?.SetResult(null);
                _fightEnds = true;
            }
        }

        private void OnPlayerDead(PlayerDiedEvent evnt)
        {
            _fightEnds = true;
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
