namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class ComboPoints : IResource
    {
        private const float BaseMaximumAmount = 6f;
        private const float BaseRecovery = 1f;

        public event Action<float>? CurrentChanges, MaximumChanges;

        public Parameter Parameter { get; } = Parameter.Resource;
        public ResourceType Type { get; } = ResourceType.Combopoints;
        public float Current { get; private set; } = 0;
        public float RecoveryAmount { get; set; }

        public float MaximumAmount { get; set; } = BaseMaximumAmount;

        public bool IsEnough(int amountToSpend) => Current >= amountToSpend;
        public void Recover()
        {
            Current += RecoveryAmount;
            if (Current > MaximumAmount)
                Current = MaximumAmount;
            CurrentChanges?.Invoke(Current);
        }
        public void OnSpend(int amount) => Current -= amount;
        public float GetBaseRecovery() => BaseRecovery;
        public float GetBaseMaximumAmount() => MaximumAmount;
    }
}
