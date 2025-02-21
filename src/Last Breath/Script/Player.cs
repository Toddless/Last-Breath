namespace Playground
{
    using Godot;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Helpers;
    using Playground.Script;
    using Playground.Script.Reputation;

    public partial class Player : ObservableCharacterBody2D, ICharacter
    {
        #region Private fields
        private AnimatedSprite2D? _sprite;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private List<IAbility>? _abilities;
        private Sprite2D? _playerAvatar;
        #endregion

        #region Components
        private EffectManager? _effectManager;
        private HealthComponent? _playerHealth;
        private AttackComponent? _playerAttack;
        private ReputationManager? _reputation;
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region Properties
        public Vector2 PlayerLastPosition
        {
            get => _lastPosition;
            set => _lastPosition = value;
        }
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }
        [Export]
        [Changeable]
        public int Speed { get; set; } = 200;
        [Changeable]
        public HealthComponent? HealthComponent
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }
        [Changeable]
        public AttackComponent? AttackComponent
        {
            get => _playerAttack;
            set => _playerAttack = value;
        }
        [Changeable]
        public ReputationManager? Reputation => _reputation;
        public EffectManager? EffectManager => _effectManager;
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }
        public List<IAbility>? Abilities => _abilities;
        public Sprite2D? PlayerAvatar => _playerAvatar;
        #endregion

        public override void _Ready()
        {
            _effects = [];
            _effectManager = new(_effects);
            _playerHealth = new(_effectManager.CalculateValues);
            _playerAttack = new(_effectManager.CalculateValues);
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _playerAvatar = GetNode<Sprite2D>(nameof(Sprite2D));
            _sprite.Play("Idle_down");
            GameManager.Instance!.Player = this;
            _reputation = new(0, 0, 0);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_canMove)
            {
                return;
            }

            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * Speed;
            MoveAndSlide();
        }
    }
}
