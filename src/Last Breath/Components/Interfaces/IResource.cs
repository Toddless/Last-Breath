namespace Playground.Components.Interfaces
{
    using Playground.Script.Enums;

    public interface IResource
    {
        ResourceType Type { get; }
        float Current { get; }
        float RecoveryAmount { get; }

        public void Recover();
        public void OnSpend(int amount);
        public bool IsEnough(int amountToSpend);
    }
}
