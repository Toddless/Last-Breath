namespace Playground
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Helpers;
    using Playground.Script.UI;

    public partial class BattleUI : Control
    {
        private Button? _returnButton;
        private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        private GridContainer? _playerEffects, _enemyEffects;
        private RandomNumberGenerator? _rnd;
        private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        private TextureButton? _player, _enemy;
        private AbilityButtons? _abilities;
        private HBoxContainer? _attackButtons;
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
        public delegate void PlayerAreaPressedEventHandler();
        [Signal]
        public delegate void EnemyAreaPressedEventHandler();

        public override void _Ready()
        {
            _attackButtons = (HBoxContainer?)NodeFinder.FindBFSCached(this, "AttackButtons");
            _abilities = (AbilityButtons?)NodeFinder.FindBFSCached(this, "AbilityButtons");
            _stanceContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "VBoxContainerStance");
            _bodyContainer = (VBoxContainer?)NodeFinder.FindBFSCached(this, "VBoxContainerBodyPart");

            _dexterityStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Dexterity");
            _strengthStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Intelligence");
            _intelligenceStance = (TextureButton?)NodeFinder.FindBFSCached(this, "Strength");

            _head = (TextureButton?)NodeFinder.FindBFSCached(this, "Head");
            _body = (TextureButton?)NodeFinder.FindBFSCached(this, "Body");
            _legs = (TextureButton?)NodeFinder.FindBFSCached(this, "Legs");

            _playerHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "PlayerHealth");
            _enemyHealthBar = (TextureProgressBar?)NodeFinder.FindBFSCached(this, "EnemyHealth");
            _playerEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "PlayerEffects");
            _enemyEffects = (GridContainer?)NodeFinder.FindBFSCached(this, "EnemyEffects");
            _returnButton = (Button?)NodeFinder.FindBFSCached(this, "ReturnButton");

            _player = (TextureButton?)NodeFinder.FindBFSCached(this, "Player");
            _enemy = (TextureButton?)NodeFinder.FindBFSCached(this, "Enemy");

            _abilities?.Initialize();
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
            _player!.Pressed += () => EmitSignal(SignalName.PlayerAreaPressed);
            _enemy!.Pressed += () => EmitSignal(SignalName.EnemyAreaPressed);
        }

        // TODO: Method to set abilities icons

        public void SubscribeBattleUI(Player player, BaseEnemy enemy)
        {
            _playerHealthBar!.MaxValue = player.Health.MaxHealth;
            _playerHealthBar.Value = player.Health.CurrentHealth;

            _enemyHealthBar!.MaxValue = enemy.Health.MaxHealth;
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

        public void OnTurnEnds() => _abilities?.UpdateAbiliesCooldown();

        public void OnTargetChanges(ICharacter? target)
        {
            if (target == null) return;
            _abilities?.UpdateAbilitiesTarget(target);
        }

        public void SetAbility(IAbility ability) => _abilities?.BindAbilitysButton(ability);
        public void HideAttackButtons() => _attackButtons?.Hide();
        public void ShowAttackButtons() => _attackButtons?.Show();

        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;
        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;
        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;
        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;
    }
}
