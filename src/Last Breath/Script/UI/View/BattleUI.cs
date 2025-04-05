namespace Playground
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.ScenesHandlers;

    public partial class BattleUI : Control
    {
        private Button? _returnButton;
        private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        private GridContainer? _playerEffects, _enemyEffects;
        private RandomNumberGenerator? _rnd;
        private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs, _firstAbility, _secondAbility;
        private BattleSceneHandler? _battleSceneHandler;
        private Panel? _panelPlayer, _panelEnemy;
        private HBoxContainer? _buttons;
        private VBoxContainer? _stanceContainer, _bodyContainer;

        public Action? Return;
        [Signal]
        public delegate void HeadButtonPressedEventHandler();
        [Signal]
        public delegate void DexterityStanceEventHandler();
        [Signal]
        public delegate void StrengthStanceEventHandler();
        [Signal]
        public delegate void IntelligenceStanceEventHandler();

        public override void _Ready()
        {
            _buttons = (HBoxContainer?)NodeFinder.FindBFSCached(this, "HBoxContainerAttackButtons");
            _stanceContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "VBoxContainerStance");
            _bodyContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "VBoxContainerBodyPart");

            _dexterityStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Dexterity");
            _strengthStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Intelligence");
            _intelligenceStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Strength");

            _head = (TextureButton?)NodeFinder.FindBFSCached(this, "Head");
            _body = (TextureButton?)NodeFinder.FindBFSCached(this, "Body");
            _legs = (TextureButton?)NodeFinder.FindBFSCached(this, "Legs");
            _firstAbility = (TextureButton?)NodeFinder.FindBFSCached(this, "FirstAbility");
            _secondAbility = (TextureButton?)NodeFinder.FindBFSCached(this, "SecondAbility");

            _playerHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "PlayerHealth");
            _enemyHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "EnemyHealth");
            _playerEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "PlayerEffects");
            _enemyEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "EnemyEffects");
            _returnButton = (Button?)NodeFinder.FindBFSCached(this, "ReturnButton");

            _panelPlayer = (Panel?)NodeFinder.FindBFSCached(this, "Panel");
            _panelEnemy = (Panel?)NodeFinder.FindBFSCached(this, "Panel2");
            SetEvents();
            NodeFinder.ClearCache();
        }

        private void SetEvents()
        {
            _returnButton!.Pressed += () => Return?.Invoke();
            _head!.Pressed += () => EmitSignal(SignalName.HeadButtonPressed);
            _dexterityStance!.Pressed += () => EmitSignal(SignalName.DexterityStance);
            _strengthStance!.Pressed += () => EmitSignal(SignalName.StrengthStance);
            _intelligenceStance!.Pressed += () => EmitSignal(SignalName.IntelligenceStance);
        }

        public void InitialSetup(Player player, BaseEnemy enemy)
        {
            _playerHealthBar.MaxValue = player.Health.MaxHealth;
            _playerHealthBar.Value = player.Health.CurrentHealth;

            _enemyHealthBar.MaxValue = enemy.Health.MaxHealth;
            _enemyHealthBar.Value = enemy.Health.CurrentHealth;
            player.Position = _panelPlayer.GlobalPosition;
            enemy.Position = _panelEnemy.GlobalPosition;

            player.Health.CurrentHealthChanged += OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged += OnPlayerMaxHealthChanged;
            enemy.Health.CurrentHealthChanged += OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged += OnEnemyMaxHealthChanged;
        }

        public void HideAttackButtons() => _buttons?.Hide();
        public void ShowAttackButtons() => _buttons?.Show();

        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;

        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;

        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;

        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;
    }
}
