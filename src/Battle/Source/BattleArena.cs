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
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Extensions;
    using Core.Interfaces;
    using Core.Narrative.Facts;
    using Core.Views.UI;
    using Godot;
    using Godot.Collections;
    using Presentation;
    using UIElements;

    public partial class BattleArena : Node2D, IInitializable, IRequireServices, ICameraFocus, IBattleField, ICombatEnvironment, ISummonHandler
    {
        private const string UID = "uid://bcj35twqggu1d";

        // Anchor of the slot formation in arena space: the scene's player spot sits one
        // ClusterDistance to its left, so the classic 1-group battle keeps today's look.
        private static readonly Vector2 s_formationCenter = new(1000f, 550f);
        private readonly RandomNumberGenerator _rnd = new();
        private readonly ICombatTurnPlanner _turnPlanner = new UtilityTurnPlanner(new DefaultRandomNumberGenerator());
        private readonly AttackContextScheduler _attackContextScheduler = new();
        private readonly QueueScheduler _queueScheduler = new();
        private readonly BattleTimeline _timeline = new();
        private TaskCompletionSource<IFightable?>? _playerTargetTcs;
        private TargetSelectionController? _selectionController;
        private IBattleEventBus? _battleEventBus;
        private IGameEventBus? _gameEventBus;
        private IAbilityProvider? _abilityProvider;
        private IWorldFactsService? _worldFacts;
        private NpcReactionsDriver? _reactionsDriver;
        private BossStagesController? _bossStagesController;
        private SummonService? _summonService;
        private INpcProvider? _npcProvider;
        private IBattleNpcSpawner? _summonSpawner;
        private ArenaRules _arenaRules = ArenaRules.Default;
        private ArenaFormation? _formation;
        private List<IFightable> _fighters = [];
        private BattleOutcome? _battleOutcome;
        [Export] private Array<EntitySpot> _spots = [];

        // Every slot of this battle: the authored scene pool, the player's spot and the
        // programmatic ones (latecomers past the pool, summons). The selection controller holds
        // the same live list, so new slots are targetable without rewiring.
        private readonly List<EntitySpot> _allSpots = [];

        // Summon slots die with their summon (no corpse holds them), so they are tracked apart.
        private readonly System.Collections.Generic.Dictionary<string, EntitySpot> _summonSpots = [];

        [Export] private EntitySpot? _playerSpot;

        // ВАЖНО: без назначенного директора бой не завершается победой — смерти доезжают до арены
        // только репаблишем EntityDiedEvent при проигрыше битов (replay-модель). Headless/тестовой
        // арене нужен фейковый директор, иначе цикл ходов крутится вечно.
        [Export] private BattleDirector? _director;
        [Export] private AbilityVisualLibrary? _visualLibrary;

        // The arena's own view: frames the whole battlefield for the duration of the battle
        // (the player's follow-camera would keep the field half off-screen). The player re-takes
        // the view when he is returned to the world (Player.OnReparented).
        [Export] private Camera2D? _camera;
        private CombatTextPresenter? _combatTextPresenter;
        private IPlayer? _player;
        private IFightable? _currentFighter;

        private readonly HashSet<string> _fledIds = [];

        // Corpses freed their spots for latecomers but stay visible on the field until the
        // context returns them to the world; they also serve as presentation anchors.
        private readonly System.Collections.Generic.Dictionary<string, Node2D> _parkedCorpses = [];

        // Ally/enemy semantics are group-based, so groupless same-faction fighters would be
        // mutual enemies: a skeleton's chain lightning legally "executed" its own kin (read as
        // a self-kill). Solo NPCs of one faction share a battle-scoped group instead.
        private readonly System.Collections.Generic.Dictionary<Fractions, EntityGroup> _fractionGroups = [];

        public override void _Ready()
        {
            _rnd.Randomize();
        }


        public override void _ExitTree()
        {
            _reactionsDriver?.Dispose();
            _reactionsDriver = null;
            _bossStagesController?.Dispose();
            _bossStagesController = null;
            _summonService?.DespawnAll(); // belt-and-suspenders: an aborted battle must not leak orphaned summon nodes
            _summonService?.Dispose();
            _summonService = null;
            _battleEventBus = null;
            _gameEventBus = null;
            _timeline.DetachAll();
            foreach (EntitySpot entitySpot in _allSpots)
                entitySpot.RemoveBattleEventBus();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
            _abilityProvider = provider.GetService<IAbilityProvider>();
            // Optional like IInventory in the loot pipeline: a sandbox without world facts
            // still fights, the twin gate simply never blocks.
            _worldFacts = provider.GetServices<IWorldFactsService>().FirstOrDefault();
            // All optional for the same reason: a project without them fights with defaults and no summons.
            _arenaRules = provider.TryGet<ICombatRulesProvider>()?.Arena ?? ArenaRules.Default;
            _npcProvider = provider.GetServices<INpcProvider>().FirstOrDefault();
            _summonSpawner = provider.GetServices<IBattleNpcSpawner>().FirstOrDefault();
        }

        public void SetupEventBus(IBattleEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerDiedEvent>(OnPlayerDead);
            _battleEventBus.Subscribe<EntityDiedEvent>(OnEntityDead);
            _battleEventBus.Subscribe<AttackTargetSelectedEvent>(OnAttackTargetSelected);
            _battleEventBus.Subscribe<AbilityActivationEvent>(OnAbilityActivation);
            _battleEventBus.Subscribe<PlayerEndTurnRequestedEvent>(OnPlayerEndTurnRequested);
            _battleEventBus.Subscribe<PlayerFleeAttemptEvent>(OnPlayerFleeAttempt);
            _battleEventBus.Subscribe<BossStageTransitionStartedEvent>(OnBossStageTransitionStarted);
        }

        /// <summary>A boss armed its stage transition: the current turn force-ends after the running
        /// action (design). The player's pending input resolves to "no attack"; a mid-resolve action
        /// is never torn — the boss's immunity already nullifies its tail. NPC turns end on their own.</summary>
        private void OnBossStageTransitionStarted(BossStageTransitionStartedEvent evnt)
        {
            if (_currentFighter is not IPlayer) return;
            _playerTargetTcs?.TrySetResult(null);
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


        public void RemoveEntitiesFromArenaSpots()
        {
            foreach (var spot in _allSpots)
            {
                if (spot == _playerSpot) continue; // the player leaves through RemovePlayerFromArenaSpot
                spot.RemoveEntityFromSpot();
            }
        }

        public IFightable GetRandomAlly(IFightable entity)
        {
            var allies = GetAllies(entity);
            return allies.Count == 0
                ? entity
                : // a lone fighter can only target itself
                allies[_rnd.RandiRange(0, allies.Count - 1)];
        }

        public IFightable GetRandomEntity(IFightable entity)
        {
            var alive = _fighters.Where(IsPresent).ToList();

            return alive[_rnd.RandiRange(0, alive.Count - 1)];
        }

        /// <summary>Same input window as the attack click: the player's turn, nothing animating.</summary>
        private bool CanResolvePlayerAction() =>
            _currentFighter is IPlayer
            && _director?.IsPlaying != true
            && _playerTargetTcs is { Task.IsCompleted: false };

        /// <summary>Still on the field: alive and not fled. Dead fighters stay in the roster
        /// (the context returns their bodies to the world), so every query must filter here.</summary>
        private bool IsPresent(IFightable fighter) =>
            fighter.IsAlive && !_fledIds.Contains(fighter.InstanceId);

        private void OnPlayerEndTurnRequested(PlayerEndTurnRequestedEvent evnt)
        {
            if (!CanResolvePlayerAction()) return;
            _playerTargetTcs!.SetResult(null); // no target — the turn ends without an attack
        }

        private void OnPlayerFleeAttempt(PlayerFleeAttemptEvent evnt)
        {
            if (!CanResolvePlayerAction() || _player == null) return;

            float chance = EscapeChanceCalculator.For(
                _fighters.Where(fighter => IsPresent(fighter) && !AreAllies(_player, fighter)).OfType<IFightableNpc>());
            bool succeeded = ChanceRoll.Roll(chance, _rnd);
            _battleEventBus?.Publish(new PlayerFleeResolvedEvent(succeeded, chance));

            if (succeeded) EndBattle(new BattleOutcome(BattleResults.PlayerFled));
            // Success or failure, the turn is spent.
            // Try: on success EndBattle has already completed the target task — a second SetResult would throw.
            _playerTargetTcs!.TrySetResult(null);
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

        private void SetupTargetSelectionController()
        {
            if (_battleEventBus == null) return;
            // The LIVE list on purpose: slots created later (latecomers, summons) become
            // selectable without rewiring — RefreshHighlights re-reads it.
            _selectionController = new TargetSelectionController(_battleEventBus, this, _allSpots);
        }

        private void SetupCombatTextPresenter(IBattleEventBus battleEventBus)
        {
            _combatTextPresenter = new CombatTextPresenter();
            AddChild(_combatTextPresenter);
            _combatTextPresenter.Setup(battleEventBus, ResolveBodyAnchor);
        }

        /// <summary>Anchor for melee approach and return: the spot currently holding the entity,
        /// or the parked corpse node itself (its spot was freed for a latecomer).</summary>
        private Node2D? FindSpotFor(string instanceId)
        {
            var spot = _allSpots.FirstOrDefault(s => s.Entity?.IsSame(instanceId) == true);
            if (spot != null) return spot;
            return _parkedCorpses.GetValueOrDefault(instanceId);
        }

        /// <summary>Presentation anchor (tracker #95): numbers and VFX play where the MODEL actually
        /// stands — after a melee approach that is the opponent's face, not the home spot 700px away.
        /// Falls back to the spot/corpse lookup when the fighter's node is gone.</summary>
        private Node2D? ResolveBodyAnchor(string instanceId)
        {
            var fighter = _fighters.FirstOrDefault(entry => entry.InstanceId == instanceId);
            if (fighter is Node2D body && IsInstanceValid(body) && body.IsInsideTree()) return body;
            return FindSpotFor(instanceId);
        }

        /// <summary>
        /// A latecomer enters the ongoing battle: takes a free spot, joins the roster and the
        /// timeline (queue picks it up on the next round). Multi-sided by design — an enemy keeps
        /// its own group, a companion joins the player's.
        /// </summary>
        public bool TryJoinBattle(IFightable fighter, bool alliedWithPlayer)
        {
            if (_battleEventBus == null || _battleOutcome != null || _formation == null) return false;
            if (_fighters.Any(existing => existing.IsSame(fighter.InstanceId))) return false;
            // The slot budget counts the roster, not free spot nodes: corpses and the fled hold
            // their slots until the battle ends; summon slots live outside the budget.
            if (OccupiedBattleSlots() >= _arenaRules.MaxBattleSlots) return false;

            if (alliedWithPlayer && _player != null) _player.Group?.TryAddToGroup(fighter);
            else EnsureFractionGroup(fighter);

            var freeSpot = TakeFreeSpot();
            freeSpot.Position = s_formationCenter + _formation.ReserveSlot(FormationGroupKey(fighter));

            _fighters.Add(fighter);
            freeSpot.SetEntity(fighter);
            freeSpot.SetBattleEventBus(_battleEventBus);
            _timeline.Attach(fighter.CombatEvents);
            _reactionsDriver?.TryAttach(fighter);
            _bossStagesController?.TryAttach(fighter);
            _summonService?.TryAttach(fighter);
            // Mid-turn join: the player's current highlights were built before the newcomer
            // existed — without a rebuild its spot stays untargetable until the next turn.
            _selectionController?.RefreshHighlights();
            return true;
        }

        /// <summary>Slots spent from the battle budget: the whole roster (the player, corpses and
        /// the fled included) minus summons — their slots are free of charge by design.</summary>
        private int OccupiedBattleSlots() => _fighters.Count(fighter => fighter is not IFightableNpc { IsSummon: true });

        /// <summary>A free authored spot, or a fresh programmatic one — the scene pool is a seed,
        /// not the limit (CombatRules.arena owns the budget).</summary>
        private EntitySpot TakeFreeSpot()
        {
            // Corpse-freed spots (scene or programmatic) are reused first; occupied summon spots
            // never match — they hold their summon until it dies and die with it.
            var free = _allSpots.FirstOrDefault(spot => spot != _playerSpot && !spot.HasEntityInit());
            return free ?? CreateDynamicSpot();
        }

        private EntitySpot CreateDynamicSpot()
        {
            var spot = EntitySpot.Initialize().Instantiate<EntitySpot>();
            AddChild(spot);
            _allSpots.Add(spot);
            return spot;
        }

        /// <summary>Formation clusters are keyed by group; a groupless non-NPC fighter is its own cluster.</summary>
        private static object FormationGroupKey(IFightable fighter) => (object?)fighter.Group ?? fighter;

        /// <summary>Groupless NPCs of one faction join a shared battle group — kin must not read
        /// as enemies to targeting. World groups (spawned squads) are kept as they came.</summary>
        private void EnsureFractionGroup(IFightable fighter)
        {
            if (fighter.Group != null || fighter is not IFightableNpc npc) return;

            if (!_fractionGroups.TryGetValue(npc.Fraction, out var group))
            {
                // Battle-scoped semantic grouping, not a squad: capacity must never split kin
                // (slots are budgeted elsewhere, summons sit outside that budget entirely).
                group = new EntityGroup(maxMembers: int.MaxValue);
                _fractionGroups[npc.Fraction] = group;
            }

            group.TryAddToGroup(fighter);
        }

        public bool PrepareBattleArena(List<IFightable> fighters)
        {
            if (_battleEventBus == null) return false;
            if (fighters.Count + 1 > _arenaRules.MaxBattleSlots) // +1 — the player's slot
            {
                // A clean abort instead of a crash mid-setup; the context's finally
                // still publishes BattleEndEvent, so nothing is left stuck in Fight.
                Tracker.TrackError($"Battle slot budget exceeded: {fighters.Count} fighters + the player for {_arenaRules.MaxBattleSlots} slots", this);
                return false;
            }

            // Own copy: death/flee bookkeeping must never mutate the caller's participant list —
            // the context returns EVERYONE (bodies of the dead included) to the world from it.
            _fighters = [.. fighters];
            if (_player != null) _fighters.Add(_player);

            // Exports are populated at instantiation, but _Ready may not have run yet (the arena
            // enters the tree via a deferred AddChild) — seed the slot registry here, not there.
            if (_allSpots.Count == 0)
            {
                _allSpots.AddRange(_spots);
                if (_playerSpot != null) _allSpots.Add(_playerSpot);
            }

            // Groups first — the formation clusters by them: the player's side is one cluster,
            // every hostile group its own, so allies never end up in each other's backs.
            foreach (var fighter in fighters)
                EnsureFractionGroup(fighter);
            _formation = new ArenaFormation(new ArenaFormationSettings());
            _formation.PlanGroups(CollectGroupKeys(fighters));

            if (_player != null && _playerSpot != null)
                _playerSpot.Position = s_formationCenter + _formation.ReserveSlot(FormationGroupKey(_player));

            foreach (var fighter in fighters)
            {
                var spot = TakeFreeSpot();
                spot.Position = s_formationCenter + _formation.ReserveSlot(FormationGroupKey(fighter));
                spot.SetEntity(fighter);
                spot.SetBattleEventBus(_battleEventBus);
            }

            _playerSpot?.SetBattleEventBus(_battleEventBus);
            if (_camera is { Enabled: true } && _camera.IsInsideTree()) _camera.MakeCurrent();
            SetupTargetSelectionController();
            SetupCombatTextPresenter(_battleEventBus);
            StartTimelineRecording();
            SetupBattleDirector(_battleEventBus);
            SetupReactionsDriver(_battleEventBus);
            SetupBossStagesController(_battleEventBus);
            SetupSummonService();

            var fightersQueue = _queueScheduler.AddFighters(_fighters);
            _battleEventBus.Publish<BattleQueueDefinedEvent>(new(fightersQueue));
            return true;
        }

        /// <summary>Distinct group keys in encounter order, the player's side first.</summary>
        private List<object> CollectGroupKeys(List<IFightable> fighters)
        {
            List<object> keys = [];
            if (_player != null) keys.Add(FormationGroupKey(_player));
            foreach (var fighter in fighters)
            {
                var key = FormationGroupKey(fighter);
                if (!keys.Contains(key)) keys.Add(key);
            }

            return keys;
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

            // Timeline for the future flee beat/log entry; battle bus for the XP processor;
            // game bus for world-level listeners (a fleeing survivor is a guaranteed witness).
            fighter.CombatEvents.Publish(new EntityFledBattleEvent(fighter));
            _battleEventBus?.Publish(new EntityFledBattleEvent(fighter));
            _gameEventBus?.Publish(new EntityFledBattleEvent(fighter));
            CheckPlayerVictory();
            return Task.CompletedTask;
        }

        public async Task<BattleResults> RunBattleAsync()
        {
            while (_battleOutcome == null)
            {
                if (!_queueScheduler.TryGetNextFighter(out _currentFighter))
                {
                    // Refill HERE, not at the turn's end: dead/fled entries are skipped with
                    // `continue` and used to drain the round past the refill — a battle where
                    // the round ended on corpses got abandoned with live enemies standing
                    //  BY DESIGN (latecomers enter the roster mid-round but the queue only next round).
                    var nextRound = _queueScheduler.RefillIfEmpty(_fighters.Where(IsPresent).ToList());
                    if (nextRound.Count > 1)
                    {
                        _battleEventBus?.Publish(new BattleQueueDefinedEvent(nextRound));
                        continue;
                    }

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

                await _currentFighter.OnTurnEnd();

                // Turn gate: the whole turn resolved instantly above; the next fighter
                // doesn't start until the director has shown everything recorded so far.
                await WaitForPresentationAsync();
                // A dead summon's node and slot go only AFTER the killing blow was shown —
                // the beats need their anchor. No corpse remains by design.
                _summonService?.CleanupDead();
            }

            // Final gate: death and battle-ending beats must finish before the results are handled.
            await WaitForPresentationAsync();
            // ANY outcome (the player's flight included) dissolves the summons like spells:
            // they never reach the context's return-to-world list.
            _summonService?.DespawnAll();
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

        /// <summary>Combat reactions of NPCs (hidden triggered casts): one driver per battle, wired
        /// to every fighter with a reactions section; latecomers attach in TryJoinBattle.</summary>
        private void SetupReactionsDriver(IBattleEventBus battleEventBus)
        {
            if (_abilityProvider == null) return;
            _reactionsDriver?.Dispose();
            _reactionsDriver = new NpcReactionsDriver(
                this, _abilityProvider, _worldFacts, new DefaultRandomNumberGenerator(),
                () => _battleOutcome == null, battleEventBus);
            foreach (var fighter in _fighters)
                _reactionsDriver.TryAttach(fighter);
        }

        /// <summary>Boss stage transitions (the "stages" section of Npc.json): one controller per
        /// battle, wired to every staged fighter; latecomers attach in TryJoinBattle.</summary>
        private void SetupBossStagesController(IBattleEventBus battleEventBus)
        {
            _bossStagesController?.Dispose();
            _bossStagesController = new BossStagesController(
                new DefaultRandomNumberGenerator(), () => _battleOutcome == null, battleEventBus);
            foreach (var fighter in _fighters)
                _bossStagesController.TryAttach(fighter);
        }

        /// <summary>Summons (hidden summoning casts of bosses): one service per battle, listening on
        /// every fighter; latecomers attach in TryJoinBattle. Without the project wiring
        /// (npc provider + battle spawner) the battle simply has no summons.</summary>
        private void SetupSummonService()
        {
            _summonService?.Dispose();
            _summonService = null;
            if (_npcProvider == null || _summonSpawner == null) return;

            _summonService = new SummonService(this, () => _battleOutcome == null);
            foreach (var fighter in _fighters)
                _summonService.TryAttach(fighter);
        }

        /// <summary>ISummonHandler: spawn the summon beside its summoner and join it to the battle.
        /// The summon slot sits OUTSIDE the battle-slot budget by design.</summary>
        IFightableNpc? ISummonHandler.SpawnSummon(IFightable summoner, string npcId, float statShare)
        {
            if (_battleEventBus == null || _battleOutcome != null || _formation == null) return null;
            if (_npcProvider == null || _summonSpawner == null) return null;
            if (FindSpotFor(summoner.InstanceId) is not EntitySpot summonerSpot) return null;

            Core.Data.NpcData.NpcDefinition definition;
            try
            {
                definition = _npcProvider.CreateDefinition(npcId);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Summon npc '{npcId}' cannot be created", e, this);
                return null;
            }

            var spot = CreateDynamicSpot();
            spot.Position = s_formationCenter + _formation.ReserveSummonSlot(summoner.InstanceId, summonerSpot.Position - s_formationCenter);

            var summon = _summonSpawner.Spawn(definition, this, spot.GlobalPosition);
            if (summon == null)
            {
                _allSpots.Remove(spot);
                spot.QueueFree();
                return null;
            }

            InheritSummonerParameters(summon, summoner, statShare);
            JoinAsSummon(summon, summoner, spot);
            return summon;
        }

        /// <summary>ISummonHandler: the summon leaves the field for good — slot destroyed, node freed,
        /// nothing returns to the world. Called after the death beat was shown, or at battle end.</summary>
        void ISummonHandler.RemoveSummon(IFightableNpc summon)
        {
            summon.Group?.RemoveFromGroup(summon);
            _fighters.RemoveAll(fighter => fighter.IsSame(summon.InstanceId));

            if (_summonSpots.Remove(summon.InstanceId, out var spot))
            {
                spot.RemoveEntityFromSpot();
                spot.RemoveBattleEventBus();
                _allSpots.Remove(spot);
                spot.QueueFree();
            }

            // A freed body must not hear the later BattleEndEvent — its handler touches the native side.
            summon.RemoveBattleEventBus();
            _summonSpawner?.Despawn(summon);
            _selectionController?.RefreshHighlights();
        }

        /// <summary>The wolves are a shadow of their master: every parameter = the summoner's
        /// CURRENT value × share (cast-time snapshot), vitals start full.</summary>
        private static void InheritSummonerParameters(IFightableNpc summon, IFightable summoner, float statShare)
        {
            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
                summon.Parameters.SetBaseValueForParameter(parameter, summoner.Parameters.GetValueForParameter(parameter) * statShare);

            summon.CurrentHealth = summon.Parameters.MaxHealth;
            summon.CurrentMana = summon.Parameters.MaxMana;
            summon.CurrentBarrier = summon.Parameters.MaxBarrier;
        }

        private void JoinAsSummon(IFightableNpc summon, IFightable summoner, EntitySpot spot)
        {
            // The summoner's EXACT group, capacity notwithstanding: a groupless wolf would read
            // as everyone's enemy — its own master included.
            EnsureGroup(summoner);
            summoner.Group!.ForceAddToGroup(summon);

            _fighters.Add(summon);
            _summonSpots[summon.InstanceId] = spot;
            spot.SetEntity(summon); // SetEntity claims the node via deferred AddChild
            spot.SetBattleEventBus(_battleEventBus!);
            if (summon is Node node) node.GetParent()?.RemoveChild(node); // spawner's parent releases it before the deferred claim runs

            summon.SetupBattleEventBus(_battleEventBus!);
            summon.IsFighting = true;
            _timeline.Attach(summon.CombatEvents);
            _reactionsDriver?.TryAttach(summon);
            _summonService?.TryAttach(summon);
            // The queue picks the wolf up with the next round's refill — same as any latecomer.
            _selectionController?.RefreshHighlights();
            _battleEventBus!.Publish(new SummonSpawnedEvent(summon, summoner));
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
            // Body anchor (tracker #95): casts/impacts/travel follow the models, not the home spots.
            presenter.Setup(ResolveBodyAnchor, _visualLibrary);
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

        /// <summary>Playback speed for an external shutdown: the remaining beats flash by instead
        /// of holding the quit for seconds.</summary>
        private const float AbortFastForwardSpeed = 8f;

        /// <summary>External shutdown (app quit, tracker #66/#130): locks the outcome so the round
        /// loop exits at its next gate and fast-forwards the remaining presentation. The battle then
        /// finishes through its NORMAL path — flags, BattleEndEvent, teardown — the caller only has
        /// to await the loop's completion before pulling the tree down.</summary>
        public void AbortBattle()
        {
            _battleEventBus?.Publish(new PlaybackSpeedChangedEvent(AbortFastForwardSpeed));
            EndBattle(new BattleOutcome(BattleResults.BattleAbandoned));
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
            if (obj.Entity is IFightableNpc { IsSummon: true })
            {
                // No corpse, no parked spot: the summon service tears the slot down after the
                // death beat has been shown (the battle loop drives the cleanup).
                _summonService?.OnSummonDied(obj.Entity);
                CheckPlayerVictory();
                return;
            }

            FreeSpotOf(obj.Entity);
            CheckPlayerVictory();
        }

        /// <summary>Death frees the spot for latecomers: the corpse reparents to the arena at the
        /// same position (still lying on the field, returned to the world by the context at the end).</summary>
        private void FreeSpotOf(IFightable entity)
        {
            var spot = _allSpots.FirstOrDefault(s => s.Entity?.IsSame(entity.InstanceId) == true);
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
