namespace Battle
{
    using System;
    using Core.Interfaces.Components;

    public class ManaResource : IResource
    {
        private const float BaseRecoveryAmount = 1.0f;
        private const float BaseMaximumAmount = 1.0f;


        public event Action<float>? CurrentChanges, MaximumChanges;

        public float RecoveryAmount { get; private set; } = BaseRecoveryAmount;

        public float Current { get; private set; }

        public float MaximumAmount { get; set; } = BaseMaximumAmount;

        public void OnSpend(int amount)
        {
            if (IsEnough(amount)) Current -= amount;
        }

        public void Recover()
        {
            Current += RecoveryAmount;
            if (Current > MaximumAmount)
                Current = MaximumAmount;
        }

        public virtual void OnParameterChanges(object? sender, IModifiersChangedEventArgs args)
        {
        }

        private bool IsEnough(int amountToSpend) => Current <= amountToSpend;
    }
}
