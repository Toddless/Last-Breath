namespace Battle.Source
{
    using System;
    using Godot;
    using Services;
    using UIElements;
    using Core.Interfaces.Data;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    internal class BattleContext : IBattleContext
    {
        private readonly IBattleEventBus _localBus = new BattleEventBus();
        private readonly IUIElementProvider _uiElementProvider;
        private readonly BattleArena _battleArena;
        private readonly List<IEntity> _entities;
        private readonly MainWorld _mainWorld;
        private readonly IEntity _player;

        public BattleContext(IEntity player, List<IEntity> entities, MainWorld mainWorld, IGameServiceProvider provider, Node2D parent)
        {
            _uiElementProvider = provider.GetService<IUIElementProvider>();
            var battleHud = _uiElementProvider.CreateAndShowMainElement<BattleHud>();
            _player = player;
            _entities = entities;
            _mainWorld = mainWorld;
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            _battleArena.SetupEventBus(_localBus);

            parent.CallDeferred(Node.MethodName.AddChild, _battleArena);
            battleHud.SetupEventBus(_localBus);
            battleHud.SetPlayerInitialValues(_player.Parameters.MaxHealth, _player.Parameters.Mana, _player.CurrentHealth, _player.CurrentMana);
            foreach (IEntity entity in _entities)
                battleHud.CreateEntityBarsWithInitialValues(entity.InstanceId, entity.Parameters.MaxHealth, entity.Parameters.Mana, entity.CurrentHealth, entity.CurrentMana);

            _player.SetupBattleEventBus(_localBus);
            RemoveParticipantFromWorld();
        }

        public async Task RunBattleAsync()
        {
            _battleArena.SetPlayer(_player);
            await _battleArena.StartFight(_entities);
        }

        public void Dispose()
        {
            ReturnParticipantsToWorld();
            _battleArena.QueueFree();
            _localBus.Dispose();
            _uiElementProvider.HideMainElement<BattleHud>();
        }

        private void ReturnParticipantsToWorld()
        {
            try
            {
                _battleArena.RemoveAliveEntitiesFromArena();
                _battleArena.RemovePlayerFromArena();
                foreach (var entity in _entities)
                {
                    if (!entity.IsAlive || entity is not Node2D asNode) continue;
                    _mainWorld.CallDeferred(Node.MethodName.AddChild, asNode);
                }

                _localBus.Publish<BattleEndGameEvent>(new());
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
                if (entity is Node2D n)
                    _mainWorld.CallDeferred(Node.MethodName.RemoveChild, n);
            }
        }
    }
}
