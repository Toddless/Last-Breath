namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class DefenseComponent
    {
        private const float BaseArmor = 100f;
        private const float BaseEvade = 0f;
        private const float BaseEnergyBarrier = 0f;
        private const float BaseMaxReduceDamage = 0.7f;
        private const float BaseMaxEvade = 0.9f;

        public float Armor { get; private set; } = BaseArmor;
        public float Evade { get; private set; } = BaseEvade;
        public float EnergyBarrier { get; private set; } = BaseEnergyBarrier;
        public float MaxReduceDamage { get; private set; } = BaseMaxReduceDamage;
        public float MaxEvadeChance { get; private set; } = BaseMaxEvade;

        public void OnParameterChanges(Parameter parameter, List<IModifier> modifiers)
        {
            switch (parameter)
            {
                case Parameter.Armor:
                    Armor = Calculations.CalculateFloatValue(BaseArmor, modifiers);
                    break;
                case Parameter.Evade:
                    Evade = Calculations.CalculateFloatValue(BaseEvade, modifiers);
                    break;
                case Parameter.EnergyBarrier:
                    EnergyBarrier = Calculations.CalculateFloatValue(BaseEnergyBarrier, modifiers);
                    break;
                case Parameter.MaxReduceDamage:
                    MaxReduceDamage = Calculations.CalculateFloatValue(BaseMaxReduceDamage, modifiers);
                    break;
                case Parameter.MaxEvadeChance:
                    MaxEvadeChance = Calculations.CalculateFloatValue(BaseMaxEvade, modifiers);
                    break;
                default:
                    break;
            }
        }
    }
}
