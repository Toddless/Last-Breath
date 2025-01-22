namespace Playground
{
    using Godot;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;
    using Playground.Script.Passives.Attacks;

    public partial class Player : CharacterBody2D, ICharacter
    {
        #region Private fields
        // for grid movement
        //  private double _maxMovementPoints = 15;
        //   private const int _tileSize = 64;

        private Vector2 _inputDirection = Vector2.Zero;
        private AnimatedSprite2D? _sprite;
        private GlobalSignals? _globalSignals;
        private ResearchArea? _currentZone;
        private double _movementPoints;
        private BodyArmor? _playerArmor;
        private Weapon? _playerWeapon;
        private bool _isMoving;
        private int _speed;
        private Vector2 _lastPosition;
        private bool _canMove = true;
        #endregion

        #region Components
        private HealthComponent? _playerHealth;
        private AttackComponent? _playerAttack;
        private AttributeComponent? _playerAttribute;
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region UI
        private InventoryComponent? _inventory;
        private GridContainer? _inventoryContainder;
        private ProgressBar? _progressBarMovement;
        private ResearchButton? _researchButton;
        private TextureProgressBar? _healthBar;
        private Button? _doSomeDamageButton;
        private RichTextLabel? _playerStats;
        private ProgressBar? _progressBar;
        private Panel? _inventoryWindow;
        private Label? _healthBarText;
        private Node2D? _playersInventoryElements, _inventoryNode;
        #endregion

        #region Properties
        public BodyArmor? PlayerArmor
        {
            get => _playerArmor;
            set => _playerArmor = value;
        }

        public Weapon? PlayerWeapon
        {
            get => _playerWeapon;
            set => _playerWeapon = value;
        }

        public HealthComponent? PlayerHealth
        {
            get => _playerHealth;
            set => _playerHealth = value;
        }

        public AttackComponent? PlayerAttack
        {
            get => _playerAttack;
            set => _playerAttack = value;
        }

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

        public InventoryComponent? Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        public AnimatedSprite2D? PlayerAnimation
        {
            get => _sprite;
            set => _sprite = value;
        }

        public AttributeComponent? PlayerAttribute
        {
            get => _playerAttribute;
            set => _playerAttribute = value;
        }

        [Export]
        public int Speed { get; set; } = 200;
        public HealthComponent HealthComponent
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }
        public AttackComponent AttackComponent
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }
        #endregion

        public override void _Ready()
        {
            var parentNode = GetParent();
            var uiNodes = parentNode.GetNode("UI");
            var playerNode = parentNode.GetNode<CharacterBody2D>(nameof(CharacterBody2D));
            _playersInventoryElements = playerNode.GetNode<Node2D>("UI");
            _inventoryNode = _playersInventoryElements.GetNode<Node2D>("Inventory");
            _inventoryWindow = _inventoryNode.GetNode<Panel>("InventoryWindow");
            _inventoryContainder = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            _inventory = _inventoryContainder.GetNode<InventoryComponent>(nameof(InventoryComponent));
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _playerHealth = playerNode.GetNode<HealthComponent>(nameof(HealthComponent));
            _playerAttack = playerNode.GetNode<AttackComponent>(nameof(AttackComponent));
            _playerAttribute = playerNode.GetNode<AttributeComponent>(nameof(AttributeComponent));
            _progressBarMovement = uiNodes.GetNode<ProgressBar>("PlayerBars/StaminaBar");
            _researchButton = uiNodes.GetNode<ResearchButton>("Buttons/ResearchButton");
            _healthBar = uiNodes.GetNode<TextureProgressBar>("PlayerBars/HealthProgressBar");
            _playerStats = _playersInventoryElements.GetNode<RichTextLabel>("PlayerStats/PlayerStats");
            _sprite = playerNode.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D));
            _researchButton.Pressed += ResearchCurrentZone;
            _globalSignals.OnEquipItem += OnEquipItem;
            _inventory.Inititalize(105, ScenePath.InventorySlot, _inventoryContainder!, _inventoryNode.Hide, _inventoryNode.Show);
            _playerHealth.RefreshHealth();
            SetHealthBar();
            UpdateStats();
            _playersInventoryElements.Hide();
            _sprite.Play("Idle_down");
        }

        private void UpdateHealthBar()
        {
            _healthBar!.Value = _playerHealth!.CurrentHealth;
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

        public override void _UnhandledInput(InputEvent @event)
        {
            //if (Input.IsActionJustPressed(InputMaps.MoveDown))
            //{
            //    _inputDirection = Vector2.Down;
            //    MoveAndSlide();
            //}
            //else if (Input.IsActionJustPressed(InputMaps.MoveUp))
            //{
            //    _inputDirection = Vector2.Up;
            //    MoveAndSlide();
            //}
            //else if (Input.IsActionJustPressed(InputMaps.MoveLeft))
            //{
            //    _inputDirection = Vector2.Left;
            //    MoveAndSlide();
            //}
            //else if (Input.IsActionJustPressed(InputMaps.MoveRight))
            //{
            //    _inputDirection = Vector2.Right;
            //    MoveAndSlide();
            //}
        }

        private void UpdateStats()
        {
            _playerStats!.Text = $"Damage: {_playerAttack!.BaseMinDamage} - {_playerAttack.BaseMaxDamage} \n" +
                $"Critical Hit Chance: {_playerAttack.BaseCriticalStrikeChance * 100}% \n" +
                $"Critical Hit Damage: {_playerAttack.BaseCriticalStrikeDamage * 100}% \n" +
                $"Health: {_playerHealth!.CurrentHealth}\n" +
                //  $"Defence: {_playerHealth.Defence}\n" +
                $"Max. Health: {_playerHealth.MaxHealth}\n" +
                $"Strength: {PlayerAttribute!.Strength!.Total}\n" +
                $"Dexterity: {PlayerAttribute.Dexterity!.Total}\n" +
                $"Intelligence: {PlayerAttribute.Intelligence!.Total}";
        }

        private void SetHealthBar()
        {
            _healthBar!.MaxValue = _playerHealth!.MaxHealth;
            _healthBar.Value = _playerHealth.CurrentHealth;
            var tween = CreateTween();
            tween.TweenProperty(_healthBar, "value", _playerHealth.CurrentHealth, 0.2f);
        }

        private void OnPlayerEnteredZone(ResearchArea zone)
        {
            _currentZone = zone;
        }

        private void OnPlayerExitedZone()
        {
            _currentZone = null;
        }

        private void ResearchCurrentZone()
        {
            if (_currentZone == null)
                return;
            var item = _currentZone.GetRandomResearchEvent();
            if (item != null)
            {
                _inventory!.AddItem(item);
            }
        }

        private void OnEquipItem(Item item)
        {
            if (item is Weapon weapon)
            {
                if (_playerWeapon != null)
                {
                    _inventory!.AddItem(_playerWeapon);
                    _playerAttack!.BaseMinDamage -= _playerWeapon.MinDamage;
                    _playerAttack.BaseMaxDamage -= _playerWeapon.MaxDamage;
                    _playerAttack.BaseCriticalStrikeChance = 0.05f;
                    _playerWeapon = null;
                    UpdateStats();
                }
                _playerWeapon = weapon;
                _playerAttack!.BaseMinDamage += _playerWeapon.MinDamage;
                _playerAttack.BaseMaxDamage += _playerWeapon.MaxDamage;
                _playerAttack.BaseCriticalStrikeChance = _playerWeapon.CriticalStrikeChance;
                _inventory!.RemoveItem(weapon);
                UpdateStats();
            }
            else if (item is BodyArmor armor)
            {
                if (_playerArmor != null)
                {
                    _inventory!.AddItem(_playerArmor);
                    _playerArmor = null;
                    UpdateStats();
                }
                _playerArmor = armor;
                _inventory!.RemoveItem(armor);
                UpdateStats();
            }
        }

        private void MoveGrid()
        {
            //if (_inputDirection != Vector2.Zero)
            //{
            //    if (!_moving && _movementPoints != 0)
            //    {
            //        _moving = true;
            //        // создаем новый tween
            //        var tween = CreateTween();
            //        // передаем в tween свойство, которое хотим изменить, направление и длину анимации
            //        tween.TweenProperty(this, "position", Position + _inputDirection * _tileSize, 0.1f);
            //        tween.TweenCallback(new Callable(this, nameof(MovingFalse)));
            //        tween.TweenCallback(new Callable(this, nameof(ReduceMovementPoint)));
            //        // ожидаем завершения анимации
            //        await ToSignal(tween, "finished");
            //    }
            //}
        }
    }
}
