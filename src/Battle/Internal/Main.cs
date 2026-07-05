namespace Battle.Internal
{
    using System;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Core.Services;
    using Godot;
    using Source;
    using Source.UIElements;
    using Utilities;
    using GameServiceProvider = Services.GameServiceProvider;
    using NotificationService = Core.Services.NotificationService;

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
            if (_layerManager != null)
            {
                _uiElementProvider.Subscribe(_layerManager);
                // Wire the notification pipeline: the layer manager is the sink, the factory builds the
                // popup on demand. Returns null until Todd's NotificationPopup.tscn UID is set — the
                // service drops the message rather than crashing.
                _provider.GetService<NotificationService>().Setup(_layerManager, CreateNotificationPopup);
            }

            // Resolve once so it subscribes to mastery/player changes and auto-learns unlocked abilities.
            _provider.GetService<IAbilityUnlockService>();
            var mastery = _provider.GetService<IMartialArtMastery>();
            mastery.AddExperience(500000);
            _gameEventBus = _provider.GetService<IGameEventBus>();
            _gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Keycode: Key.N, Pressed: true })
                _uiElementProvider?.OpenWindow(typeof(MartialArtMasteryWindow));
        }

        private static Control? CreateNotificationPopup()
        {
            var scene = NotificationPopup.Initialize();
            return scene?.Instantiate<NotificationPopup>();
        }

        private async void OnBattleInitialized(BattleInitializedEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiElementProvider);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                var context = new BattleContext(evnt.Player, evnt.Entities, _mainWorld, _provider, this);
                await ToSignal(GetTree(), "process_frame");
                var result = await context.RunBattleAsync();
                _gameEventBus?.Publish(new BattleEndEvent(result));
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
