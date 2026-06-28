namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class CompositeCondition : ICondition
    {
        private readonly IReadOnlyList<ICondition> _conditions;
        private readonly Mode _mode;

        public enum Mode
        {
            All,
            Any
        }

        public bool IsMet { get; private set; }

        public event Action<bool>? StateChanged;
        public void Attach(IConditionTarget target) => throw new NotImplementedException();

        public void Detach() => throw new NotImplementedException();

        public CompositeCondition(IReadOnlyList<ICondition> conditions, Mode mode)
        {
            _conditions = conditions;
            _mode = mode;
            foreach (ICondition condition in _conditions)
                condition.StateChanged += _ => Evaluate();
            Evaluate();
        }

        private void Evaluate()
        {
            bool result = _mode == Mode.All ? _conditions.All(c => c.IsMet) : _conditions.Any(c => c.IsMet);
            if (result == IsMet) return;
            IsMet = result;
            StateChanged?.Invoke(IsMet);
        }
    }
}
