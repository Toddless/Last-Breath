namespace Playground.Script.StateMachine
{
    using Stateless;

    public class StateExamlpe
    {

        enum Trigger
        {
            None,
            Trigger,
            FirstTrigger,
            SecondTrigger
        }

        enum State
        {
            Idle,
            Moving,
            Fight
        }

        State _state = State.Idle;

        StateMachine<State, Trigger>? _machine;
        StateMachine<State, Trigger>.TriggerWithParameters<int> _dangerLevel;
        StateMachine<State, Trigger>.TriggerWithParameters<string> _fraction;


        public StateExamlpe()
        {
            _machine = new StateMachine<State, Trigger>(()=> _state, s=> _state = s);

            _dangerLevel = _machine.SetTriggerParameters<int>(Trigger.SecondTrigger);
            _fraction = _machine.SetTriggerParameters<string>(Trigger.FirstTrigger);

        }
    }
}
