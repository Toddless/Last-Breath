namespace Core.Interfaces.Components
{
    using System;

    public interface IDefenceComponent
    {
        event Action<float>? CurrentBarrierChanged;
        float Armor { get; }
        float MaxBarrier { get; }
        float Evade { get; }
        float MaxEvadeChance { get; }
        float MaxReduceDamage { get; }
        float CurrentBarrier { get; }

        float BarrierAbsorbDamage(float amount);
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
    }
}
