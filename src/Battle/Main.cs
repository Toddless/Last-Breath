namespace Battle
{
    using Godot;
    using System;
    using Services;
    using Utilities;
    using System.Linq;
    using Source.GameEvents;
    using Core.Interfaces.Data;

    public partial class Main : Node2D
    {
        [Export] private MainWorld? _mainWorld;
        [Export] private UiLayerManager? _layerManager;

        public override void _Ready()
        {
            var provider = GameServiceProvider.Instance.GetService<IUIElementProvider>();
            if (_layerManager != null) provider.Subscribe(_layerManager);
            GameEventBus.Instance.Subscribe<InitializeFightGameEvent>(OnFightInitialized);
        }

        private async void OnFightInitialized(InitializeFightGameEvent obj)
        {
            try
            {
                var player = obj.Player;
                var node = player as Node2D;
                if (node != null)
                    _mainWorld?.CallDeferred(Node.MethodName.RemoveChild, node);
                var battleArena = BattleArena.Initialize().Instantiate<BattleArena>();
                battleArena.SetPlayer(player);

                foreach (var asNode in obj.Entities.Select(objEntity => objEntity as Node2D))
                {
                    if (asNode != null)
                        _mainWorld?.CallDeferred(Node.MethodName.RemoveChild, asNode);
                }

                CallDeferred(Node.MethodName.AddChild, battleArena);
                await ToSignal(GetTree(), "process_frame");
                await battleArena.StartFight(obj.Entities);

                if (player.IsAlive)
                {
                    battleArena.CallDeferred(Node.MethodName.RemoveChild, node);
                    _mainWorld?.CallDeferred(Node.MethodName.AddChild, node);
                }

                CallDeferred(Node.MethodName.RemoveChild, battleArena);
            }
            catch (Exception es)
            {
                Tracker.TrackException("Failed to instantiate BattleArena", es, this);
            }
        }
    }
}
