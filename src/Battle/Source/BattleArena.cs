namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Ai;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Extensions;
    using Core.Interfaces;
    using Core.Views.UI;
    using Godot;
    using Godot.Collections;
    using Presentation;
    using UIElements;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices, ICameraFocus, IBattleField, ICombatEnvironment
    {
        private const string UID = "uid://bcj35twqggu1d";
        private readonly RandomNumberGenerator _rnd = new();
        private readonly ICombatTurnPlanner _turnPlanner = new UtilityTurnPlanner(new DefaultRandomNumberGenerator());
        private readonly AttackContextScheduler _attackContextScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private readonly BattleTimeline _timeline = new();
        private TaskCompletionSource<IFightable?>? _playerTargetTcs;
        private TargetSelectionController? _selectionController;
        private IBattleEventBus? _battleEventBus;
        private List<IFightable> _fighters = [];
        private BattleOutcome? _battleOutcome;
        [Export] private Array<EntitySpot> _spots = [];

        [Export] private EntitySpot? _playerSpot;

        // ВАЖНО: без назначенного директора бой не завершается победой — смерти доезжают до арены
        // только репаблишем EntityDiedEvent при проигрыше битов (replay-модель). Headless/тестовой
        // арене нужен фейковый директор, иначе цикл ходов крутится вечно.
        [Export] private BattleDirector? _director;
        [Export] private AbilityVisualLibrary? _visualLibrary;
        private CombatTextPresenter? _combatTextPresenter;
        private IPlayer? _player;
        private IFightable? _currentFighter;
        private readonly HashSet<string> _fledIds = [];
        // Corpses freed their spots for latecomers but stay visible on the field until the
        // context returns them to the world; they also serve as presentation anchors.
        private readonly System.Collections.Generic.Dictionary<string, Node2D> _parkedCorpses = [];

        /// <summary>The ordered record of the current battle; the presentation layer replays it.</summary>
        public IBattleTimeline Timeline => _timeline;

        public override void _Ready()
        {
            _rnd.Randomize();
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

        public void RemovePlayerFromArenaSpot() => _playerSpot?.RemoveEntityFromSpot();
        public Vector2 GetCameraPosition() => GlobalPosition;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public IReadOnlyList<IFightable> GetEnemies(IFightable entity) =>
            _fighters.Where(fighter => IsPresent(fighter) && !AreAllies(entity, fighter)).ToList();

        /// <summary>Living groupmates, the entity itself excluded.</summary>
        public IReadOnlyList<IFightable> GetAllies(IFightable entity) =>
            _fighters.Where(fighter => IsPresent(fighter) && !fighter.IsSame(entity.InstanceId) && AreAllies(entity, fighter)).ToList();

        public IReadOnlyList<IFightable> GetAll() => _fighters.Where(IsPresent).ToList();

        public IFightable GetRandomEntity(IFightable entity)
        {
            var alive = _fighters.Where(IsPresent).ToList();

            return alive[_rnd.RandiRange(0, alive.Count - 1)];
        }

        /// <summary>Still on the field: alive and not fled. Dead fighters stay in the roster
        /// (the context returns their bodies to the world), so every query must filter here.</summary>
        private bool IsPresent(IFightable fighter) =>
            fighter.IsAlive && !_fledIds.Contains(fighter.InstanceId);

        public void RemoveEntitiesFromArenaSpots()
        {
            foreach (var spot in _spots)
                spot.RemoveEntityFromSpot();
        }

        private void SetupTargetSelectionController()
        {
            if (_battleEventBus == null) return;
            var allSpots = _spots.Where(_ => true).ToList();
            if (_playerSpot != null) allSpots.Add(_playerSpot);
            _selectionController = new TargetSelectionController(_battleEventBus, this, allSpots);
        }

        private void SetupCombatTextPresenter(IBattleEventBus battleEventBus)
        {
            _combatTextPresenter = new CombatTextPresenter();
            AddChild(_combatTextPresenter);
            _combatTextPresenter.Setup(battleEventBus, FindSpotFor);
        }

        /// <summary>Anchor for floating combat text and melee approach: the spot currently holding
        /// the entity, or the parked corpse node itself (its spot was freed for a latecomer).</summary>
        private Node2D? FindSpotFor(string instanceId)
        {
            if (_playerSpot?.Entity?.IsSame(instanceId) == true) return _playerSpot;
            var spot = _spots.FirstOrDefault(s => s.Entity?.IsSame(instanceId) == true);
            if (spot != null) return spot;
            return _parkedCorpses.GetValueOrDefault(instanceId);
        }

        public IFightable GetRandomAlly(IFightable entity)
        {
            var allies = GetAllies(entity);
            if (allies.Count == 0) return entity; // a lone fighter can only target itself

            return allies[_rnd.RandiRange(0, allies.Count - 1)];
        }



        /// <summary>
        /// A latecomer enters the ongoing battle: takes a free spot, joins the roster and the
        /// timeline (queue picks it up on the next round). Multi-sided by design — an enemy keeps
        /// its own group, a companion joins the player's.
        /// </summary>
        public bool TryJoinBattle(IFightable fighter, bool alliedWithPlayer)
        {
            if (_battleEventBus == null || _battleOutcome != null) return false;
            if (_fighters.Any(existing => existing.IsSame(fighter.InstanceId))) return false;

            var freeSpot = _spots.FirstOrDefault(spot => !spot.HasEntityInit());
            if (freeSpot == null) return false;

            if (alliedWithPlayer && _player != null) _player.Group?.TryAddToGroup(fighter);

            _fighters.Add(fighter);
            freeSpot.SetEntity(fighter);
            freeSpot.SetBattleEventBus(_battleEventBus);
            _timeline.Attach(fighter.CombatEvents);
            return true;
        }

        public bool PrepareBattleArena(List<IFightable> fighters)
        {
            if (_battleEventBus == null) return false;
            if (fighters.Count > _spots.Count)
            {
                // A clean abort instead of an index crash mid-setup; the context's finally
                // still publishes BattleEndEvent, so nothing is left stuck in Fight.
                Tracker.TrackError($"Not enough arena spots: {fighters.Count} fighters for {_spots.Count} spots", this);
                return false;
            }

            // Own copy: death/flee bookkeeping must never mutate the caller's participant list —
            // the context returns EVERYONE (bodies of the dead included) to the world from it.
            _fighters = [.. fighters];
            if (_player != null) _fighters.Add(_player);

            for (int i = 0; i < fighters.Count; i++)
            {
                var spot = _spots[i];
                spot.SetEntity(fighters[i]);
                spot.SetBattleEventBus(_battleEventBus);
            }

            // TODO:
            // Сейчас на арене создан только спот для игрока. НЕобходимы споты для союзников
            _playerSpot?.SetBattleEventBus(_battleEventBus);
            SetupTargetSelectionController();
            SetupCombatTextPresenter(_battleEventBus);
            StartTimelineRecording();
            SetupBattleDirector(_battleEventBus);

            var fightersQueue = _queueScheduler.AddFighters(_fighters);
            _battleEventBus.Publish<BattleQueueDefinedEvent>(new(fightersQueue));
            return true;
        }

        /// <summary>NPC cast path: straight to Execute — TargetSelectionController is the player's UI path.</summary>
        public Task CastAbilityAsync(IFightable caster, IAbility ability, IReadOnlyList<IFightable> targets) =>
            ability.Execute(targets.ToList(), this);

        public async Task BasicAttackAsync(IFightable attacker, IFightable target)
        {
            var context = CreateAttackContext(attacker, target);
            context.RawCriticalChance = attacker.Parameters.CriticalChance;
            _attackContextScheduler.Schedule(context);
            await _attackContextScheduler.DrainQueue();
        }

        /// <summary>
        /// The fighter leaves the battle alive: out of the fighter list and the enemy count,
        /// its state restores with everyone else on battle end. Counts toward victory like a death.
        /// </summary>
        public Task FleeBattleAsync(IFightable fighter)
        {
            if (!_fledIds.Add(fighter.InstanceId)) return Task.CompletedTask;

            // Timeline for the future flee beat/log entry; battle bus for the XP processor.
            fighter.CombatEvents.Publish(new EntityFledBattleEvent(fighter));
            _battleEventBus?.Publish(new EntityFledBattleEvent(fighter));
            CheckPlayerVictory();
            return Task.CompletedTask;
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
                if (_fledIds.Contains(_currentFighter.InstanceId)) continue; // fled fighters may linger in the current queue
                _currentFighter.OnTurnStart();

                var skipCause = _currentFighter.StatusEffects.GetSkipTurnCause();
                if (skipCause != StatusEffects.None)
                {
                    // The turn starts and immediately ends: start/end effects, dots and
                    // cooldowns still tick, only the action phase is skipped.
                    _currentFighter.CombatEvents.Publish(new TurnSkippedEvent(_currentFighter, skipCause));
                }
                else if (_currentFighter is IFightableNpc { Behavior: { } behavior } npc)
                {
                    // Data-driven NPC: the planner plays the whole turn (free casts + closing attack).
                    await _turnPlanner.PlayTurnAsync(npc, behavior, this);
                }
                else
                {
                    // Player turn and the legacy fallback for NPCs without a behavior profile.
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

                var queue = _queueScheduler.RefillIfEmpty(_fighters.Where(IsPresent).ToList());
                if (queue.Count > 1)
                    _battleEventBus?.Publish(new BattleQueueDefinedEvent(queue));
            }

            // Final gate: death and battle-ending beats must finish before the results are handled.
            await WaitForPresentationAsync();
            _timeline.DetachAll();

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
            // FindSpotFor doubles as the melee-approach anchor lookup for attack phrases.
            _director?.Setup(_timeline, battleEventBus, CreateVfxPresenter(), FindSpotFor);
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

        /// <summary>Every fighter's personal bus feeds the shared timeline; entries arrive in causal order.</summary>
        private void StartTimelineRecording()
        {
            _timeline.Clear();
            foreach (var fighter in _fighters)
                _timeline.Attach(fighter.CombatEvents);
        }

        /// <summary>
        /// Locks the outcome; the loop exits on the next iteration. The timeline keeps recording —
        /// the killing cast's trailing events must still reach the presentation; detach happens
        /// after the final gate in RunBattleAsync.
        /// </summary>
        private void EndBattle(BattleOutcome outcome)
        {
            if (_battleOutcome != null) return;
            _battleOutcome = outcome;

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

        // ICombatEnvironment: the primitives the NPC turn planner acts through.
        IBattleField ICombatEnvironment.Field => this;

        private void OnEntityDead(EntityDiedEvent obj)
        {
            if (obj.Entity is IPlayer) return; // the player's defeat resolves through PlayerDiedEvent
            FreeSpotOf(obj.Entity);
            CheckPlayerVictory();
        }

        /// <summary>Death frees the spot for latecomers: the corpse reparents to the arena at the
        /// same position (still lying on the field, returned to the world by the context at the end).</summary>
        private void FreeSpotOf(IFightable entity)
        {
            var spot = _spots.FirstOrDefault(s => s.Entity?.IsSame(entity.InstanceId) == true);
            if (spot == null || entity is not Node2D corpse) return;

            var worldPosition = corpse.GlobalPosition;
            spot.RemoveEntityFromSpot();
            AddChild(corpse);
            corpse.GlobalPosition = worldPosition;
            _parkedCorpses[entity.InstanceId] = corpse;
        }


        /// <summary>
        /// Group-based outcome: the player wins when no enemy of HIS group is standing.
        /// An ally's death or flight never shrinks the enemy side by accident.
        /// </summary>
        private void CheckPlayerVictory()
        {
            if (_player == null) return;
            if (_fighters.Any(fighter => IsPresent(fighter) && !AreAllies(_player, fighter))) return;

            // No standing enemies: dead or fled, the player took the field.
            if (_playerTargetTcs is { Task.IsCompleted: false })
                _playerTargetTcs.SetResult(null);
            EndBattle(new BattleOutcome(BattleResults.PlayerWon));
        }

        private void OnPlayerDead(PlayerDiedEvent evnt) => EndBattle(new BattleOutcome(BattleResults.PlayerLost));

        private class BattleOutcome(BattleResults results)
        {
            public readonly BattleResults Results = results;
        }
    }
}
