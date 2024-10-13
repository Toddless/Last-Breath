namespace Playground.Script.StateMachine
{
    public partial class IdleDownState : State
    {
        public override void Enter()
        {
            AnimatedSprite2D.Play("Idle_down");
        }
    }
}
