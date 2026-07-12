namespace Battle.Internal
{
    using System;
    using Core;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Services;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Source;
    using Source.UIElements;
    using GameServiceProvider = Services.GameServiceProvider;

    public partial class Main : Node2D
    {
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IUiElementsManager? _uiElementProvider;
        private IGameEventBus? _gameEventBus;
        private BattleContext? _activeContext;
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
                // TODO:
                //  необходимо создавать NotificationPopup через uiElementsmanager.
              //  _provider.GetService<NotificationService>().Setup(_layerManager, CreateNotificationPopup);
            }
            // internal stuff
            _uiElementProvider.RegisterPopupFactory(typeof(IKeywordTooltipPopup), () => KeywordTooltipPopup.Initialize().Instantiate<KeywordTooltipPopup>());

            // Resolve once so it subscribes to mastery/player changes and auto-learns unlocked abilities.
            _provider.GetService<IAbilityUnlockService>();
            var mastery = _provider.GetService<IMartialArtMastery>();
            mastery.AddExperience(500000);
            _gameEventBus = _provider.GetService<IGameEventBus>();
            _gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus.Subscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
        }

        public override void _ExitTree()
        {
            // The game bus outlives the scene (save-load reloads it): stale subscriptions would
            // keep calling handlers on a freed node.
            _gameEventBus?.Unsubscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus?.Unsubscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
        }

        /// <summary>Refusal (no free spot / battle over) simply leaves the NPC in the world.
        /// Deferred: the request comes from a physics callback, and joining reparents a physics
        /// body (world → spot) — doing that mid-flush silently fails and strands the node in the world.</summary>
        private void OnBattleJoinRequest(BattleJoinRequestEvent evnt) =>
            // Statement lambda on purpose: an expression lambda would return bool? and Callable
            // has no Variant conversion for Nullable — crashes at invoke time.
            Callable.From(() => { _activeContext?.TryJoinBattle(evnt.Fighter, evnt.AlliedWithPlayer); }).CallDeferred();


        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Keycode: Key.N, Pressed: true })
                _uiElementProvider?.ToggleWindow(typeof(MartialArtMasteryWindow));
            if (@event is InputEventKey { Keycode: Key.R, Pressed: true })
            {
                // var item = _provider.GetService<IItemGameDataFactory>().CreateEquipItem("Weapon_Bloodthirsty");
                // GD.Print($"{item.DisplayName}");
            }
        }
        private async void OnBattleInitialized(BattleInitializedEvent evnt)
        {
            BattleContext? context = null;
            BattleSiteMarker? battleSite = null;
            try
            {
                ArgumentNullException.ThrowIfNull(_uiElementProvider);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                battleSite = CreateBattleSiteMarker(evnt.Player);
                context = new BattleContext(evnt.Player, evnt.Entities, _mainWorld, _provider, this);
                _activeContext = context;
                await ToSignal(GetTree(), "process_frame");
                var result = await context.RunBattleAsync();
                _gameEventBus?.Publish(new BattleEndEvent(result));
            }
            catch (Exception es)
            {
                GD.Print($"Exception: {es.Message}, Stack Trace: {es.StackTrace}");
                Tracker.TrackException("Failed to instantiate BattleArena", es, this);
            }
            finally
            {
                // Dispose must survive a crashed battle: it returns the fighters to the world
                // and frees the arena — otherwise the NPCs vanish with the leaked arena node.
                _activeContext = null;
                battleSite?.QueueFree();
                context?.Dispose();
            }
        }

        /// <summary>The world-side presence of the battle: the player's node leaves the world for the
        /// arena, the marker stays at the engagement point so latecomers have something to reach.</summary>
        private BattleSiteMarker? CreateBattleSiteMarker(IFightable player)
        {
            if (_mainWorld == null || _gameEventBus == null || player is not Node2D playerNode) return null;

            var marker = new BattleSiteMarker();
            _mainWorld.AddChild(marker);
            marker.GlobalPosition = playerNode.GlobalPosition;
            marker.Setup(_gameEventBus);
            return marker;
        }
    }
}
