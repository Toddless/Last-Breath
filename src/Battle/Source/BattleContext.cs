namespace Battle.Source
{
    using Godot;
    using System;
    using Core.Data;
    using UIElements;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;

    internal class BattleContext : IBattleContext
    {
        private readonly IBattleEventBus _localBus;
        private readonly IUiElementProvider _uiElementProvider;
        private readonly BattleArena _battleArena;
        private readonly List<IEntity> _entities;
        private readonly Node2D _mainWorld;
        private readonly IEntity _player;

        public BattleContext(IEntity player, List<IEntity> entities, Node2D mainWorld, IGameServiceProvider provider, Node2D parent)
        {
            _uiElementProvider = provider.GetService<IUiElementProvider>();
            _player = player;
            _entities = entities;
            _mainWorld = mainWorld;
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            _localBus = new BattleEventBus();
            _battleArena.SetupEventBus(_localBus);
            parent.CallDeferred(Node.MethodName.AddChild, _battleArena);
            _player.SetupBattleEventBus(_localBus);
            RemoveParticipantFromWorld();
        }

        public async Task RunBattleAsync()
        {
            var battleHud = await _uiElementProvider.CreateAndShowMainElement<BattleHud>();
            battleHud.SetupEventBus(_localBus);
            battleHud.SetPlayerInitialValues(_player.Parameters.MaxHealth, _player.Parameters.MaxMana, _player.CurrentHealth, _player.CurrentMana);
            foreach (IEntity entity in _entities)
                battleHud.CreateEntityBarsWithInitialValues(entity.InstanceId, entity.Parameters.MaxHealth, entity.Parameters.MaxMana, entity.CurrentHealth, entity.CurrentMana);

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
                    _mainWorld.AddChild(asNode);
                }

                _localBus.Publish<BattleEndEvent>(new());
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
