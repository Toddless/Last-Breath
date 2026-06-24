namespace Battle.Source
{
    using Godot;
    using System;
    using Core.Data;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Godot.Collections;
    using Core.Interfaces.UI;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices, ICameraFocus, IBattleField
    {
        private const string UID = "uid://bcj35twqggu1d";
        private readonly RandomNumberGenerator _rnd = new();
        private readonly AttackContextScheduler _attackContextScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private IBattleEventBus? _battleEventBus;
        private List<IEntity> _fighters = [];
        private int _playersEnemiesCount;
        private BattleOutcome? _battleOutcome;
        [Export] private Array<EntitySpot> _spots = [];
        [Export] private EntitySpot? _playerSpot;
        private IPlayer? _player;
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
            _battleEventBus.Subscribe<AbilityActivationEvent>(OnAbilityActivation);
        }

        private async void OnAbilityActivation(AbilityActivationEvent obj)
        {
            try
            {
                var targets = _spots
                    .Where(s => s.SelectionId == obj.SelectionId)
                    .Select(s => s.Entity)
                    .OfType<IEntity>()
                    .ToList();

                await obj.Ability.Execute(targets, this);
            }
            catch (Exception e)
            {
                GD.Print($"Failed to activate ability {obj.Ability.Id}", e.Message, e.StackTrace);
                Tracker.TrackException("Failed to activate ability", e, this);
            }
        }


        public void SetPlayer(IEntity player)
        {
            if (_playerSpot == null) return;
            if (player is not IPlayer p) return;
            _playerSpot.SetEntity(player);
            _player = p;
        }

        public void RemovePlayerFromArena() => _playerSpot?.RemoveEntityFromSpot();
        public Vector2 GetCameraPosition() => GlobalPosition;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public IReadOnlyList<IEntity> GetEnemies(IEntity entity) => throw new NotImplementedException();

        // how I can define with entity is an enemy/ally to player or each other?
        public IReadOnlyList<IEntity> GetAllies(IEntity entity) => throw new NotImplementedException();

        public IReadOnlyList<IEntity> GetAll() => _fighters.Where(x => x.IsAlive).ToList();

        public IEntity GetRandomEntity(IEntity entity)
        {
            var alive = _fighters.Where(x => x.IsAlive).ToList();

            return alive[_rnd.RandiRange(0, alive.Count - 1)];
        }

        public void RemoveAliveEntitiesFromArena()
        {
            foreach (var spot in _spots)
                spot.RemoveEntityFromSpot();
        }

        public IEntity GetRandomAlly(IEntity entity) => throw new NotImplementedException();

        public bool PrepareBattleArena(List<IEntity> fighters)
        {
            if (_battleEventBus == null) return false;
            int enemiesCount = fighters.Count;

            for (int i = 0; i < enemiesCount; i++)
            {
                var npc = fighters[i];
                _fighters.Add(npc);
                _spots[i].SetEntity(npc);
            }

            _playersEnemiesCount = enemiesCount;

            _fighters = fighters;
            if (_player != null)
                _fighters.Add(_player);

            foreach (var spot in _spots)
            {
                if (!spot.HasEntityInit()) continue;
                spot.SetBattleEventBus(_battleEventBus);
            }

            _playerSpot?.SetBattleEventBus(_battleEventBus);

            var fightersQueue = _queueScheduler.AddFighters(_fighters);
            _battleEventBus.Publish<BattleQueueDefinedEvent>(new(fightersQueue));
            return true;
        }

        public async Task<BattleResults> RunBattleAsync()
        {
            while (_battleOutcome == null)
            {
                if (!_queueScheduler.TryGetNextFighter(out _currentFighter))
                {
                    EndBattle(new BattleOutcome(BattleResults.BattleAbandoned));
                    break;
                }

                if (_currentFighter is not { IsAlive: true }) continue;
                _currentFighter.OnTurnStart();

                var target = await ResolveTargetAsync(_currentFighter);
                if (target is { IsAlive: true })
                {
                    var context = CreateAttackContext(_currentFighter, target);
                    context.RawCriticalChance = _currentFighter.Parameters.CriticalChance;
                    _attackContextScheduler.Schedule(context);
                    await _attackContextScheduler.DrainQueue();
                }

                _currentFighter.OnTurnEnd();

                var queue = _queueScheduler.RefillIfEmpty(_fighters);
                if (queue.Count > 1)
                    _battleEventBus?.Publish(new BattleQueueDefinedEvent(queue));

                #region OldLogic

                // if (!_queueScheduler.TryGetNextFighter(out _currentFighter)) break;
                //
                // if (_currentFighter is not { IsAlive: true }) continue;
                //
                // _currentFighter.OnTurnStart();
                // if (_currentFighter is IPlayer)
                // {
                //     _playerTargetTcs = new TaskCompletionSource<IEntity?>();
                //
                //     var target = await _playerTargetTcs.Task;
                //
                //     if (target is not { IsAlive: true }) continue;
                //     var context = CreateAttackContext(_currentFighter, target);
                //     context.RawCriticalChance = _currentFighter.Parameters.CriticalChance;
                //     _attackContextScheduler.Schedule(context);
                // }
                // else
                // {
                //     var target = GetEntityTarget();
                //     if (target is not { IsAlive: true }) continue;
                //     var context = CreateAttackContext(_currentFighter, target);
                //     context.RawCriticalChance = _currentFighter.Parameters.CriticalChance;
                //     _attackContextScheduler.Schedule(context);
                // }
                //
                // await _attackContextScheduler.DrainQueue();
                //
                // _currentFighter.OnTurnEnd();
                //
                // var queue = _queueScheduler.RefillIfEmpty(_fighters);
                // if (queue.Count > 1) _battleEventBus?.Publish<BattleQueueDefinedEvent>(new(queue));

                #endregion
            }

            return _battleOutcome?.Results ?? BattleResults.BattleAbandoned;
        }

        private void EndBattle(BattleOutcome outcome)
        {
            if (_battleOutcome != null) return;
            _battleOutcome = outcome;

            if (_playerTargetTcs is { Task.IsCompleted: false })
                _playerTargetTcs.SetResult(null);
        }

        private async Task<IEntity?> ResolveTargetAsync(IEntity fighter)
        {
            if (fighter is not IPlayer)
            {
                return fighter.ChoseTarget(_fighters);
            }

            _playerTargetTcs = new TaskCompletionSource<IEntity?>();
            return await _playerTargetTcs.Task;
        }

        private void OnQueueContainLessThenTwoFighters()
        {
            _fightEnds = true;
        }

        private void OnAttackTargetSelected(AttackTargetSelectedEvent obj)
        {
            if (_currentFighter is not IPlayer) return;
            if (_playerTargetTcs == null || _playerTargetTcs.Task.IsCompleted) return;

            _playerTargetTcs.SetResult(obj.Target);
        }

        private IAttackContext CreateAttackContext(IEntity currentFighter, IEntity target) => new AttackContext(currentFighter, target,
            currentFighter.GetDamage(), new RandomNumberGenerator(), _attackContextScheduler);

        private void OnEntityDead(EntityDiedEvent obj)
        {
            _playersEnemiesCount--;
            _fighters.Remove(obj.Entity);
            if (_playersEnemiesCount <= 0 && _currentFighter is IPlayer && _playerTargetTcs is { Task.IsCompleted: false })
            {
                _playerTargetTcs?.SetResult(null);
                _fightEnds = true;
            }
        }

        private void OnPlayerDead(PlayerDiedEvent evnt)
        {
            _fightEnds = true;
        }

        private class BattleOutcome(BattleResults results)
        {
            public BattleResults Results = results;
        }
    }
}
