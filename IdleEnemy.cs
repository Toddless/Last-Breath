namespace Playground
{
    using Godot;
    using Playground.Script.StateMachine;

    public partial class IdleEnemy : State
    {
        private Node _node;
        private AnimatedSprite2D _sprite;
        private Timer _timer;

        public override void _Ready()
        {
            _node = GetNode<Node>("/root/MainScene/Enemy/FSM/Idle");
            _sprite = GetNode<AnimatedSprite2D>("/root/MainScene/Enemy/FSM/Idle/AnimatedSprite2D");
            _timer = GetNode<Timer>("Timer");
            _timer.Timeout += OnTimeout;
        }

        public override void Enter()
        {

            _sprite.Play();
            _timer.Start();
        }

        public override void Exit()
        {

            _sprite.Stop();
            _timer.Stop();
        }

        private void OnTimeout()
        {
            fsm!.TransitionTo("Walk");
        }
    }
}

