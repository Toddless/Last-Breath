namespace Playground.Components.Interfaces
{
    using System;
    using Playground.Script.Enums;

    public interface IResource
    {
        Parameter Parameter { get; }
        ResourceType Type { get; }
        float Current { get; }
        float MaximumAmount { get; }
        float RecoveryAmount { get; set; }

        event Action<float>? CurrentChanges, MaximumChanges;

        float GetBaseRecovery();
        float GetBaseMaximumAmount();
        void Recover();
        void OnSpend(int amount);
        bool IsEnough(int amountToSpend);
    }
}
