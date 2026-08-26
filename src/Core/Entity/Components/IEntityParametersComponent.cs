namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Decorator;

    public interface IEntityParametersComponent
    {
        float PhysicalDamage { get; }
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
        float BarrierRecovery { get; }
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

        /// <summary>Marks the owner's rolls on <paramref name="parameter"/> lucky or unlucky while the source lasts.
        /// Sources stack by count and opposite kinds cancel, so a fighter both blessed and cursed rolls once, fairly.</summary>
        void AddChanceLuck(EntityParameter parameter, ChanceLuck luck);
        void RemoveChanceLuck(EntityParameter parameter, ChanceLuck luck);

        /// <summary>How rolls on <paramref name="parameter"/> settle right now — what <see cref="ChanceRoll"/> asks for.</summary>
        ChanceLuck GetChanceLuck(EntityParameter parameter);
        /// <summary>The parameter counted from its own sources alone, with every pool-conversion line left
        /// out — the measure a conversion takes of the pool it takes over. Counting conversion lines would
        /// close the reading on itself: a conversion's own drain leaves it nothing to convert, and two
        /// conversions pointing at one another would feed each other without end.</summary>
        float GetUnconvertedValueForParameter(EntityParameter parameter);
        void SetBaseValueForParameter(EntityParameter parameter, float baseValue);
        void OnParameterModifiersChange(object? sender, IModifiersChangedEventArgs args);
    }
}
