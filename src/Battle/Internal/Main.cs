namespace Battle.Internal
{
    using System;
    using Services;
    using Source;
    using Core.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Godot;
    using Utilities;

    public partial class Main : Node2D
    {
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IUiElementsManager? _uiElementProvider;
        private IGameEventBus? _gameEventBus;
        [Export] private MainWorld? _mainWorld;
        [Export] private UiLayerManager? _layerManager;

        public override void _Ready()
        {
            _uiElementProvider = _provider.GetService<IUiElementsManager>();
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
