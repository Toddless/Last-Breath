namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Decorator;

    public interface IEntityParametersComponent
    {
        // TODO:
        // Слишком абстрактно. Какой это урон? Оружия? Общий? решить позднее
        float Damage { get; }
        float BlockChance { get; }
        float CriticalChance { get; }
        float AdditionalHit { get; }
        float CriticalDamage { get; }
        float MulticastChance { get; }
        float SpellDamage { get; }
        float Accuracy { get; }
        float Armor { get; }
        float ArmorPenetration { get; }
        float Evade { get; }
        float MaxBarrier { get; }
        float MaxMana { get; }
        float ManaRecovery { get; }
        float MoveSpeed { get; }
        float Suppress { get; }
        float MaxHealth { get; }
        float HealthRecovery { get; }

        event Action<EntityParameter, float>? ParameterChanged;

        float GetValueForParameter(EntityParameter parameter);
        void Initialize(Func<EntityParameter, IReadOnlyList<IModifier>> getModifiers);
        void AddModuleDecorator(EntityParameterModuleDecorator decorator);
        void RemoveModuleDecorator(string id, EntityParameter param);
        float CalculateForBase(EntityParameter parameter, float baseValue);
        void SetBaseValueForParameter(EntityParameter parameter, float baseValue);
        void OnParameterModifiersChange(object? sender, IModifiersChangedEventArgs args);
    }
}
