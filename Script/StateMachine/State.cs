namespace Playground.Script.StateMachine
{
    using Godot;

    public partial class State : Node2D
    {
        public StateMachine? fsm;
        public RandomNumberGenerator rnd = new();
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
