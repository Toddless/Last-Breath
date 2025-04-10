namespace Playground.Components.Interfaces
{
    using Playground.Script.Enums;

    public interface IResource
    {
        Parameter Parameter { get; }
        ResourceType Type { get; }
        float Current { get; }
        float RecoveryAmount { get; set; }

        float GetBaseRecovery();
        void Recover();
        void OnSpend(int amount);
        bool IsEnough(int amountToSpend);
    }
}
