namespace LastBreath
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Source;
    using Battle.Source.UIElements;
    using Battle.Source.UIElements.PassiveWheel;
    using Core;
    using Core.Battle.Abilities;
    using Core.Constants;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.Save;
    using Core.Services;
    using Core.Views.UI;
    using Godot;
    using Inventory;
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
        [Export] private bool _addTestItems = false;

        public override void _Ready()
        {
            // Closing the app mid-battle used to tear the tree down under the battle's async
            // continuations — native AV on exit (tracker #66/#130). The window X now routes
            // through QuitGracefully; _ExitTree restores stock behavior for the menu scene.
            GetTree().AutoAcceptQuit = false;
            _uiElementProvider = _provider.GetService<IUiElementsManager>();
            _uiElementProvider.Subscribe(_layerManager);
            // Wire the notification pipeline: the layer manager is the sink, the factory builds the
            // popup on demand. Returns null until Todd's NotificationPopup.tscn UID is set — the
            // service drops the message rather than crashing.
            _provider.GetService<NotificationService>().Setup(_layerManager, CreateNotificationPopup);

            // Resolve once: the service catches the player's book up on every non-hidden ability it
            // does not hold yet, and keeps watching the accessor for a new player.
            _provider.GetService<IAbilityUnlockService>();
            // Spoils of battle land on this world's floor (cleared on exit — the node dies with the scene).
            _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(_mainWorld);
            _gameEventBus = _provider.GetService<IGameEventBus>();
            _gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus.Subscribe<PlayerFinalDeathEvent>(OnPlayerFinalDeath);
            _gameEventBus.Subscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
            _uiElementProvider.ChangeHud(typeof(PlayerHud));
            // A pending load owns the bag (same policy as the spawn points own the population): the
            // restore fills it from the file, and seeding it first only mints what is about to be
            // thrown away — and toasts a full bag over the world the player is loading into.
            bool loadPending = _provider.GetService<ISaveGameService>()?.HasPendingLoad == true;
            if (_addTestItems && !loadPending) AddTestItems();
        }

        private void AddTestItems()
        {
            var inventory = _provider.GetService<IInventory>();
            var itemCreation = _provider.GetService<IItemCreationService>();
            var augmentCatalog = _provider.GetService<IAbilityAugmentCatalog>();
            var augments = augmentCatalog.All.ToList();
            var itemMinter = _provider.GetService<IItemMinter>();
            float chance = 0.3f;
            float multiplier = 1f;
            inventory.TryAddItem(itemCreation.CreateItem("Gloves_Hunters_Dream", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Boots_Hunters_Dream", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Body_Hunters_Dream", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Helmet_Hunters_Dream", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Simple_Sword", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Simple_Dagger", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Simple_Axe", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Bloodthirsty", [], Rarity.Unique, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Simple_Sword", [], Rarity.Uncommon, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Amulet_Recovery_Source", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Belt_Leather", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Ring_of_Assassin", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Ring_Archmage_Signet", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Body_Mysterious_Bastion", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Righteous_Wrath", [], Rarity.Legendary, chance, multiplier));
            inventory.TryAddItem(itemCreation.CreateItem("Weapon_Silent_Fury", [], Rarity.Legendary, chance, multiplier));
            var resources = _provider.GetService<IItemDataProvider>().GetAllResources();
            foreach (IItem item in resources.ToList())
                inventory.TryAddItem(item.Copy<IItem>(), 999);
            foreach (AbilityAugmentData abilityAugmentData in augments)
            {
                inventory.TryAddItem(itemMinter.MintItem(abilityAugmentData.Id));
            }
        }

        public override void _ExitTree()
        {
            // The game bus outlives the scene (save-load reloads it): stale subscriptions would
            // keep calling handlers on a freed node.
            _gameEventBus?.Unsubscribe<BattleInitializedEvent>(OnBattleInitialized);
            _gameEventBus?.Unsubscribe<PlayerFinalDeathEvent>(OnPlayerFinalDeath);
            _gameEventBus?.Unsubscribe<BattleJoinRequestEvent>(OnBattleJoinRequest);
            _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(null);
            GetTree().AutoAcceptQuit = true; // menu scene has no battles — stock quit is fine there
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest) QuitGracefully();
        }

        /// <summary>The safety net against a hung battle: quitting late is still better than never.</summary>
        private const ulong QuitTimeoutMsec = 5000;

        private bool _quitting;

        /// <summary>Winds an active battle down before quitting (tracker #66/#130): the abort locks
        /// the outcome, the battle task finishes through its normal teardown (BattleEndEvent,
        /// Dispose), and only then the tree goes down. Also serves the in-game quit buttons —
        /// they propagate NotificationWMCloseRequest instead of calling Quit() directly.</summary>
        private async void QuitGracefully()
        {
            if (_quitting) return;
            _quitting = true;

            // The pause menu freezes the tree; a frozen battle would never wind down.
            GetTree().Paused = false;

            if (_activeContext != null)
            {
                _activeContext.Abort();
                ulong deadline = Time.GetTicksMsec() + QuitTimeoutMsec;
                while (_activeContext != null && Time.GetTicksMsec() < deadline)
                    await ToSignal(GetTree(), "process_frame");
            }

            GetTree().Quit();
        }


        public override void _Input(InputEvent @event)
        {
            // Typing is not a hotkey: a focused text field (debug console, future chat) owns the keys.
            if (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;

            foreach ((string action, var window) in s_windowHotkeys)
            {
                if (!@event.IsActionPressed(action)) continue;
                _uiElementProvider?.ToggleWindow(window);
                GetViewport().SetInputAsHandled();
                return;
            }
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

        // Window hotkeys route through InputMap actions (Core.Constants.Settings names them) —
        // availability per game situation is the UiContext map's job, not checks here.
        private static readonly Dictionary<string, Type> s_windowHotkeys = new()
        {
            [Settings.Inventory] = typeof(InventoryWindow),
            [Settings.Quests] = typeof(QuestJournalWindow),
            [Settings.Character] = typeof(CharacterWindow),
            [Settings.Mastery] = typeof(MartialArtMasteryWindow),
            [Settings.PassivesTree] = typeof(PassiveWheelWindow)
        };

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
