namespace Battle.TestData
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using System.Collections.Generic;

    public interface IParameterCalculator
    {
        IEnumerable<(EntityParameter Parameter, float Value)> Calculate(float baseValue, IReadOnlyList<IModifierInstance> modifiers,
            Func<EntityParameter, IReadOnlyList<IModifierInstance>> readParameterModifiers,
            Func<EntityParameter, float> readBaseValue);
    }
}
