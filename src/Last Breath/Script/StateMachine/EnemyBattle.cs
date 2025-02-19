namespace Playground.Script.StateMachine
{
    using Godot;

    public partial class EnemyBattle
    {
        private BaseEnemy? _enemy;
        private Vector2 _vector2;
        public void StateReady()
        {
            _vector2 = _enemy!.Velocity;
        }


        public void Enter() => _enemy!.Velocity = Vector2.Zero;
        public void Exit() => _enemy!.Velocity = _vector2;
    }
}
