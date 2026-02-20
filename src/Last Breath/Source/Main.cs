namespace LastBreath.Source
{
    using Godot;
    using System;
    using Services;
    using Utilities;
    using Core.Data;
    using Battle.Source;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;

    public partial class Main : Node2D
    {
        private const string UID = "uid://drgs10sgp405d";
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IUiElementProvider? _uiElementProvider;
        private IGameEventBus? _gameEventBus;
        [Export] private MainWorld? _mainWorld;
        [Export] private Node? _uiLayerManager;

        public override void _Ready()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiLayerManager);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                _uiElementProvider = _provider.GetService<IUiElementProvider>();
                if (_uiLayerManager != null) _uiElementProvider.Subscribe(_uiLayerManager);
                _gameEventBus = _provider.GetService<IGameEventBus>();
                _gameEventBus.Subscribe<BattleStartEvent>(OnBattleInitialized);

            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to load main.", ex, this);
            }
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

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
