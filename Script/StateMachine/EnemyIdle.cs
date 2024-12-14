namespace Playground.Script.StateMachine
{
    using Godot;
    using Playground.Script.Enums;

    public partial class EnemyIdle : State
    {
        public override void Enter()
        {
            GetNode<Timer>(nameof(Timer)).Start(rnd.RandiRange(1, 2));
            GetOwner().GetNode<AnimatedSprite2D>(nameof(AnimatedSprite2D)).Play("Bat_Myth");
        }

        public override void Exit()
        {
            GetNode<Timer>(nameof(Timer)).Stop();
        }


        private void OnTimerTimeOut()
        {
            fsm!.TransitionTo(States.Move.ToString());
        }
    }
}
