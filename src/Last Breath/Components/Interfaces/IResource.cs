namespace LastBreath.Components.Interfaces
{
    using System;
    using LastBreath.Components;
    using LastBreath.Script.Enums;

    public interface IResource
    {
        ResourceType ResourceType { get; }
        float Current { get; }
        float MaximumAmount { get; }
        float RecoveryAmount { get; }

        event Action<float>? CurrentChanges, MaximumChanges;

        void OnSpend(int amount);
        void Recover();
        void OnParameterChanges(object? sender, ModifiersChangedEventArgs args);
    }
}
