namespace Core.Modifiers.Conditions
{
    using System;
    using Entity;
    using Interfaces;

    /// <summary>Met while the owner has any barrier.</summary>
    public class BarrierPresentCondition : ICondition
    {
        private IFightable? _owner;

        public bool IsMet { get; private set; }

        public event Action<bool>? StateChanged;

        public void Attach(IFightable owner)
        {
            _owner = owner;
            owner.CurrentBarrierChanged += OnBarrierChanged;
            Evaluate();
        }

        public void Detach()
        {
            if (_owner == null) return;
            _owner.CurrentBarrierChanged -= OnBarrierChanged;
            _owner = null;
        }

        public ICondition Copy() => new BarrierPresentCondition();

        private void OnBarrierChanged(float _) => Evaluate();

        private void Evaluate()
        {
            bool met = _owner is { CurrentBarrier: > 0 };
            if (met == IsMet) return;
            IsMet = met;
            StateChanged?.Invoke(IsMet);
        }
    }
}
