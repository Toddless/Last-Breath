namespace Playground
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class Player : CharacterBody2D
    {

        #region Private fields
        private Vector2 _inputDirection = Vector2.Zero;
        private double _maxMovementPoints = 15;
        private ResearchArea _currentZone;
        private const int _tileSize = 64;
        private double _movementPoints;
        private bool _moving = false;
        // здесь хранится зона в которой находится игрок
        #endregion

        #region Export fields
        [Export]
        private RestorePlayerMovement _restoreMovementButton;
        [Export]
        private AnimationTree _animationTree;
        [Export]
        private InventoryComponent _playerInventory;
        [Export]
        private BodyArmor _playerArmor;
        [Export]
        private Weapon _playerWeapon;

        #endregion

        #region Components
        private HealthComponent _healthComponent;
        private AttackComponent _attackComponent;
        #endregion

        #region UI
        [Export]
        private ProgressBar _progressBarMovement;
        [Export]
        private ResearchButton _researchButton;
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
            _movementPoints = _maxMovementPoints;
            _playerInventory = GetNode<InventoryComponent>(nameof(InventoryComponent));
            _healthComponent = GetNode<HealthComponent>(nameof(HealthComponent));
            _attackComponent = GetNode<AttackComponent>(nameof(AttackComponent));
            _restoreMovementButton.Pressed += RestoreMovementPoints;
            _researchButton.Pressed += ResearchCurrentZone;
            _healthComponent.OnCharacterDied += PlayerDied;
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed("ui_down"))
            {
                _inputDirection = Vector2.Down;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_up"))
            {
                _inputDirection = Vector2.Up;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_left"))
            {
                _inputDirection = Vector2.Left;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_right"))
            {
                _inputDirection = Vector2.Right;
                Move();
            }
        }

        private void OnEquipWeapon(Weapon weapon)
        {
            _attackComponent.BaseMinDamage += weapon.MinDamage;
            _attackComponent.BaseMaxDamage += weapon.MaxDamage;
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
                _playerInventory.AddItem(item);
            }
        }
    }
}
