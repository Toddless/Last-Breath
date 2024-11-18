namespace Playground.Script.StateMachine
{
    using Godot;
    using Godot.Collections;

    public partial class EnemyMoves : State
    {
        private Vector2 _targetPosition;
        private NavigationAgent2D? _navigationAgent;

        private EnemyAI? _enemy;

        private int _arrayLenght = 32;
        private float _steerForce = 0.4f;
        private int _speed = 200;
        private float _lookAhead = 1.01f;
        private float _maxRadius = 100;

        private Array<Vector2> _rayDirection = [];
        private Array<float> _interests = [];
        private Array<float> _danger = [];

        private Vector2 _chosenDirection = Vector2.Zero;
        private Vector2 _velocity = Vector2.Zero;
        private Vector2 _acceleration = Vector2.Zero;


        [Signal]
        public delegate void EnemyReachedEventHandler();

        public override void StateReady()
        {
            _enemy = GetOwner<EnemyAI>();
            _navigationAgent = _enemy.NavigationAgent2D;
            EnemyReached += EnemyReachedDestination;
        }

        public override void Enter()
        {
            _interests.Resize(_arrayLenght);
            _rayDirection.Resize(_arrayLenght);
            _danger.Resize(_arrayLenght);

            for (int i = 0; i < _arrayLenght; i++)
            {
                var angle = i * 2 * Mathf.Pi / _arrayLenght;
                _rayDirection[i] = (_enemy!.RespawnPosition.Rotated(angle) - _enemy.RespawnPosition).Normalized();
            }

            SetRandomTarget();
        }

        private void SetRandomTarget()
        {
            const float radius = 150.0f;
            do
            {
                Vector2 randomOffset = GetRandomPointInCircle(radius);
                _targetPosition = _enemy.RespawnPosition + randomOffset;
            } while (_targetPosition.DistanceTo(_enemy.Position) < 20.0f);
        }

        private Vector2 GetRandomPointInCircle(float radius)
        {


            float angle = rnd.RandfRange(0, 2 * Mathf.Pi);
            float distance = rnd.RandfRange(0, radius);

            return new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
        }

        public override void PhysicsUpdate(float delta)
        {
            SetInterests();
            SetDanger();
            ChoseDirection();
            QueueRedraw();
            var desiredVelocity = _chosenDirection * _speed;
            _enemy!.Velocity = _velocity.Lerp(desiredVelocity, _steerForce);
            _enemy!.Rotation = _velocity.Angle();
            var collide = _enemy.MoveAndCollide(_enemy.Velocity * delta);

            if(collide != null)
            {
                SetRandomTarget();
            }

            if (_enemy.Position.DistanceTo(_targetPosition) <= 10.0f)
            {
                GD.Print("Point reached");
                EmitSignal(SignalName.EnemyReached);
            }

            GD.Print($"Target Position: {_targetPosition}");
            GD.Print($"Current Position: {_enemy.Position}");
        }

        public override void _Draw()
        {
            DrawCircle(_enemy!.Position, _maxRadius, new Color(0.2f, 0.8f, 0.2f, 0.5f));

            for (int i = 0; i < _danger.Count; i++)
            {
                var color = _danger[i] == 1 ? new Color(0.545098f, 0, 0, 1) : new Color(0.133333f, 0.545098f, 0.133333f, 1);
                DrawLine(_enemy!.Position, _enemy.Position + _rayDirection[i] * _lookAhead, color);
            }
        }

        private void ChoseDirection()
        {
            for (int i = 0; i < _arrayLenght; i++)
            {
                if (_danger[i] > 0)
                {
                    _interests[i] = 0.0f;
                }
            }
            _chosenDirection = Vector2.Zero;
            for (int i = 0; i < _arrayLenght; i++)
            {
                _chosenDirection += _rayDirection[i] * _interests[i];
            }

            if (_chosenDirection.Length() > 0.01f)
            {
                _chosenDirection = _chosenDirection.Normalized();
            }
            else
            {
                _chosenDirection = (_targetPosition - _enemy.Position).Normalized(); // Прямое направление
            }
        }

        private void SetDanger()
        {
            var spaceState = _enemy!.GetWorld2D().DirectSpaceState;

            for (int i = 0; i < _arrayLenght; i++)
            {
                var start = _enemy.Position;
                var end = start + _rayDirection[i] * _lookAhead;
                var query = PhysicsRayQueryParameters2D.Create(start, end, 1, [_enemy.GetRid()]);
                var result = spaceState.IntersectRay(query);
                _danger[i] = result.Count != 0 ? 1.0f : 0.0f;
            }
        }

        private void SetInterests()
        {
            var directionToTarget = (_targetPosition - _enemy.Position).Normalized();

            for (int i = 0; i < _arrayLenght; i++)
            {
                var d = _rayDirection[i].Dot(directionToTarget);
                _interests[i] = Mathf.Max(0, d);
            }
        }

        private void SetDefaultInterests()
        {
            for (int i = 0; i < _arrayLenght; i++)
            {
                var d = _rayDirection[i].Rotated(_enemy!.Rotation).Dot(_enemy.Transform.X);
                _interests[i] = Mathf.Max(0, d);
            }
        }

        private void EnemyReachedDestination()
        {
            fsm!.TransitionTo("Idle");
        }
    }
}
