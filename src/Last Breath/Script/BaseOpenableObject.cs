namespace Playground.Script
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Script.Items;
    using Stateless;

    public partial class BaseOpenableObject : StaticBody2D
    {
        private enum Trigger { Open, Close }
        private enum State { Open, Closed }
        private readonly StateMachine<State, Trigger> _machine = new(State.Closed);
        private AnimatedSprite2D? _sprite;
        private CollisionShape2D? _collisionShape;
        private Area2D? _area;
        private bool _isPlayerNearby;

        [Export]
        public Array<Item> Items { get; set; } = [];
        public event Action<BaseOpenableObject>? OpenObject;
        public event Action? CloseObject;

        public override void _Ready()
        {
            _sprite = GetNode<AnimatedSprite2D>("Sprite2D");
            _collisionShape = GetNode<CollisionShape2D>(nameof(CollisionShape2D));
            _area = GetNode<Area2D>(nameof(Area2D));
            SetEvents();
            ConfigureMachine();
        }

        public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
        {
            if (@event is InputEventMouseButton mouse)
            {
                if (!_isPlayerNearby) return;
                if (mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
                {
                    if (_machine?.State != State.Closed) return;
                    _machine?.Fire(Trigger.Open);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        public override void _ExitTree()
        {
            if(_area != null)
            {
                _area.BodyEntered -= PlayerEnter;
                _area.BodyExited -= PlayerLeave;
            }
            CloseObject = null;
            OpenObject = null;
            base._ExitTree();
        }

        public void Close() => _machine?.Fire(Trigger.Close);

        private void ConfigureMachine()
        {
            _machine?.Configure(State.Open)
                .OnEntry(() =>
                {
                    OpenObject?.Invoke(this);
                    _sprite?.Play("Opening");
                    _sprite?.Play("Open");
                })
                .OnExit(() =>
                {
                    CloseObject?.Invoke();
                    _sprite?.Play("Closing");
                    _sprite?.Play("Closed");
                })
                .Permit(Trigger.Close, State.Closed);

            _machine?.Configure(State.Closed)
                .Permit(Trigger.Open, State.Open);
        }

        private void SetEvents()
        {
            if (_area == null) return;
            _area.BodyEntered += PlayerEnter;
            _area.BodyExited += PlayerLeave;
        }

        private void PlayerLeave(Node2D body)
        {
            if (_isPlayerNearby && body is Player)
            {
                _isPlayerNearby = false;
                if (_machine?.State == State.Open) Close();
            }
        }

        private void PlayerEnter(Node2D body)
        {
            if (body is Player)
                _isPlayerNearby = true;
        }
    }
}
