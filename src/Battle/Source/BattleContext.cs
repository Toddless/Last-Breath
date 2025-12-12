namespace Battle.Source
{
    using Godot;
    using Services;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    internal class BattleContext : IBattleContext
    {
        private readonly IGameEventBus _localBus = new GameEventBus();
        private readonly IEntity _player;
        private readonly List<IEntity> _entities;
        private readonly MainWorld _mainWorld;
        private readonly BattleHud _battleHud;
        private readonly BattleArena _battleArena;

        public BattleContext(IEntity player, List<IEntity> entities, MainWorld mainWorld, IGameServiceProvider provider, Node2D parent)
        {
            _player = player;
            _entities = entities;
            _mainWorld = mainWorld;
            var uiProvider = provider.GetService<IUIElementProvider>();
            _battleHud = uiProvider.CreateAndShowMainElement<BattleHud>();
            _battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
            parent.CallDeferred(Node.MethodName.AddChild, _battleArena);

            _battleArena.SetupEventBus(_localBus);
            _battleHud.SetupEventBus(_localBus);
            _battleHud.SetInitialValues(_player.Parameters.MaxHealth, _player.Parameters.Mana, _player.CurrentHealth, _player.CurrentMana);
            _player.SetupEventBus(_localBus);

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
            _battleHud.QueueFree();
        }

        private void ReturnParticipantsToWorld()
        {
            if (!_player.IsAlive || _player is not Node2D node)
                return;

            _battleArena.CallDeferred(Node.MethodName.RemoveChild, node);
            _mainWorld.CallDeferred(Node.MethodName.AddChild, node);
        }

        private void RemoveParticipantFromWorld()
        {
            if (_player is Node2D node)
                _mainWorld.CallDeferred(Node.MethodName.RemoveChild, node);

            foreach (var entity in _entities)
            {
                entity.SetupEventBus(_localBus);
                if (entity is Node2D n)
                    _mainWorld.CallDeferred(Node.MethodName.RemoveChild, n);
            }
        }
    }
}
