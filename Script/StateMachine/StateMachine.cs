namespace Playground.Script.StateMachine
{
    using System;
    using System.Collections.Generic;
    using Godot;

    [GlobalClass]
    public partial class StateMachine : Node
    {
        [Export]
        public NodePath? initialState;

        private Dictionary<string, State> _states = [];

        private State? _currentState;

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

            if(_currentState == null)
            {
                ArgumentNullException.ThrowIfNull(_currentState);
            }
        }

        public override void _Process(double delta)
        {
            _currentState!.Update((float)delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            _currentState!.PhysicsUpdate((float)delta);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            _currentState!.HandleInput(@event);
        }

        public void TransitionTo(string key)
        {
            if (!_states.ContainsKey(key) || _currentState == _states[key])
                return;
            _currentState!.Exit();
            _currentState = _states[key];
            _currentState.Enter();
        }
    }
}
