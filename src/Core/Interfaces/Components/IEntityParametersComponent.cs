namespace Core.Interfaces.Components
{
    using System;
    using Battle.Decorator;
    using Enums;

    public interface IEntityParametersComponent
    {
        float MaxHealth { get; }
        float HealthRecovery { get; }
        float CurrentHealth { get; set; }
        float CurrentBarrier { get; set; }
        float Damage { get; }
        float CriticalChance { get; }
        float CriticalDamage { get; }
        float AdditionalHit { get; }
        float SpellDamage { get; }
        float Armor { get; }
        float Evade { get; }
        float MaxBarrier { get; }
        float MaxReduceDamage { get; }
        float MaxEvadeChance { get; }

        event Action<float>? CurrentBarrierChanged;
        event Action<float>? CurrentHealthChanged;
        event Action<EntityParameter, float>? ParameterChanged;
        void AddModuleDecorator(StatModuleDecorator decorator);
        void RemoveModuleDecorator(string id, EntityParameter param);
        float CalculateForBase(EntityParameter parameter, float baseValue);
        void OnParameterChanges(object? sender, IModifiersChangedEventArgs args);
    }
}
