namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    public class Fury : IResource
    {
        private const float BaseRecovery = 0.3f;

        public Parameter Parameter { get; } = Parameter.Fury;
        public ResourceType Type { get; } = ResourceType.Fury;
        public float Current { get; private set; } = 8f;
        public float RecoveryAmount { get; set; }

        public bool IsEnough(int amountToSpend) => Current >= amountToSpend;
        public void Recover() => Current += RecoveryAmount;
        public void OnSpend(int amount) => Current -= amount;
        public float GetBaseRecovery() => BaseRecovery;
    }
}
