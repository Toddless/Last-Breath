namespace LastBreath.UI
{
    using Battle.Source.UIElements;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Localization;
    using Core.Services;
    using Core.Views.UI;
    using Crafting.Source.UIElements;
    using Godot;
    using Inventory;

    /// <summary>
    /// The world HUD: vitals, day/night clock, window buttons. Values come from events only
    /// (vitals from the game bus, maximums from the player's parameter component); the clock
    /// label refreshes on a one-second tick — the world minute is coarser than a frame.
    /// </summary>
    public partial class PlayerHud : Control, IHud
    {
        private const string UID = "uid://boqqyrt0sfpve";
        private const float ClockRefreshSeconds = 1f;

        [Export] private Button? _characterBtn, _inventoryBtn, _questsBtn, _craftingBtn;
        [Export] private Label? _clock;
        [Export] private CharacterBar? _playerBar;
        private IUiElementsManager? _uiElements;
        private IGameEventBus? _events;
        private IPlayerAccessor? _playerAccessor;
        private IWorldClock? _worldClock;
        private IPlayer? _boundPlayer;
        private float _clockTimer;

        public override void _Ready()
        {
            _characterBtn?.Pressed += OnCharacterBtnPressed;
            _inventoryBtn?.Pressed += OnInventoryBtnPressed;
            _questsBtn?.Pressed += OnQuestBtnPressed;
            _craftingBtn?.Pressed += OnCraftingBtnPressed;
        }

        public override void _Process(double delta)
        {
            _clockTimer += (float)delta;
            if (_clockTimer < ClockRefreshSeconds) return;
            _clockTimer = 0f;
            RefreshClock();
        }

        public override void _ExitTree()
        {
            _events?.Unsubscribe<PlayerHealthChangesEvent>(OnHealthChanged);
            _events?.Unsubscribe<PlayerManaChangesEvent>(OnManaChanged);
            _events?.Unsubscribe<PlayerBarrierChangesEvent>(OnBarrierChanged);
            if (_playerAccessor != null) _playerAccessor.PlayerChanged -= BindPlayer;
            UnbindPlayer();
        }

        public void Remove() => GetParent()?.RemoveChild(this);

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElements = provider.GetService<IUiElementsManager>();
            _worldClock = provider.GetService<IWorldClock>();
            _events = provider.GetService<IGameEventBus>();
            _events.Subscribe<PlayerHealthChangesEvent>(OnHealthChanged);
            _events.Subscribe<PlayerManaChangesEvent>(OnManaChanged);
            _events.Subscribe<PlayerBarrierChangesEvent>(OnBarrierChanged);
            _playerAccessor = provider.GetService<IPlayerAccessor>();
            _playerAccessor.PlayerChanged += BindPlayer;
            if (_playerAccessor.Player != null) BindPlayer(_playerAccessor.Player);
            RefreshClock();
        }

        private void OnBarrierChanged(PlayerBarrierChangesEvent obj) => RefreshVitals();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void BindPlayer(IPlayer player)
        {
            UnbindPlayer();
            _boundPlayer = player;
            player.Parameters.ParameterChanged += OnParameterChanged;
            _playerBar?.SetInitialValues(_boundPlayer.Parameters.MaxMana, _boundPlayer.CurrentMana, _boundPlayer.Parameters.MaxHealth, _boundPlayer.CurrentHealth,
                _boundPlayer.Parameters.MaxBarrier, _boundPlayer.CurrentBarrier);
            _playerBar?.SetIdentity(player.PlayerName, Localization.Localize(player.Fractions.ToString()));
            RefreshVitals();
        }

        private void UnbindPlayer()
        {
            if (_boundPlayer != null) _boundPlayer.Parameters.ParameterChanged -= OnParameterChanged;
            _boundPlayer = null;
        }

        private void OnHealthChanged(PlayerHealthChangesEvent evnt) => RefreshVitals();

        private void OnManaChanged(PlayerManaChangesEvent evnt) => RefreshVitals();

        private void OnParameterChanged(Core.Enums.EntityParameter parameter, float value) => RefreshVitals();

        private void RefreshVitals()
        {
            if (_boundPlayer == null) return;

            _playerBar?.UpdateHealth(_boundPlayer.CurrentHealth);
            _playerBar?.UpdateMaxHealth(_boundPlayer.Parameters.MaxHealth);
            _playerBar?.UpdateMana(_boundPlayer.CurrentMana);
            _playerBar?.UpdateMaxMana(_boundPlayer.Parameters.MaxMana);
            _playerBar?.UpdateBarrier(_boundPlayer.CurrentBarrier, _boundPlayer.Parameters.MaxBarrier);
        }

        private void RefreshClock()
        {
            if (_clock == null || _worldClock == null) return;
            _clock.Text = $"Day {_worldClock.Day}   {_worldClock.Hour:00}:{_worldClock.Minute:00}   {_worldClock.Phase}";
        }

        // Straight through the manager: the OpenWindowMessage handler is gone, published
        // messages went nowhere. Toggle = hotkey semantics (a second press closes).
        private void OnCraftingBtnPressed() => _uiElements?.ToggleWindow(typeof(CraftingWindow));
        private void OnQuestBtnPressed() => _uiElements?.ToggleWindow(typeof(QuestJournalWindow));
        private void OnInventoryBtnPressed() => _uiElements?.ToggleWindow(typeof(InventoryWindow));
        private void OnCharacterBtnPressed() => _uiElements?.ToggleWindow(typeof(CharacterWindow));
    }
}
