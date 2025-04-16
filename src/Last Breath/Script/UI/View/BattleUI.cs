namespace Playground
{
    using System;
    using Godot;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.UI;

    public partial class BattleUI : Control
    {
        private RandomNumberGenerator? _rnd;
        [Export] private Button? _returnButton;
        [Export] private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        [Export] private GridContainer? _playerEffects, _enemyEffects;
        [Export] private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        [Export] private ResourceProgressBar? _playerResource, _enemyResource;
        [Export] private TextureButton? _player, _enemy;
        [Export] private AbilityButtons? _abilities;
        [Export] private HBoxContainer? _attackButtons;
        [Export] private VBoxContainer? _stanceContainer, _bodyContainer;


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
            _abilities?.Initialize();
            SetEvents();
        }

        private void SetEvents()
        {
            _returnButton!.Pressed += () => Return?.Invoke();
            _dexterityStance!.Pressed += () => EmitSignal(SignalName.DexterityStance);
            _strengthStance!.Pressed += () => EmitSignal(SignalName.StrengthStance);
            _intelligenceStance!.Pressed += () => EmitSignal(SignalName.IntelligenceStance);
            _head!.Pressed += () => EmitSignal(SignalName.HeadButtonPressed);
            _player!.Pressed += () => EmitSignal(SignalName.PlayerAreaPressed);
            _enemy!.Pressed += () => EmitSignal(SignalName.EnemyAreaPressed);
        }

        // TODO: On stance change update Abilities
        // TODO: Method to set abilities icons

        public void SetEnemyHealthBar(float current, float max)
        {
            _enemyHealthBar!.MaxValue = max;
            _enemyHealthBar.Value = current;
            GD.Print($"Enemy hp set: {_enemyHealthBar!.Value}. Max health: {_enemyHealthBar.MaxValue}");
        }

        public void SetPlayerHealthBar(float current, float max)
        {
            _playerHealthBar!.MaxValue = max;
            _playerHealthBar.Value = current;
            GD.Print($"Player hp set: {_playerHealthBar!.Value}. Max health: {_playerHealthBar.MaxValue}");
        }

        public void SetAbility(IAbility ability)
        {
            _abilities?.UnbindAbilityButtons();
            _abilities?.BindAbilitysButton(ability);
        }

        public void ClearAbilities() => _abilities?.UnbindAbilityButtons();
        public void HideAttackButtons() => _attackButtons?.Hide();
        public void ShowAttackButtons() => _attackButtons?.Show();
        public void SetPlayerResource(ResourceType type, float current, float max) => _playerResource?.SetNewResource(type, current, max);
        public void SetEnemyResource(ResourceType type, float current, float max) => _enemyResource?.SetNewResource(type, current, max);

        public void OnPlayerCurrenResourceChanges(float obj) => _playerResource!.Value = obj;
        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;
        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;

        public void OnEnemyCurrentResourceChanges(float obj) => _enemyResource!.Value = obj;
        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;
        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;
    }
}
