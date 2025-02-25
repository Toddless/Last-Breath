namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Localization;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Helpers;
    using Playground.Script.NPC;
    using Playground.Script.Reputation;

    public partial class Player : ObservableCharacterBody2D, ICharacter, ISpeaking
    {
        #region Private fields
        private AnimatedSprite2D? _sprite;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private PlayerProgress? _progress;
        private Dictionary<string, DialogueNode> _dialogs = [];
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
        public bool FirstSpawn { get; set; } = true;
        [Export]
        [Changeable]
        public int Speed { get; set; } = 200;
        public Dictionary<string, DialogueNode> Dialogs => _dialogs;
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
            _progress = new();
            _sprite = GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _playerAvatar = GetNode<Sprite2D>(nameof(Sprite2D));
            _sprite.Play("Idle_down");
            _reputation = new(0, 0, 0);
            _dialogs = LocalizationManager.LoadDialogue("Resource/Dialogues/playerDialogues.json");
            GameManager.Instance!.Player = this;
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
