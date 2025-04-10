namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    internal class Mana : IResource
    {
        private const float BaseRecovery = 0.3f;
        public Parameter Parameter { get; } = Parameter.Mana;
        public ResourceType Type { get; } = ResourceType.Mana;
        public float Current { get; private set; }
        public float RecoveryAmount { get; set; }

        public bool IsEnough(int amountToSpend) => Current >= amountToSpend;
        public void Recover() => Current += RecoveryAmount;
        public void OnSpend(int amount) => Current -= amount;
        public float GetBaseRecovery() => BaseRecovery;
    }
}
