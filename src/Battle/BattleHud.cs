namespace Battle
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Events.GameEvents;

    public partial class BattleHud : Control, IInitializable, IRequireServices
    {
        private IGameEventBus? _battleEventBus;
        private const string UID = "uid://2w3t3maumkh6";
        [Export] private Button? _returnButton;
        [Export] private TextureProgressBar? _playerHealthBar, _playerManaBar;
        [Export] private GridContainer? _playerEffects;
        [Export] private HBoxContainer? _stanceButtons, _abilityButtons;

        private IMediator? _mediator;

        public override void _Ready()
        {
        }

        public void SetupEventBus(IGameEventBus battleEventBus)
        {
            _battleEventBus = battleEventBus;
            _battleEventBus.Subscribe<PlayerHealthChangesGameEvent>(OnPlayerHealthChanges);
            _battleEventBus.Subscribe<PlayerManaChangesGameEvent>(OnPlayerManaChanges);
        }

        public void SetInitialValues(float maxHealth, float maxMana, float health, float mana)
        {
            _playerHealthBar?.MaxValue = maxHealth;
            _playerHealthBar?.Value = health;
            _playerManaBar?.MaxValue = maxMana;
            _playerManaBar?.Value = mana;
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            try
            {
                _mediator = provider.GetService<IMediator>();
            }
            catch (Exception ex)
            {
                Tracker.TrackError("Failed to inject services.", ex);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        private void OnPlayerHealthChanges(PlayerHealthChangesGameEvent obj)
        {
            _playerHealthBar?.Value = (obj.Value);
        }

        private void OnPlayerManaChanges(PlayerManaChangesGameEvent obj)
        {
            _playerManaBar?.Value = (obj.Value);
        }
    }
}
