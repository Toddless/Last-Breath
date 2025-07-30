namespace Playground.Script.StateMachine
{

    public partial class EnemyMoves
    {
        //private NavigationAgent2D? _navigationAgent;
        //private Vector2 _targetPosition;

        //private BaseEnemy? _enemy;

        //private int _arrayLenght = 32;
        //private float _steerForce = 0.4f;
        //private int _speed = 200;
        //private float _lookAhead = 1.01f;
        //private float _maxRadius = 250;

        //private Array<Vector2> _rayDirection = [];
        //private Array<float> _interests = [];
        //private Array<float> _danger = [];
        //List<Vector2> dangerPoints = [];
        //private System.Collections.Generic.Dictionary<Vector2, float> _pointForce = [];

        //private Vector2 _chosenDirection = Vector2.Zero;
        //private Vector2 _velocity = Vector2.Zero;
        //private Vector2 _acceleration = Vector2.Zero;


        //[Signal]
        //public delegate void EnemyReachedEventHandler();

        //public void StateReady()
        //{
        //    _enemy = GetOwner<BaseEnemy>();
        //    _navigationAgent = _enemy.NavigationAgent2D;
        //    EnemyReached += EnemyReachedDestination;
        //}

        //public void Enter()
        //{
        //    _interests.Resize(_arrayLenght);
        //    _rayDirection.Resize(_arrayLenght);
        //    _danger.Resize(_arrayLenght);

        //    for (int i = 0; i < _arrayLenght; i++)
        //    {
        //        var angle = i * 2 * Mathf.Pi / _arrayLenght;
        //        _rayDirection[i] = _enemy!.RespawnPosition.Rotated(angle);
        //    }

        //    SetRandomTarget();
        //}

        //private void SetRandomTarget()
        //{
        //    Vector2 randomOffset = GetRandomPointInCircle(_maxRadius);
        //    _targetPosition = _enemy!.RespawnPosition + randomOffset;
        //}

        //private Vector2 GetRandomPointInCircle(float radius)
        //{
        //    float angle = rnd.RandfRange(0, 2 * Mathf.Pi);
        //    float distance = rnd.RandfRange(0, radius);

        //    return new Vector2(
        //        Mathf.Cos(angle) * distance,
        //        Mathf.Sin(angle) * distance
        //    );
        //}

        //public override void PhysicsUpdate(float delta)
        //{
        //    SetInterests();
        //    SetDanger();
        //    ChoseDirection();
        //    QueueRedraw();
        //    var desiredVelocity = _chosenDirection * _speed;
        //    _enemy!.Velocity = _velocity.Lerp(desiredVelocity, _steerForce);
        //    _enemy!.Rotation = _velocity.Angle();
        //    var collide = _enemy.MoveAndCollide(_enemy.Velocity * delta);

        //    if (_enemy.Position.DistanceTo(_targetPosition) <= 10.0f)
        //    {
        //        EmitSignal(SignalName.EnemyReached);
        //    }
        //}

        //private float SetSteerForce(float dangerPounts)
        //{
        //    return dangerPounts switch
        //    {
        //        var d when d >= 80 => 0.4f,
        //        var d when d >= 50 => 0.5f,
        //        var d when d >= 35 => 0.6f,
        //        var d when d >= 30 => 0.7f,
        //        var d when d >= 25 => 0.8f,
        //        var d when d >= 20 => 0.9f,
        //        _ => 0.3f,
        //    };
        //}

        //private void ChoseDirection()
        //{
        //    for (int i = 0; i < _arrayLenght; i++)
        //    {
        //        if (_danger[i] > 0)
        //        {
        //            _interests[i] = 0.0f;
        //        }
        //    }

        //    _chosenDirection = Vector2.Zero;
        //    for (int i = 0; i < _arrayLenght; i++)
        //    {
        //        _chosenDirection += _rayDirection[i] * _interests[i];
        //    }

        //    _chosenDirection = _chosenDirection.Normalized();
        //}

        //private void SetDanger()
        //{
        //    var spaceState = _enemy!.GetWorld2D().DirectSpaceState;

        //    for (int i = 0; i < _arrayLenght; i++)
        //    {
        //        var start = _enemy.Position;
        //        var end = start + _rayDirection[i] * _lookAhead;
        //        var query = PhysicsRayQueryParameters2D.Create(start, end, 1, [_enemy.GetRid()]);
        //        var result = spaceState.IntersectRay(query);

        //        if (result.Count != 0)
        //        {
        //            _danger[i] = 1.0f;

        //            result.TryGetValue("position", out var collisionPoint);
        //            dangerPoints.Add((Vector2)collisionPoint);
        //        }
        //        else
        //        {
        //            _danger[i] = 0.0f;
        //        }
        //    }
        //}

        //private void SetInterests()
        //{
        //    var directionToTarget = (_targetPosition - _enemy!.Position).Normalized();

        //    for (int i = 0; i < _arrayLenght; i++)
        //    {
        //        var d = _rayDirection[i].Normalized().Dot(directionToTarget);
        //        _interests[i] = Mathf.Max(d, 0);
        //    }
        //}

        //public override void _Draw()
        //{
        //    DrawCircle(_enemy!.Position, _maxRadius, new Color(0.2f, 0.8f, 0.2f, 0.5f));

        //    for (int i = 0; i < _danger.Count; i++)
        //    {
        //        var color = _danger[i] == 1 ? new Color(0.545098f, 0, 0, 1) : new Color(0.133333f, 0.545098f, 0.133333f, 1);
        //        DrawLine(_enemy!.Position, _enemy.Position + _rayDirection[i] * _lookAhead, color);
        //    }
        //}


        //private void EnemyReachedDestination()
        //{
        //    fsm!.TransitionTo(States.Idle.ToString());
        //}
    }
}
