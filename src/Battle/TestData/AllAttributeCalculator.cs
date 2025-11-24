namespace Battle.TestData
{
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    public class AllAttributeCalculator : IParameterCalculator
    {
        private readonly int[] _parameters = [(int)EntityParameter.Dexterity, (int)EntityParameter.Strength, (int)EntityParameter.Intelligence];

        public IEnumerable<(EntityParameter Parameter, float Value)> Calculate(float baseValue, IReadOnlyList<IModifierInstance> modifiers,
            Func<EntityParameter, IReadOnlyList<IModifierInstance>> readParameterModifiers,
            Func<EntityParameter, float> readBaseValue)
        {
            foreach (int parameter in _parameters)
            {
                var parameterEnum = (EntityParameter)parameter;
                yield return (parameterEnum, Calculations.CalculateFloatValue([..modifiers.Concat(readParameterModifiers(EntityParameter.Dexterity))]));
            }
        }
    }
}
