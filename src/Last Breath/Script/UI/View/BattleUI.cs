namespace Playground
{
    using System;
    using Godot;
    using Playground.Script.Helpers;

    public partial class BattleUI : Control
    {
        private Button? _returnButton;
        private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        private GridContainer? _playerEffects, _enemyEffects;
        private RandomNumberGenerator? _rnd;
        private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        private TextureButton? _abilityOne, _abilityTwo, _abilityThree, _abilityFour, _abilityFive, _abilitySix, _abilitySeven;
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

        [Signal]
        public delegate void FirstAbilityPressedEventHandler();
        [Signal]
        public delegate void SecondAbilityPressedEventHandler();
        [Signal]
        public delegate void ThirdAbilityPressedEventHandler();
        [Signal]
        public delegate void FourthAbilityPressedEventHandler();
        [Signal]
        public delegate void FifthAbilityPressedEventHandler();
        [Signal]
        public delegate void SixthAbilityPressedEventHandler();
        [Signal]
        public delegate void SeventhAbilityPressedEventHandler();

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

            _abilityOne = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability1");
            _abilityTwo = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability2");
            _abilityThree = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability3");
            _abilityFour = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability4");
            _abilityFive = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability5");
            _abilitySix = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability6");
            _abilitySeven = (TextureButton?)NodeFinder.FindBFSCached(this, "Ability7");

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

            #region Abilities
            _abilityOne!.Pressed += () => EmitSignal(SignalName.FirstAbilityPressed);
            _abilityTwo!.Pressed += () => EmitSignal(SignalName.SecondAbilityPressed);
            _abilityThree!.Pressed += () => EmitSignal(SignalName.ThirdAbilityPressed);
            _abilityFour!.Pressed += () => EmitSignal(SignalName.FourthAbilityPressed);
            _abilityFive!.Pressed += () => EmitSignal(SignalName.FifthAbilityPressed);
            _abilitySix!.Pressed += () => EmitSignal(SignalName.SixthAbilityPressed);
            _abilitySeven!.Pressed += () => EmitSignal(SignalName.SeventhAbilityPressed);
            #endregion
        }

        // TODO: Method to set abilities icons

        public void SubscribeBattleUI(Player player, BaseEnemy enemy)
        {
            _playerHealthBar.MaxValue = player.Health.MaxHealth;
            _playerHealthBar.Value = player.Health.CurrentHealth;

            _enemyHealthBar.MaxValue = enemy.Health.MaxHealth;
            _enemyHealthBar.Value = enemy.Health.CurrentHealth;

            player.Health.CurrentHealthChanged += OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged += OnPlayerMaxHealthChanged;
            enemy.Health.CurrentHealthChanged += OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged += OnEnemyMaxHealthChanged;
        }

        public void UnsubscribeBattleUI(Player player, BaseEnemy enemy)
        {
            player.Health.CurrentHealthChanged -= OnPlayerCurrentHealthChanged;
            player.Health.MaxHealthChanged -= OnPlayerMaxHealthChanged;
            enemy.Health.CurrentHealthChanged -= OnEnemyCurrentHealthChanged;
            enemy.Health.MaxHealthChanged -= OnEnemyMaxHealthChanged;
        }

        public void HideAttackButtons() => _buttons?.Hide();
        public void ShowAttackButtons() => _buttons?.Show();

        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;

        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;

        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;

        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;
    }
}
