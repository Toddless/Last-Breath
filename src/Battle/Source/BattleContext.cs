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
    using UIElements;

    internal class BattleContext : IBattleContext
    {
        private readonly BattleExperienceProcessor _battleExperienceProcessor;
        private readonly IUiElementsManager _uiElementManager;
        private readonly IBattleEventBus _localBus;
        private readonly BattleArena _battleArena;
        private readonly List<IFightable> _entities;
        private readonly Node2D _mainWorld;
        private readonly IFightable _player;
        private BattleHud? _battleHud;
        private bool _battleRunning;

        public BattleContext(IFightable player, List<IFightable> entities, Node2D mainWorld, IGameServiceProvider provider, Node2D parent)
        {
            _uiElementManager = provider.GetService<IUiElementsManager>();
            _player = player;
            _entities = entities;
            _mainWorld = mainWorld;
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            _localBus = new BattleEventBus();
            _battleArena.SetupEventBus(_localBus);
            _battleArena.InjectServices(provider);
            _battleExperienceProcessor = new BattleExperienceProcessor(_localBus, provider, player);
            // Summons enter mid-battle from the arena's side; the context only mirrors the
            // latecomer path's HUD bars (the summon never reaches the return-to-world list).
            _localBus.Subscribe<SummonSpawnedEvent>(OnSummonSpawned);
            parent.CallDeferred(Node.MethodName.AddChild, _battleArena);
            _player.SetupBattleEventBus(_localBus);
            // The context owns the fighting status: the flag goes up synchronously inside the
            // BattleInitializedEvent publish, so a second contact in the same frame can't start
            // a second battle (BaseNpc.OnBodyEnter checks it).
            _player.IsFighting = true;
            RemoveParticipantFromWorld();
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
                _player.IsFighting = false;
                foreach (var entity in _entities)
                    entity.IsFighting = false; // groups set it on Attacked, nobody reset it: survivors' world brains froze
                // The battle-bus subscribers (Player/NPC state machines, ability buttons, presenters)
                // exit their fight state here; Main separately publishes the game-bus copy for loot.
                _localBus.Publish(new BattleEndEvent(results));
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
            if (!_battleArena.TryJoinBattle(fighter, alliedWithPlayer)) return false;

            fighter.SetupBattleEventBus(_localBus);
            fighter.IsFighting = true;
            if (fighter is Node2D node)
                node.GetParent()?.RemoveChild(node); // the spot already claimed the node via deferred AddChild

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
            ReturnParticipantsToWorld();
            _battleExperienceProcessor.Dispose();
            _localBus.Unsubscribe<SummonSpawnedEvent>(OnSummonSpawned);
            _battleArena.QueueFree();
            _localBus.Dispose();
        }

        private void OnSummonSpawned(SummonSpawnedEvent evt) =>
            _battleHud?.CreateEntityBarsWithInitialValues(evt.Summon);

        private void ReturnParticipantsToWorld()
        {
            try
            {
                _battleArena.RemoveEntitiesFromArenaSpots();
                _battleArena.RemovePlayerFromArenaSpot();
                // The context's list is the source of truth for the return: the arena works on its
                // own copy, so the dead and the fled are still here — bodies must lie in the world.
                ReturnToWorld(_player);
                foreach (var entity in _entities)
                    ReturnToWorld(entity);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to return participants to the world", ex, this);
                GD.Print($"{ex.Message}, {ex.StackTrace}");
            }
        }

        private void ReturnToWorld(IFightable entity)
        {
            if (entity is not Node2D asNode) return;
            // Legacy NPCs without body rules free themselves on battle end — don't resurrect the node.
            if (!GodotObject.IsInstanceValid(asNode) || asNode.IsQueuedForDeletion()) return;
            // Parked corpses live as arena children (their spot was freed for a latecomer).
            asNode.GetParent()?.RemoveChild(asNode);
            _mainWorld.AddChild(asNode);
        }

        private void RemoveParticipantFromWorld()
        {
            if (_player is Node2D node)
                _mainWorld.CallDeferred(Node.MethodName.RemoveChild, node);

            foreach (var entity in _entities)
            {
                entity.SetupBattleEventBus(_localBus);
                entity.IsFighting = true; // solo NPCs never got the flag (only EntityGroup set it)
                if (entity is Node2D n)
                    _mainWorld.CallDeferred(Node.MethodName.RemoveChild, n);
            }
        }
    }
}
