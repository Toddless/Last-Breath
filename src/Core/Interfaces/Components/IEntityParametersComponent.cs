namespace Core.Interfaces.Components
{
    using System;
    using System.Collections.Generic;
    using Decorator;
    using Enums;

    public interface IEntityParametersComponent
    {
        float Damage { get; }
        float BlockChance { get; }
        float CriticalChance { get; }
        float AdditionalHit { get; }
        float CriticalDamage { get; }
        float MulticastChance { get; }
        float SpellDamage { get; }
        float Armor { get; }
        float Evade { get; }
        float MaxBarrier { get; }
        float Mana { get; }
        float ManaRecovery { get; }
        float MoveSpeed { get; }
        float Suppress { get; }
        float MaxHealth { get; }
        float HealthRecovery { get; }

        event Action<EntityParameter, float>? ParameterChanged;

        void Initialize(Func<EntityParameter, IReadOnlyList<IModifier>> getModifiers);
        void AddModuleDecorator(EntityParameterModuleDecorator decorator);
        void RemoveModuleDecorator(string id, EntityParameter param);
        float CalculateForBase(EntityParameter parameter, float baseValue);
        void SetBaseValueForParameter(EntityParameter parameter, float baseValue);
        void OnModifiersChange(object? sender, IModifiersChangedEventArgs args);
    }
}
