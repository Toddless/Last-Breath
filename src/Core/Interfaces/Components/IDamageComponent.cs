namespace Core.Interfaces.Components
{
    using System;
    using Enums;

    public interface IDamageComponent
    {
        float AdditionalHit { get; }
        float CriticalChance { get; }
        float CriticalDamage { get; }
        float Damage { get; }
        float SpellDamage { get; }

        event Action<EntityParameter, float> ParameterChanged;

        float CalculateForBase(EntityParameter parameter, float baseValue);
    }
}
