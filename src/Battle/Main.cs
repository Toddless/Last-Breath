namespace Battle
{
    using Godot;
    using Source;
    using System;
    using Core.Data;
    using Services;
    using Utilities;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;

    public partial class Main : Node2D
    {
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IUiElementProvider? _uiElementProvider;
        private IGameEventBus? _gameEventBus;
        [Export] private MainWorld? _mainWorld;
        [Export] private UiLayerManager? _layerManager;

        public override void _Ready()
        {
            _uiElementProvider = _provider.GetService<IUiElementProvider>();
            if (_layerManager != null) _uiElementProvider.Subscribe(_layerManager);
            _gameEventBus = _provider.GetService<IGameEventBus>();
            _gameEventBus.Subscribe<BattleStartEvent>(OnBattleInitialized);
        }

        private async void OnBattleInitialized(BattleStartEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiElementProvider);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                var context = new BattleContext(evnt.Player, evnt.Entities, _mainWorld, _provider, this);
                await ToSignal(GetTree(), "process_frame");
                await context.RunBattleAsync();
                context.Dispose();
            }
            catch (Exception es)
            {
                GD.Print($"Exception: {es.Message}, Stack Trace: {es.StackTrace}");
                Tracker.TrackException("Failed to instantiate BattleArena", es, this);
            }
        }
    }
}
