namespace Playground
{
    using Godot;
    using Godot.Collections;
    using Playground.Script;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class Player : CharacterBody2D
    {
        [Export]
        private RestorePlayerMovement _restoreMovementButton;
        [Export]
        private Weapon _playerWeapon;
        [Export]
        private BodyArmor _playerArmor;
        [Export]
        private AnimationTree _animationTree;
        [Export]
        private Inventory _playerInventory;
        private const int _tileSize = 64;
        private bool _moving = false;
        private Vector2 _inputDirection = Vector2.Zero;
        private double _movementPoints;
        private double _maxMovementPoints = 15;
        // здесь хранится зона в которой находится игрок
        private ZoneToResearch _currentZone;

        #region Combat
        [Export]
        private bool _isPlayer;
        [Export]
        private float _currentHealth;
        [Export]
        private float _maxHealth = 100;
        [Export]
        private Array _combatAction;
        [Export]
        private Node _opponent;
        #endregion

        #region UI
        private ProgressBar _healthBar;
        [Export]
        private ProgressBar _progressBarMovement;
        [Export]
        private ResearchButton _researchButton;
        private Label _healthBarText;
        #endregion

        public Weapon PlayerWeapon
        {
            get => _playerWeapon;
            set => _playerWeapon = value;
        }

        public BodyArmor PlayerArmor
        {
            get => _playerArmor;
            set => _playerArmor = value;

        }

        public override void _Ready()
        {
            _movementPoints = _maxMovementPoints;
            _restoreMovementButton.Pressed += RestoreMovementPoints;
            _researchButton.Pressed += ResearchCurrentZone;
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

        private void UpdateHealthBar()
        {
            // обновляем значение healthBar
            _healthBar.Value = _currentHealth;
            // обновляем текст
            _healthBarText.Text = _currentHealth.ToString();
        }

        private void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            // если здоровье меньше 0
            if (_currentHealth < 0)
                // вызываем метод GameOver
                QueueFree();
            UpdateHealthBar();
        }

        private void Heal(float amount)
        {
            _currentHealth += amount;
            if (_currentHealth > _maxHealth)
                // если здоровье больше максимального, присваиваем максимальное значение
                _currentHealth = _maxHealth;
            UpdateHealthBar();
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
                    tween.TweenProperty(this, "position", Position + _inputDirection * _tileSize, 0.25f);
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

        private void OnPlayerEnteredZone(ZoneToResearch zone)
        {
            // если игрок в зоне, сохраняем ее
            _currentZone = zone;
            GD.Print($"Player is in area to research");
        }

        private void OnPlayerExitedZone()
        {
            // если игрок покинул зону, обнуляем
            _currentZone = null;
            GD.Print("Player left the area");
        }

        private void ResearchCurrentZone()
        {
            // при нажатии кнопки срабатывает ивент, который вызывает этот метод
            if (_currentZone == null)
                return;
            var item = _currentZone.ResearchArea();
            if (item != null)
            {
                item.Description();
                _playerInventory.AddItem(item);
            }
        }
    }
}
