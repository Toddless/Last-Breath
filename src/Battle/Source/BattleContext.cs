namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
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

        public BattleContext(IFightable player, List<IFightable> entities, Node2D mainWorld, IGameServiceProvider provider, Node2D parent)
        {
            _uiElementManager = provider.GetService<IUiElementsManager>();
            _player = player;
            _entities = entities;
            _mainWorld = mainWorld;
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            _localBus = new BattleEventBus();
            _battleArena.SetupEventBus(_localBus);
            _battleExperienceProcessor = new BattleExperienceProcessor(_localBus, provider);
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
                var battleHud = (BattleHud)_uiElementManager.ChangeHud(typeof(BattleHud));
                await battleHud.SetupEventBus(_localBus);
                battleHud.SetPlayerInitialValues(_player.Parameters.MaxHealth, _player.Parameters.MaxMana, _player.CurrentHealth, _player.CurrentMana);
                battleHud.SetAbilityBook(_player.AbilityBook);
                foreach (IFightable entity in _entities)
                    battleHud.CreateEntityBarsWithInitialValues(entity.InstanceId, entity.Parameters.MaxHealth, entity.Parameters.MaxMana, entity.CurrentHealth, entity.CurrentMana);

                _battleArena.SetPlayer(_player);
                if (!_battleArena.PrepareBattleArena(_entities)) return results;
                results = await _battleArena.RunBattleAsync();
                _battleExperienceProcessor.CompleteBattle(results);
                return results;
            }
            finally
            {
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

        public void Dispose()
        {
            ReturnParticipantsToWorld();
            _battleExperienceProcessor.Dispose();
            _battleArena.QueueFree();
            _localBus.Dispose();
        }

        private void ReturnParticipantsToWorld()
        {
            try
            {
                _battleArena.RemoveAliveEntitiesFromArena();
                _battleArena.RemovePlayerFromArena();
                foreach (var entity in _entities)
                {
                    if (entity is not Node2D asNode) continue;
                    // Dead NPCs return too: the body stays in the world (resurrection/burning
                    // lifecycle). Legacy NPCs without body rules free themselves on battle end.
                    if (!GodotObject.IsInstanceValid(asNode) || asNode.IsQueuedForDeletion()) continue;
                    _mainWorld.AddChild(asNode);
                }
            }
            catch (Exception ex)
            {
                GD.Print($"{ex.Message}, {ex.StackTrace}");
            }
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
