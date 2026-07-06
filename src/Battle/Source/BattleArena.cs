namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Godot;
    using Godot.Collections;
    using Presentation;
    using UIElements;
    using Utilities;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices, ICameraFocus, IBattleField
    {
        private const string UID = "uid://bcj35twqggu1d";
        private readonly RandomNumberGenerator _rnd = new();
        private readonly AttackContextScheduler _attackContextScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private readonly BattleTimeline _timeline = new();
        private TaskCompletionSource<IFightable?>? _playerTargetTcs;
        private TargetSelectionController? _selectionController;
        private IBattleEventBus? _battleEventBus;
        private List<IFightable> _fighters = [];
        private int _playersEnemiesCount;
        private BattleOutcome? _battleOutcome;
        [Export] private Array<EntitySpot> _spots = [];
        [Export] private EntitySpot? _playerSpot;
        [Export] private BattleDirector? _director;
        [Export] private AbilityVisualLibrary? _visualLibrary;
        private CombatTextPresenter? _combatTextPresenter;
        private IPlayer? _player;
        private IFightable? _currentFighter;
        private bool _fightEnds;

        /// <summary>The ordered record of the current battle; the presentation layer replays it.</summary>
        public IBattleTimeline Timeline => _timeline;

        public override void _Ready()
        {
            _rnd.Randomize();
            _queueScheduler.QueueContainLessThenTwoFighters += OnQueueContainLessThenTwoFighters;
            _fightEnds = false;
        }


        public override void _ExitTree()
        {
            _battleEventBus = null;
            _timeline.DetachAll();
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
                var targets = _selectionController?.TakeCommittedTargets(obj.SelectionId).ToList() ?? [];
                await obj.Ability.Execute(targets, this);
            }
            catch (Exception e)
            {
                GD.Print($"Failed to activate ability {obj.Ability.Id}", e.Message, e.StackTrace);
                Tracker.TrackException("Failed to activate ability", e, this);
            }
        }

        public void SetPlayer(IFightable player)
        {
            if (_playerSpot == null) return;
            if (player is not IPlayer p) return;
            _playerSpot.SetEntity(player);
            _player = p;
            EnsureGroup(player); // ally/enemy semantics are group-based; companions will join this group later
        }

        public void RemovePlayerFromArena() => _playerSpot?.RemoveEntityFromSpot();
        public Vector2 GetCameraPosition() => GlobalPosition;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public IReadOnlyList<IFightable> GetEnemies(IFightable entity) =>
            _fighters.Where(fighter => fighter.IsAlive && !AreAllies(entity, fighter)).ToList();

        /// <summary>Living groupmates, the entity itself excluded.</summary>
        public IReadOnlyList<IFightable> GetAllies(IFightable entity) =>
            _fighters.Where(fighter => fighter.IsAlive && !fighter.IsSame(entity.InstanceId) && AreAllies(entity, fighter)).ToList();

        public IReadOnlyList<IFightable> GetAll() => _fighters.Where(x => x.IsAlive).ToList();

        public IFightable GetRandomEntity(IFightable entity)
        {
            var alive = _fighters.Where(x => x.IsAlive).ToList();

            return alive[_rnd.RandiRange(0, alive.Count - 1)];
        }

        public void RemoveAliveEntitiesFromArena()
        {
            foreach (var spot in _spots)
                spot.RemoveEntityFromSpot();
        }

        private void SetupTargetSelectionController()
        {
            if (_battleEventBus == null) return;
            var allSpots = _spots.Where(spot => spot != null).ToList();
            if (_playerSpot != null) allSpots.Add(_playerSpot);
            _selectionController = new TargetSelectionController(_battleEventBus, this, allSpots);
        }

        private void SetupCombatTextPresenter(IBattleEventBus battleEventBus)
        {
            _combatTextPresenter = new CombatTextPresenter();
            AddChild(_combatTextPresenter);
            _combatTextPresenter.Setup(battleEventBus, FindSpotFor);
        }

        /// <summary>Anchor for floating combat text: the spot currently holding the entity.</summary>
        private Node2D? FindSpotFor(string instanceId)
        {
            if (_playerSpot?.Entity?.IsSame(instanceId) == true) return _playerSpot;
            return _spots.FirstOrDefault(spot => spot.Entity?.IsSame(instanceId) == true);
        }

        public IFightable GetRandomAlly(IFightable entity)
        {
            var allies = GetAllies(entity);
            if (allies.Count == 0) return entity; // a lone fighter can only target itself

            return allies[_rnd.RandiRange(0, allies.Count - 1)];
        }

        /// <summary>
        /// Ally/enemy semantics: same group = allies, everyone outside = enemies.
        /// A fighter without a group is hostile to everyone and allied only with itself.
        /// </summary>
        private static bool AreAllies(IFightable first, IFightable second)
        {
            if (first.IsSame(second.InstanceId)) return true;
            return first.Group != null && ReferenceEquals(first.Group, second.Group);
        }

        private static void EnsureGroup(IFightable entity)
        {
            if (entity.Group != null) return;
            new EntityGroup().TryAddToGroup(entity);
        }

        public bool PrepareBattleArena(List<IFightable> fighters)
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
            SetupTargetSelectionController();
            SetupCombatTextPresenter(_battleEventBus);
            StartTimelineRecording();
            SetupBattleDirector(_battleEventBus);

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

                var skipCause = _currentFighter.StatusEffects.GetSkipTurnCause();
                if (skipCause != StatusEffects.None)
                {
                    // The turn starts and immediately ends: start/end effects, dots and
                    // cooldowns still tick, only the action phase is skipped.
                    _currentFighter.CombatEvents.Publish(new TurnSkippedEvent(_currentFighter, skipCause));
                }
                else
                {
                    var target = await ResolveTargetAsync(_currentFighter);
                    if (target is { IsAlive: true })
                    {
                        var context = CreateAttackContext(_currentFighter, target);
                        context.RawCriticalChance = _currentFighter.Parameters.CriticalChance;
                        _attackContextScheduler.Schedule(context);
                        await _attackContextScheduler.DrainQueue();
                    }
                }

                _currentFighter.OnTurnEnd();

                // Turn gate: the whole turn resolved instantly above; the next fighter
                // doesn't start until the director has shown everything recorded so far.
                await WaitForPresentationAsync();

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
                //     _playerTargetTcs = new TaskCompletionSource<IFightable?>();
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

            // Final gate: death and battle-ending beats must finish before the results are handled.
            await WaitForPresentationAsync();

            return _battleOutcome?.Results ?? BattleResults.BattleAbandoned;
        }

        /// <summary>Turn gate between logic time and presentation time.</summary>
        private async Task WaitForPresentationAsync()
        {
            if (_director == null) return;
            await _director.WaitUntilIdleAsync();
        }

        private void SetupBattleDirector(IBattleEventBus battleEventBus)
        {
            _director?.Setup(_timeline, battleEventBus, CreateVfxPresenter());
        }

        /// <summary>VFX live in arena space (spot anchors); without a library the director plays without ability VFX.</summary>
        private AbilityVfxPresenter? CreateVfxPresenter()
        {
            if (_visualLibrary == null)
            {
                GD.PushWarning("BattleArena: _visualLibrary is not assigned — ability VFX are disabled.");
                return null;
            }
            var presenter = new AbilityVfxPresenter();
            AddChild(presenter);
            presenter.Setup(FindSpotFor, _visualLibrary);
            return presenter;
        }

        /// <summary>Every fighter's personal bus feeds the shared timeline; entries arrive in causal order.</summary>
        private void StartTimelineRecording()
        {
            _timeline.Clear();
            foreach (var fighter in _fighters)
                _timeline.Attach(fighter.CombatEvents);
        }

        private void EndBattle(BattleOutcome outcome)
        {
            if (_battleOutcome != null) return;
            _battleOutcome = outcome;
            _timeline.DetachAll();

            if (_playerTargetTcs is { Task.IsCompleted: false })
                _playerTargetTcs.SetResult(null);
        }

        private async Task<IFightable?> ResolveTargetAsync(IFightable fighter)
        {
            if (fighter is not IPlayer)
            {
                return fighter.ChoseTarget(_fighters);
            }

            _playerTargetTcs = new TaskCompletionSource<IFightable?>();
            _selectionController?.BeginBasicAttack(fighter);
            try
            {
                return await _playerTargetTcs.Task;
            }
            finally
            {
                _selectionController?.EndTargeting();
            }
        }

        private void OnQueueContainLessThenTwoFighters()
        {
            _fightEnds = true;
        }

        private void OnAttackTargetSelected(AttackTargetSelectedEvent obj)
        {
            if (_currentFighter is not IPlayer) return;
            // The input window is closed while beats are playing: an attack clicked
            // mid-animation (e.g. during an ability cast being shown) must not resolve.
            if (_director?.IsPlaying == true) return;
            if (_playerTargetTcs == null || _playerTargetTcs.Task.IsCompleted) return;

            _playerTargetTcs.SetResult(obj.Target);
        }

        private IAttackContext CreateAttackContext(IFightable currentFighter, IFightable target) => new AttackContext(currentFighter, target,
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
