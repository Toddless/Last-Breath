namespace LastBreath.UI
{
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.MessageBus;
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
        [Export] private ProgressBar? _health, _mana;
        [Export] private Label? _healthText, _manaText, _clock;
        [Export] private GridContainer? _playerEffects;

        private IGameMessageBus? _gameMessageBus;
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
            if (_playerAccessor != null) _playerAccessor.PlayerChanged -= BindPlayer;
            UnbindPlayer();
        }

        public void Remove() => GetParent().RemoveChild(this);

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameMessageBus = provider.GetService<IGameMessageBus>();
            _worldClock = provider.GetService<IWorldClock>();
            _events = provider.GetService<IGameEventBus>();
            _events.Subscribe<PlayerHealthChangesEvent>(OnHealthChanged);
            _events.Subscribe<PlayerManaChangesEvent>(OnManaChanged);

            _playerAccessor = provider.GetService<IPlayerAccessor>();
            _playerAccessor.PlayerChanged += BindPlayer;
            if (_playerAccessor.Player != null) BindPlayer(_playerAccessor.Player);
            RefreshClock();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void BindPlayer(IPlayer player)
        {
            UnbindPlayer();
            _boundPlayer = player;
            player.Parameters.ParameterChanged += OnParameterChanged;
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

            UpdateBar(_health, _healthText, _boundPlayer.CurrentHealth, _boundPlayer.Parameters.MaxHealth);
            UpdateBar(_mana, _manaText, _boundPlayer.CurrentMana, _boundPlayer.Parameters.MaxMana);
        }

        private static void UpdateBar(ProgressBar? bar, Label? text, float current, float max)
        {
            if (bar != null)
            {
                bar.MaxValue = Mathf.Max(max, 1f);
                bar.Value = current;
            }

            if (text != null) text.Text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void RefreshClock()
        {
            if (_clock == null || _worldClock == null) return;
            _clock.Text = $"Day {_worldClock.Day}   {_worldClock.Hour:00}:{_worldClock.Minute:00}   {_worldClock.Phase}";
        }

        // TODO:
        // старый подход. Инжектим uiElementsManager и открываем окна с его помощью
        private void OnCraftingBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(CraftingWindow)));
        private void OnQuestBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(QuestJournalWindow)));
        private void OnInventoryBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(InventoryWindow)));
        private void OnCharacterBtnPressed() => _gameMessageBus?.PublishMessageAsync(new OpenWindowMessage(typeof(CharacterWindow)));
    }
}
