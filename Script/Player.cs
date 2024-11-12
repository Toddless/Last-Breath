namespace Playground
{
    using System;
    using Godot;
    using Playground.Script;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class Player : CharacterBody2D
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
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        #endregion

        [Signal]
        public delegate void PlayerEnterTheBattleEventHandler();

        [Signal]
        public delegate void PlayerExitedTheBattleEventHandler();

        #region UI
        private InventoryComponent? _inventoryComponent;
        private GridContainer? _inventoryContainder;
        private ProgressBar? _progressBarMovement;
        private ResearchButton? _researchButton;
        private TextureProgressBar? _healthBar;
        private Button? _doSomeDamageButton;
        private RichTextLabel? _playerStats;
        private ProgressBar? _progressBar;
        private Panel? _inventoryWindow;
        private Label? _healthBarText;
        private TextureRect? _goldIcon;
        private Label? _goldAmount;
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
            get => _healthComponent;
            set => _healthComponent = value;
        }

        public AttackComponent? PlayerAttack
        {
            get => _attackComponent;
            set => _attackComponent = value;
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
            get => _inventoryComponent;
            set => _inventoryComponent = value;
        }

        public AnimatedSprite2D? PlayerAnimation
        {
            get => _sprite;
            set => _sprite = value;
        }

        [Export]
        public int Speed { get; set; } = 200;
        #endregion

        public override void _Ready()
        {
            var parentNode = GetParent();
            var uiNodes = parentNode.GetNode("UI");
            var playerNode = parentNode.GetNode<CharacterBody2D>("CharacterBody2D");
            _inventoryWindow = playerNode.GetNode("Inventory").GetNode<Panel>("InventoryWindow");
            _inventoryContainder = _inventoryWindow.GetNode<GridContainer>("InventoryContainer");
            _inventoryComponent = _inventoryContainder.GetNode<InventoryComponent>("InventoryComponent");
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _healthComponent = playerNode.GetNode<HealthComponent>("HealthComponent");
            _attackComponent = playerNode.GetNode<AttackComponent>("AttackComponent");
            _progressBarMovement = uiNodes.GetNode<ProgressBar>("PlayerBars/StaminaBar");
            _researchButton = uiNodes.GetNode<ResearchButton>("Buttons/ResearchButton");
            _goldIcon = _inventoryWindow.GetNode<TextureRect>("TextureRect");
            _goldAmount = _goldIcon.GetNode<Label>("Label");
            _healthBar = uiNodes.GetNode<TextureProgressBar>("PlayerBars/HealthProgressBar");
            _playerStats = playerNode.GetNode<RichTextLabel>("PlayerStats/PlayerStats");
            _sprite = playerNode.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            _attackComponent.OnPlayerCriticalHit += PlayerDidCriticalDamage;
            _researchButton.Pressed += ResearchCurrentZone;
            _globalSignals.OnEquipItem += OnEquipItem;
            _inventoryComponent.Inititalize(105, SceneParh.InventorySlot, _inventoryContainder!);
            _healthComponent.IncreasedMaximumHealth(10000);
            _healthComponent.RefreshHealth();
            ToggleWindow(false);
            SetHealthBar();
            UpdateStats();
            _sprite.Play("Idle_down");
            if (_attackComponent == null || _inventoryComponent == null || _progressBarMovement == null || _healthBar == null)
            {
                ArgumentNullException.ThrowIfNull(_healthBar);
                ArgumentNullException.ThrowIfNull(_attackComponent);
                ArgumentNullException.ThrowIfNull(_inventoryComponent);
                ArgumentNullException.ThrowIfNull(_progressBarMovement);
            }
        }

        private void UpdateHealthBar()
        {
            _healthBar!.Value = _healthComponent!.CurrentHealth;
        }

        private void PlayerDidCriticalDamage()
        {
            GD.Print($"Critical hit!");
        }

        public bool ToggleWindow(bool isOpen)
        {
            #region if you need to hide the mouse cursor
            //if (isOpen)
            //{
            //    Input.MouseMode = Input.MouseModeEnum.Visible;
            //}
            //else
            //{
            //    Input.MouseMode = Input.MouseModeEnum.Captured;
            //}

            #endregion
            _playerStats!.Visible = isOpen;
            return _inventoryWindow!.Visible = isOpen;
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed(InputMaps.OpenInventoryOnI))
            {
                ToggleWindow(!_inventoryWindow!.Visible);
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


        private void UpdateStats()
        {
            _playerStats!.Text = $"Damage: {_attackComponent!.BaseMinDamage} - {_attackComponent.BaseMaxDamage} \n" +
                $"Critical Hit Chance: {_attackComponent.CriticalStrikeChance * 100}% \n" +
                $"Critical Hit Damage: {_attackComponent.CriticalStrikeDamage * 100}% \n" +
                $"Health: {_healthComponent!.CurrentHealth}\n" +
                $"Defence: {_healthComponent.Defence}\n" +
                $"Max. Health: {_healthComponent.MaxHealth}";
        }



        private void SetHealthBar()
        {
            _healthBar!.MaxValue = _healthComponent!.MaxHealth;
            _healthBar.Value = _healthComponent.CurrentHealth;
            var tween = CreateTween();
            tween.TweenProperty(_healthBar, "value", _healthComponent.CurrentHealth, 0.2f);
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
                _inventoryComponent!.AddItem(item);
            }
        }

        private void OnEquipItem(Item item)
        {
            if (item is Weapon weapon)
            {
                if (_playerWeapon != null)
                {
                    _inventoryComponent!.AddItem(_playerWeapon);
                    _attackComponent!.BaseMinDamage -= _playerWeapon.MinDamage;
                    _attackComponent.BaseMaxDamage -= _playerWeapon.MaxDamage;
                    _attackComponent.CriticalStrikeChance = 0.05f;
                    _playerWeapon = null;
                    UpdateStats();
                }
                _playerWeapon = weapon;
                _attackComponent!.BaseMinDamage += _playerWeapon.MinDamage;
                _attackComponent.BaseMaxDamage += _playerWeapon.MaxDamage;
                _attackComponent.CriticalStrikeChance = _playerWeapon.CriticalStrikeChance;
                _inventoryComponent!.RemoveItem(weapon);
                UpdateStats();
            }
            else if (item is BodyArmor armor)
            {
                if (_playerArmor != null)
                {
                    _inventoryComponent!.AddItem(_playerArmor);
                    _healthComponent!.MaxHealth -= _playerArmor.BonusHealth;
                    _healthComponent.Defence -= _playerArmor.Defence;
                    _playerArmor = null;
                    UpdateStats();
                }
                _playerArmor = armor;
                _healthComponent!.MaxHealth += _playerArmor.BonusHealth;
                _healthComponent.Defence += _playerArmor.Defence;
                _inventoryComponent!.RemoveItem(armor);
                UpdateStats();
            }
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
