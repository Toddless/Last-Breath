namespace Playground.Script.StateMachine
{
    using Godot;

    public partial class EnemyBattle : State
    {
        private EnemyAI? _enemy;
        private Vector2 _vector2;
        public override void StateReady()
        {
            _enemy = GetOwner<EnemyAI>();

            _vector2 = _enemy.Velocity;
        }


        public override void Enter() => _enemy.Velocity = Godot.Vector2.Zero;

        public override void Exit() => _enemy.Velocity = _vector2;
    }
}
