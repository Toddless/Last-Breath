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
            _direction = (_newPosition - _enemy!.Position).Normalized();
        }

        public override void PhysicsUpdate(float delta)
        {
            _enemy!.Velocity = _direction * speed;
            _enemy.RayCast!.TargetPosition = _enemy.Velocity / 2;
            _enemy.MoveAndCollide(_enemy.Velocity * delta, false, 1f);
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
