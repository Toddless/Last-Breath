namespace Playground.Components
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class DefenseComponent
    {
        private const float BaseArmor = 100f;
        private const float BaseDodge = 0f;
        private const float BaseEnergyBarrier = 0f;
        private const float BaseMaxReduce = 0.7f;

        public float CurrentArmor { get; private set; } = BaseArmor;
        public float CurrentDodge { get; private set; } = BaseDodge;
        public float CurrentEnergyBarrier { get; private set; } = BaseEnergyBarrier;
        public float CurrentMaxReduce { get; private set; } = BaseMaxReduce;

        public void OnParameterChanges(Parameter parameter,List<IModifier> modifiers)
        {
            switch (parameter)
            {
                case Parameter.Armor:
                    CurrentArmor = Calculations.CalculateFloatValue(BaseArmor, modifiers);
                    GD.Print($"CurrentArmor set to: {CurrentArmor}");
                    break;
                case Parameter.Dodge:
                    CurrentDodge = Calculations.CalculateFloatValue(BaseDodge, modifiers);
                    break;
                case Parameter.EnergyBarrier:
                    CurrentEnergyBarrier = Calculations.CalculateFloatValue(BaseEnergyBarrier, modifiers);
                    break;
                case Parameter.MaxReduce:
                    CurrentMaxReduce = Calculations.CalculateFloatValue(BaseMaxReduce, modifiers);
                    break;
                default:
                    break;
            }
        }
    }
}
