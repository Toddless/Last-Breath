namespace Core.Interfaces.Components
{
    using System;
    using Core.Enums;

    public interface IDamageComponent
    {
        float AdditionalHit { get; }
        float CriticalChance { get; }
        float CriticalDamage { get; }
        float Damage { get; }
        float SpellDamage { get; }

        event Action<Parameter, float> ParameterChanged;
    }
}
