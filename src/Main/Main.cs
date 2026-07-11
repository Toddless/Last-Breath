namespace LastBreath
{
    using System;
    using Battle.Source;
    using Battle.Source.UIElements;
    using Core;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Services;
    using Core.Views.UI;
    using Godot;
    using LootGeneration.Source;
    using Services;
    using UI;
    using World;
    using GameOverWindow = UI.GameOverWindow;
    using GameServiceProvider = Services.GameServiceProvider;
    using NotificationPopup = UI.NotificationPopup;

    public partial class Main : Node2D
    {
        private const string UID = "uid://cvru2meygw8jj";
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IUiElementsManager? _uiElementProvider;
        private IGameEventBus? _gameEventBus;
        private BattleContext? _activeContext;
        [Export] private MainWorld? _mainWorld;
        [Export] private UiLayerManager? _layerManager;

        public override void _Ready()
        {
            _uiElementProvider = _provider.GetService<IUiElementsManager>();
            _uiElementProvider.Subscribe(_layerManager);
            // Wire the notification pipeline: the layer manager is the sink, the factory builds the
            // popup on demand. Returns null until Todd's NotificationPopup.tscn UID is set — the
            // service drops the message rather than crashing.
            _provider.GetService<NotificationService>().Setup(_layerManager, CreateNotificationPopup);

            // Resolve once so it subscribes to mastery/player changes and auto-learns unlocked abilities.
            _provider.GetService<IAbilityUnlockService>();
            // Spoils of battle land on this world's floor (cleared on exit — the node dies with the scene).
            _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(_mainWorld);
            _gameEventBus = _provider.GetService<IGameEventBus>();
            _gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus.Subscribe<PlayerFinalDeathEvent>(OnPlayerFinalDeath);
            _gameEventBus.Subscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
            _uiElementProvider.ChangeHud(typeof(PlayerHud));
        }

        public override void _ExitTree()
        {
            // The game bus outlives the scene (save-load reloads it): stale subscriptions would
            // keep calling handlers on a freed node.
            _gameEventBus?.Unsubscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus?.Unsubscribe<PlayerFinalDeathEvent>(OnPlayerFinalDeath);
            _gameEventBus?.Unsubscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
            _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(null);
        }

        /// <summary>Refusal (no free spot / battle over) simply leaves the NPC in the world.
        /// Deferred: the request comes from a physics callback, and joining reparents a physics
        /// body (world → spot) — doing that mid-flush silently fails and strands the node in the world.</summary>
        private void OnBattleJoinRequest(BattleJoinRequestEvent evnt) =>
            // Statement lambda on purpose: an expression lambda would return bool? and Callable
            // has no Variant conversion for Nullable — crashes at invoke time.
            Callable.From(() => { _activeContext?.TryJoinBattle(evnt.Fighter, evnt.AlliedWithPlayer); }).CallDeferred();

        private void OnPlayerFinalDeath(PlayerFinalDeathEvent evnt) =>
            _uiElementProvider?.OpenWindow(typeof(GameOverWindow));

        public override void _Input(InputEvent @event)
        {
            // TODO:
            // Обработка инпута (горячие клавиши окон). Выносим из мейна? Или оствляем здесь?
            if (@event is InputEventKey { Keycode: Key.N, Pressed: true })
                _uiElementProvider?.ToggleWindow(typeof(MartialArtMasteryWindow));
        }

        private static Control? CreateNotificationPopup()
        {
            var scene = NotificationPopup.Initialize();
            return scene.Instantiate<NotificationPopup>();
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
                _uiElementProvider?.ChangeHud(typeof(PlayerHud));
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

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
