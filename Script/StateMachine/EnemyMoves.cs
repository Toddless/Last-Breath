namespace Playground.Script.StateMachine
{
    using Godot;

    public partial class EnemyMoves : State
    {
        private EnemyAI? _enemy;
        private Vector2 _newPosition;
        private Vector2 _direction;
        private int speed = 100;


        [Signal]
        public delegate void EnemyReachedEventHandler();

        public override void StateReady()
        {
            _enemy = GetOwner<EnemyAI>();
            this.EnemyReached += EnemyReachedDestination;
        }

        public override void Enter()
        {
            _newPosition = new Vector2(rnd.RandfRange(0, 500), rnd.RandfRange(0, 500));
            _enemy!.GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D)).Play("Bat_Legend");
            if (!_enemy.RayCast!.Enabled)
            {
                _enemy!.RayCast.Enabled = true;
            }

            _direction = (_newPosition - _enemy!.Position).Normalized();
        }

        public override void PhysicsUpdate(float delta)
        {
            _enemy!.Velocity = _direction * speed;
            var collision = _enemy.MoveAndCollide(_enemy.Velocity * delta);

            if (_enemy!.RayCast!.IsColliding())
            {
                _newPosition.Rotated(Mathf.Pi / 4);
                _enemy.Velocity = _newPosition * speed;
            }

            if (_enemy.Position.DistanceTo(_newPosition) < 1.0f)
            {
                EmitSignal(SignalName.EnemyReached);
            }
        }

        private void EnemyReachedDestination()
        {
            fsm!.TransitionTo("Idle");
        }
    }
}
