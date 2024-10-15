namespace Playground
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class Player : CharacterBody2D
    {

        #region Const
        private const string RestoreMovementPointsButton = "/root/MainScene/UI/Buttons/ChillButton";
        private const string ResearchButton = "/root/MainScene/UI/Buttons/ResearchButton";
        private const string StaminaProgressBar = "/root/MainScene/UI/PlayerBars/StaminaBar";
        #endregion

        #region Private fields
        private Vector2 _inputDirection = Vector2.Zero;
        private double _maxMovementPoints = 15;
        private ResearchArea _currentZone;
        private const int _tileSize = 64;
        private double _movementPoints;
        private BodyArmor _playerArmor;
        private Weapon _playerWeapon;
        private bool _moving = false;
        #endregion

        #region Export fields

      

        #endregion

        #region Components
        private HealthComponent _healthComponent;
        private AttackComponent _attackComponent;
        #endregion

        #region UI
        private RestorePlayerMovement _restoreMovementButton;
        private InventoryComponent _inventoryComponent;
        private ProgressBar _progressBarMovement;
        private ResearchButton _researchButton;
        private Button _doSomeDamageButton;
        private ProgressBar _healthBar;
        private Label _healthBarText;
        #endregion

        #region Signals

        #endregion

        #region Properties
        public BodyArmor PlayerArmor
        {
            get => _playerArmor;
            set => _playerArmor = value;
        }

        public Weapon PlayerWeapon
        {
            get => _playerWeapon;
            set => _playerWeapon = value;
        }
        #endregion

        public override void _Ready()
        {
            _restoreMovementButton = GetNode<RestorePlayerMovement>(RestoreMovementPointsButton);
            _doSomeDamageButton = GetNode<Button>("/root/MainScene/UI/Buttons/DoSomeDamage");
            _inventoryComponent = GetNode<InventoryComponent>(nameof(InventoryComponent));
            _healthComponent = GetNode<HealthComponent>(nameof(HealthComponent));
            _attackComponent = GetNode<AttackComponent>(nameof(AttackComponent));
            _progressBarMovement = GetNode<ProgressBar>(StaminaProgressBar);
            _attackComponent.OnPlayerCriticalHit += PlayerDidCriticalDamage;
            _researchButton = GetNode<ResearchButton>(ResearchButton);
            _restoreMovementButton.Pressed += RestoreMovementPoints;
            //_globalSignals = GetNode("/root/GlobalSignal") as GlobalSignals;
            _researchButton.Pressed += ResearchCurrentZone;
            _healthComponent.OnCharacterDied += PlayerDied;
            _movementPoints = _maxMovementPoints;
        }

        private void PlayerDidCriticalDamage()
        {
            GD.Print($"Critical hit!");
        }

        private void OnPressed()
        {
            GD.Print($"Current damage is: {_attackComponent.BaseMinDamage} - {_attackComponent.BaseMaxDamage}");
            GD.Print($"Player did: {_attackComponent.DealDamage()}");
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed(InputMaps.MoveDown))
            {
                _inputDirection = Vector2.Down;
                Move();
            }
            else if (Input.IsActionJustPressed(InputMaps.MoveUp))
            {
                _inputDirection = Vector2.Up;
                Move();
            }
            else if (Input.IsActionJustPressed(InputMaps.MoveLeft))
            {
                _inputDirection = Vector2.Left;
                Move();
            }
            else if (Input.IsActionJustPressed(InputMaps.MoveRight))
            {
                _inputDirection = Vector2.Right;
                Move();
            }
        }

        private void OnEquipItem(Item item)
        {
            if (item is Weapon s)
            {
                _playerWeapon = s;
                _attackComponent.BaseMinDamage += _playerWeapon.MinDamage;
                _attackComponent.BaseMaxDamage += _playerWeapon.MaxDamage;
                _attackComponent.CriticalStrikeChance = _playerWeapon.CriticalStrikeChance;
            }
        }

        private void PlayerDied()
        {
            GD.Print("I am dead. Game over");
        }

        private async void Move()
        {
            if (_inputDirection != Vector2.Zero)
            {
                if (!_moving && _movementPoints != 0)
                {
                    _moving = true;
                    // создаем новый tween
                    var tween = CreateTween();
                    // передаем в tween свойство, которое хотим изменить, направление и длину анимации
                    tween.TweenProperty(this, "position", Position + _inputDirection * _tileSize, 0.1f);
                    tween.TweenCallback(new Callable(this, nameof(MovingFalse)));
                    tween.TweenCallback(new Callable(this, nameof(ReduceMovementPoint)));
                    // ожидаем завершения анимации
                    await ToSignal(tween, "finished");
                }
            }
        }

        private void MovingFalse()
        {
            _moving = false;
        }

        private void ReduceMovementPoint()
        {
            _movementPoints -= 1;
            UpdateMovementBar();
        }

        private void RestoreMovementPoints()
        {
            _movementPoints = _maxMovementPoints;
            UpdateMovementBar();
        }

        private void UpdateMovementBar()
        {
            _progressBarMovement.MaxValue = _maxMovementPoints;
            var tween = CreateTween();
            tween.TweenProperty(_progressBarMovement, "value", _movementPoints, 0.5f);
        }

        private void OnPlayerEnteredZone(ResearchArea zone)
        {
            // если игрок в зоне, сохраняем ее
            _currentZone = zone;
            // GD.Print($" {_attackComponent.DealDamage()}");
        }

        private void OnPlayerExitedZone()
        {
            // если игрок покинул зону, обнуляем
            _currentZone = null;
        }

        private void TakeDamageOnAreaDamage(float damage)
        {
            _healthComponent.TakeDamage(damage);
        }

        private void ResearchCurrentZone()
        {
            // при нажатии кнопки срабатывает ивент, который вызывает этот метод
            if (_currentZone == null)
                return;
            var item = _currentZone.GetRandomResearchEvent();
            if (item != null)
            {
                item.Description();
                _inventoryComponent.AddItem(item);
            }
        }
    }
}
