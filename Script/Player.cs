namespace Playground
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Inventory;
    using Playground.Script.Items;
    using System;

    public partial class Player : CharacterBody2D
    {
        [Export]
        private RestorePlayerMovement _button;
        [Export]
        private ProgressBar _progressBarMovement;
        [Export]
        private ResearchButton _researchButton;
        [Export]
        private Weapon playerWeapon;
        [Export]
        private BodyArmor playerArmor;
        [Export]
        private GridContainer inventory;
        private AnimationTree animationTree;
        private Inventory playerInventory;
        private const int TileSize = 64;
        private bool moving = false;
        private Vector2 inputDirection = Vector2.Zero;
        private double movementPoints;
        private double MaxMovementPoints = 15;
        // здесь хранится зона в которой находится игрок
        private ZoneToResearch _currentZone;


        public Inventory PlayerInventory
        {
            get => playerInventory;
            private set => playerInventory = value;
        }

        public Weapon PlayerWeapon
        {
            get => playerWeapon;
            set => playerWeapon = value;
        }

        public BodyArmor PlayerArmor
        {
            get => playerArmor;
            set => playerArmor = value;
               
        }

        public override void _Ready()
        {
            movementPoints = MaxMovementPoints;
            _button.Pressed += RestoreMovementPoints;
            playerInventory = new(30);
            inventory = playerInventory;

            // связываем зоны с сигналом
            foreach (ZoneToResearch zone in GetTree().GetNodesInGroup("ZonesToResearch"))
            {
                zone.Connect(nameof(ZoneToResearch.OnPlayerEnteredZone), new Callable( this, nameof(OnPlayerEnteredZone)));
            }
            // подписываемся на эвент и вызываем метод
            _researchButton.Pressed += ResearchCurrentZone;
        }

        public override void _Input(InputEvent @event)
        {
            Movement(@event);
        }

        private void Movement(InputEvent @event)
        {
            if (Input.IsActionJustPressed("ui_down"))
            {
                inputDirection = Vector2.Down;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_up"))
            {
                inputDirection = Vector2.Up;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_left"))
            {
                inputDirection = Vector2.Left;
                Move();
            }
            else if (Input.IsActionJustPressed("ui_right"))
            {
                inputDirection = Vector2.Right;
                Move();
            }
        }

        private async void Move()
        {
            if (inputDirection != Vector2.Zero)
            {
                if (!moving && movementPoints != 0)
                {
                    moving = true;
                    var tween = CreateTween();
                    tween.TweenProperty(this, "position", Position + inputDirection * TileSize, 0.25f);
                    tween.TweenCallback(new Callable(this, nameof(MovingFalse)));
                    tween.TweenCallback(new Callable(this, nameof(ReduceMovementPoint)));
                    await ToSignal(tween, "finished");
                }
            }
        }

        private void MovingFalse()
        {
            moving = false;
        }

        private void ReduceMovementPoint()
        {
            movementPoints -= 1;
            UpdateMovementBar();
        }

        private void RestoreMovementPoints()
        {
            movementPoints = MaxMovementPoints;
            UpdateMovementBar();
        }

        private void UpdateMovementBar()
        {
            _progressBarMovement.MaxValue = MaxMovementPoints;
            var tween = CreateTween();
            tween.TweenProperty(_progressBarMovement, "value", movementPoints, 0.5f);
        }

        private void OnPlayerEnteredZone(ZoneToResearch zone)
        {
            // если игрок в зоне, сохраняем ее
            _currentZone = zone;
            GD.Print($"Player is in Zone: {zone}");
        }

        private void ResearchCurrentZone()
        {
            // при нажатии кнопки срабатывает ивент, который вызывает этот метод
            // который вызывает метод зоны
            var item = _currentZone.AddEvents();
            if( item != null )
            {
                playerInventory.AddItem(item);
            }
        }
    }
}
