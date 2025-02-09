namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Godot;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;

    public partial class Player : CharacterBody2D, ICharacter
    {
        #region Private fields
        private AnimatedSprite2D? _sprite;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        private ObservableCollection<IEffect>? _effects;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private List<IAbility>? _abilities;
        #endregion

        #region Components
        private EffectManager? _effectManager;
        private HealthComponent? _playerHealth;
        private AttackComponent? _playerAttack;
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region UI
        private PlayerInventory? _inventory;
        private GridContainer? _inventoryContainer;
        private ProgressBar? _progressBarMovement;
        private RichTextLabel? _playerStats;
        private ProgressBar? _progressBar;
        private Panel? _inventoryWindow;
        private Label? _healthBarText;
        private Node2D? _playersInventoryElements, _inventoryNode;
        #endregion

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

        public PlayerInventory? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        [Export]
        public int Speed { get; set; } = 200;

        public HealthComponent? HealthComponent
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }
        public AttackComponent? AttackComponent
        {
            get => _playerAttack;
            set => _playerAttack = value;
        }

        public EffectManager? EffectManager =>_effectManager;

        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }

        public List<IAbility>? Abilities => _abilities;

        #endregion

        public override void _Ready()
        {
            _effects = [];
            _effectManager = new(_effects);
            _playerHealth = new(_effectManager.CalculateValues);
            _playerAttack = new(_effectManager.CalculateValues);
            var parentNode = GetParent();
            var playerNode = parentNode.GetNode<CharacterBody2D>(nameof(CharacterBody2D));
            _playersInventoryElements = playerNode.GetNode<Node2D>("UI");
            _inventoryNode = _playersInventoryElements.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainer = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            _inventory = new PlayerInventory();
            _playerStats = _playersInventoryElements.GetNode<RichTextLabel>("PlayerStats/PlayerStats");
            _sprite = playerNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _inventory.Initialize(105, ScenePath.InventorySlot, _inventoryContainer!, _inventoryNode.Hide, _inventoryNode.Show);
            _playerHealth.RefreshHealth();
            _playersInventoryElements.Hide();
            _sprite.Play("Idle_down");
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed(InputMaps.OpenInventoryOnI))
            {
                if (_playersInventoryElements!.Visible)
                {
                    _playersInventoryElements?.Hide();
                }
                else
                {
                    _playersInventoryElements?.Show();
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_canMove)
            {
                return;
            }

            Vector2 inputDirection = Input.GetVector(InputMaps.MoveLeft, InputMaps.MoveRight, InputMaps.MoveUp, InputMaps.MoveDown);
            Velocity = inputDirection * Speed;
            MoveAndSlide();
        }
    }
}
