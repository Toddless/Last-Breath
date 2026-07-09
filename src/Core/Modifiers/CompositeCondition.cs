namespace Core.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
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

        public CompositeCondition(IReadOnlyList<ICondition> conditions, Mode mode)
        {
            _conditions = conditions;
            _mode = mode;
            foreach (ICondition condition in _conditions)
                condition.StateChanged += _ => Evaluate();
            Evaluate();
        }

        public void Attach(IFightable owner)
        {
            foreach (ICondition condition in _conditions) condition.Attach(owner);
            Evaluate();
        }

        public void Detach()
        {
            foreach (ICondition condition in _conditions) condition.Detach();
        }

        public ICondition Copy() => new CompositeCondition(_conditions.Select(condition => condition.Copy()).ToList(), _mode);

        private void Evaluate()
        {
            bool result = _mode == Mode.All ? _conditions.All(c => c.IsMet) : _conditions.Any(c => c.IsMet);
            if (result == IsMet) return;
            IsMet = result;
            StateChanged?.Invoke(IsMet);
        }
    }
}
