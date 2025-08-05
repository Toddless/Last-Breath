namespace LastBreath
{
    using System;
    using Core.Enums;
    using Godot;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.BattleSystem;
    using LastBreath.Script.UI;
    using LastBreath.Script.UI.View;

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


        public void SetEnemyHealthBar(float current, float max)
        {
            _enemyHealthBar!.MaxValue = max;
            _enemyHealthBar.Value = current;
        }

        public void SetPlayerHealthBar(float current, float max)
        {
            _playerHealthBar!.MaxValue = max;
            _playerHealthBar.Value = current;
        }

        public void SetAbility(IAbility ability)
        {
            _abilities?.UnbindAbilityButtons();
            _abilities?.BindAbilitysButton(ability);
        }

        public void ClearAbilities() => _abilities?.UnbindAbilityButtons();
        public void HideButtons()
        {
            _attackButtons?.Hide();
            _returnButton?.Hide();
        }
        public void ShowButtons()
        {
            _attackButtons?.Show();
            _returnButton?.Show();
        }
        public void SetPlayerResource(ResourceType type, float current, float max) => _playerResource?.SetNewResource(type, current, max);
        public void SetEnemyResource(ResourceType type, float current, float max) => _enemyResource?.SetNewResource(type, current, max);

        public void OnPlayerCurrenResourceChanges(float value) => _playerResource!.Value = value;
        public void OnPlayerMaxResourceChanges(float value) => _playerResource!.MaxValue = value;
        public void OnPlayerCurrentHealthChanged(float newValue) => _playerHealthBar!.Value = newValue;
        public void OnPlayerMaxHealthChanged(float newValue) => _playerHealthBar!.MaxValue = newValue;

        public void OnEnemyCurrentResourceChanges(float value) => _enemyResource!.Value = value;
        public void OnEnemyMaxResourceChanges(float value) => _enemyResource!.MaxValue = value;
        public void OnEnemyCurrentHealthChanged(float newValue) => _enemyHealthBar!.Value = newValue;
        public void OnEnemyMaxHealthChanged(float newValue) => _enemyHealthBar!.MaxValue = newValue;

        public void OnGettingAttack(OnGettingAttackEventArgs args)
        {
            // TODO: Rework and remove this from here
            var floatingText = new FloatingText();
            var targetRect = args.Character is Player ? _player.GetGlobalRect() : _enemy.GetGlobalRect();
            Vector2 globalPosition = new(targetRect.Position.X + targetRect.Size.X / 2, targetRect.Position.Y);
            Vector2 localPosition = GetGlobalTransform().AffineInverse() * globalPosition;
            floatingText.Position = localPosition;
            AddChild(floatingText);
            switch (args.Result)
            {
                case AttackResults.Evaded:
                    floatingText.ShowValue("Evade!", new Vector2(0, -75), 1f, 5f);
                    break;
                case AttackResults.Blocked:
                    floatingText.ShowValue("Blocked!", new Vector2(0, -75), 1f, 5f);
                    break;
                case AttackResults.Succeed:
                    floatingText.ShowValue(Mathf.RoundToInt(args.Damage).ToString(), new Vector2(0, -75), 1f, 5f, args.IsCrit);
                    break;
            }
        }
    }
}
