namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Views.UI;
    using Godot;
    using Core.World.Spaces;
    using UIElements;

    internal class BattleContext : IBattleContext, IDisposable
    {
        private readonly BattleExperienceProcessor _battleExperienceProcessor;
        private readonly IUiElementsManager _uiElementManager;
        private readonly IBattleEventBus _localBus;
        private readonly BattleArena _battleArena;
        private readonly List<IFightable> _entities;
        private readonly BattlePresentation _presentation;
        private readonly Dictionary<IFightable, ParticipantPlacement> _placements = [];
        private readonly Node2D _origin;
        private readonly IFightable _player;
        private BattleHud? _battleHud;
        private bool _battleRunning;
        private bool _returned;
        private bool _disposed;
        private bool _ended;

        public Guid BattleId { get; }

        public BattleContext(IFightable player, List<IFightable> entities, Node2D origin, IGameServiceProvider provider, Node2D parent, Guid battleId = default)
        {
            BattleId = battleId == Guid.Empty ? Guid.NewGuid() : battleId;
            _uiElementManager = provider.GetService<IUiElementsManager>();
            _player = player;
            _entities = [.. entities];
            _origin = origin;
            CapturePlacement(player);
            foreach (var entity in _entities) CapturePlacement(entity);
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            _localBus = new BattleEventBus();
            _battleArena.SetupEventBus(_localBus);
            _battleArena.InjectServices(provider);
            _battleExperienceProcessor = new BattleExperienceProcessor(_localBus, provider, player);
            // Summons enter mid-battle from the arena's side; the context only mirrors the
            // latecomer path's HUD bars (the summon never reaches the return-to-world list).
            _localBus.Subscribe<SummonSpawnedEvent>(OnSummonSpawned);
            _localBus.Subscribe<SummonRemovedEvent>(OnSummonRemoved);
            _presentation = new BattlePresentation(origin, parent);
            _presentation.Viewport.AddChild(_battleArena);
            try
            {
                // Claim combat state synchronously, before another contact can start a battle.
                _player.IsFighting = true;
                _player.SetupBattleEventBus(_localBus);
                PrepareParticipants();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public async Task<BattleResults> RunBattleAsync()
        {
            var results = BattleResults.BattleAbandoned;
            try
            {
                _battleHud = (BattleHud)_uiElementManager.ChangeHud(typeof(BattleHud));
                await _battleHud.SetupEventBus(_localBus);
                _battleHud.SetPlayerInitialValues(_player);
                _battleHud.SetPlayerStance(_player.AbilityBook.CurrentStance);
                _battleHud.SetAbilityBook(_player.AbilityBook);
                foreach (IFightable entity in _entities)
                    _battleHud.CreateEntityBarsWithInitialValues(entity);

                _battleArena.SetPlayer(_player);
                if (!_battleArena.PrepareBattleArena(_entities)) return results;
                _battleRunning = true;
                results = await _battleArena.RunBattleAsync();
                _battleExperienceProcessor.CompleteBattle(results);
                return results;
            }
            finally
            {
                _battleRunning = false;
                // The end signal must be unmissable — even when the battle aborts or throws.
                // Without it the player's FSM stays in Fight forever and every next battle
                // start dies with "Fight from Fight" before the NPCs reach the arena.
                EndBattle(results);
            }
        }

        /// <summary>
        /// A latecomer joins the ongoing battle. The side is a parameter: enemies keep their own
        /// group (multi-sided fights), future companions land in the player's group. Refusal
        /// (no free spot / battle over) leaves the NPC in the world untouched.
        /// </summary>
        public bool TryJoinBattle(IFightable fighter, bool alliedWithPlayer)
        {
            if (!_battleRunning || !fighter.IsAlive || fighter.IsFighting) return false;
            if (fighter is not Node2D body || !SpatialAccess.SharesSpace(_origin, body)) return false;
            var placement = new ParticipantPlacement(body);
            if (!_battleArena.TryJoinBattle(fighter, alliedWithPlayer)) return false;

            _placements.Add(fighter, placement);
            fighter.SetupBattleEventBus(_localBus);
            fighter.IsFighting = true;

            _entities.Add(fighter); // the return-to-world list must include the latecomer
            _battleHud?.CreateEntityBarsWithInitialValues(fighter);
            return true;
        }

        /// <summary>App-quit path (tracker #66/#130): asks the arena to wind the battle down;
        /// RunBattleAsync then completes through its normal finally (flags, BattleEndEvent),
        /// and the host's battle task performs the usual teardown.</summary>
        public void Abort()
        {
            if (!_battleRunning) return;
            _battleArena.AbortBattle();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { EndBattle(BattleResults.BattleAbandoned); }
            finally
            {
                _battleExperienceProcessor.Dispose();
                _localBus.Unsubscribe<SummonSpawnedEvent>(OnSummonSpawned);
                _localBus.Unsubscribe<SummonRemovedEvent>(OnSummonRemoved);
                _battleArena.QueueFree();
                _presentation.Dispose();
                _localBus.Dispose();
            }
        }

        private void OnSummonSpawned(SummonSpawnedEvent evt) =>
            _battleHud?.CreateEntityBarsWithInitialValues(evt.Summon);

        /// <summary>The mirror of <see cref="OnSummonSpawned"/>: the body is leaving the field for
        /// good, so the bars created for it go now — while its node is still alive. The hud is
        /// checked for validity because the removal can also arrive from the arena's teardown.</summary>
        private void OnSummonRemoved(SummonRemovedEvent evt)
        {
            if (_battleHud == null || !GodotObject.IsInstanceValid(_battleHud)) return;
            _battleHud.RemoveEntityBars(evt.Summon.InstanceId);
        }

        private void EndBattle(BattleResults results)
        {
            if (_ended) return;
            _ended = true;
            _battleRunning = false;
            _player.IsFighting = false;
            foreach (var entity in _entities) entity.IsFighting = false;
            try { _localBus.Publish(new BattleEndEvent(results)); }
            finally { ReturnParticipantsToWorld(); }
        }

        private void ReturnParticipantsToWorld()
        {
            if (_returned) return;
            _returned = true;
            try
            {
                _battleArena.RemoveEntitiesFromArenaSpots();
                _battleArena.RemovePlayerFromArenaSpot();
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to return participants to the world", ex, this);
                GD.Print($"{ex.Message}, {ex.StackTrace}");
            }
            finally
            {
                ReturnToWorld(_player);
                foreach (var entity in _entities) ReturnToWorld(entity);
            }
        }

        private void ReturnToWorld(IFightable entity)
        {
            try
            {
                if (_placements.TryGetValue(entity, out var placement)) placement.Restore();
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to restore a battle participant", exception, this);
            }
        }

        private void CapturePlacement(IFightable entity)
        {
            if (entity is not Node2D body || !SpatialAccess.SharesSpace(_origin, body))
                throw new InvalidOperationException("Battle participants must belong to the origin space.");
            if (!_placements.TryAdd(entity, new ParticipantPlacement(body)))
                throw new InvalidOperationException("A participant cannot occupy two battle slots.");
        }

        private void PrepareParticipants()
        {
            foreach (var entity in _entities)
            {
                entity.SetupBattleEventBus(_localBus);
                entity.IsFighting = true; // solo NPCs never got the flag (only EntityGroup set it)
            }
        }
    }
}
