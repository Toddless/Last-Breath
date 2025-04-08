namespace Playground.Components
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

    internal class Mana : IResource
    {
        public ResourceType Type { get; } = ResourceType.Fury;
        public float Current { get; private set; }
        public float RecoveryAmount { get; set; }

        public bool IsEnough(int amountToSpend) => Current >= amountToSpend;
        public void Recover() => Current += RecoveryAmount;
        public void OnSpend(int amount) => Current -= amount;
    }
}
