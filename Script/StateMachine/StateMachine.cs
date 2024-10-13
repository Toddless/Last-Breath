namespace Playground.Script.StateMachine
{
    using System.Collections.Generic;
    using Godot;

    public partial class StateMachine : Node
    {
        [Export]
        public NodePath initialState;

        private Dictionary<string, State> _states = [];

        private State _currentState;

        public override void _Ready()
        {
            foreach (Node node in GetChildren())
            {
                if(node is State s)
                {
                    _states[node.Name] = s;
                    s.fsm = this;
                    s.StateReady();
                    s.Exit();
                }
            }

            _currentState = GetNode<State>(initialState);
            _currentState.Enter();
        }

        public override void _Process(double delta)
        {
            _currentState.Update((float)delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            _currentState.PhysicsUpdate((float)delta);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            _currentState.HandleInput(@event);
        }

        public void TransitionTo(string key)
        {
            if (!_states.TryGetValue(key, out State value) || value == _currentState)
                return;
            _currentState.Exit();
            _currentState = value;
            _currentState.Enter();
        }
    }
}
