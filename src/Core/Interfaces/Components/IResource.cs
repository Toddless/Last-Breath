namespace Core.Interfaces.Components
{
    using System;

    public interface IResource
    {
        float Current { get; }
        float MaximumAmount { get; }
        float RecoveryAmount { get; }

        event Action<float>? CurrentChanges, MaximumChanges;

        void OnSpend(int amount);
        void Recover();
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
    }
}
