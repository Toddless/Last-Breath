namespace Core.Modifiers.Conditions
{
    using System;
    using Entity;
    using Interfaces;

    /// <summary>Met while the owner's health ratio is at or below (whenBelow) / at or above the threshold.
    /// Threshold is a fraction: 0.5 = 50% health.</summary>
    public class HealthPercentCondition(float threshold, bool whenBelow) : ICondition
    {
        private IFightable? _owner;

        public bool IsMet { get; private set; }

        public event Action<bool>? StateChanged;

        public void Attach(IFightable owner)
        {
            _owner = owner;
            owner.CurrentHealthChanged += OnHealthChanged;
            Evaluate();
        }

        public void Detach()
        {
            if (_owner == null) return;
            _owner.CurrentHealthChanged -= OnHealthChanged;
            _owner = null;
        }

        public ICondition Copy() => new HealthPercentCondition(threshold, whenBelow);

        private void OnHealthChanged(float _) => Evaluate();

        private void Evaluate()
        {
            if (_owner == null) return;
            float maxHealth = _owner.Parameters.MaxHealth;
            if (maxHealth <= 0) return;
            float ratio = _owner.CurrentHealth / maxHealth;
            bool met = whenBelow ? ratio <= threshold : ratio >= threshold;
            if (met == IsMet) return;
            IsMet = met;
            StateChanged?.Invoke(IsMet);
        }
    }
}
