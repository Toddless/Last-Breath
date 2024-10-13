namespace Playground.Script.StateMachine
{
    using Godot;

    public partial class State : Node
    {
        [Export]
        private AnimatedSprite2D _animatedSprite2D;

        public AnimatedSprite2D AnimatedSprite2D
        {
            get { return _animatedSprite2D; }
        }

        public StateMachine fsm;

        public virtual void Enter()
        {
        }
        public virtual void Exit()
        {
        }
        public virtual void StateReady()
        {
        }
        public virtual void Update(float delta)
        {
        }
        public virtual void PhysicsUpdate(float delta)
        {
        }
        public virtual void HandleInput(InputEvent @event)
        {
        }
    }
}
