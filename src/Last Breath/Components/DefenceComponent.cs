namespace Playground.Components
{
    using Playground.Script.Enums;

    public class DefenseComponent
    {
        private const float BaseArmor = 100f;
        private const float BaseDodge = 1f;
        private const float BaseEnergyBarrier = 0f;

        private readonly ModifierManager _modifierManager;

        public float CurrentArmor { get; private set; }
        public float CurrentDodge { get; private set; }
        public float CurrentEnergyBarrier { get; private set; }

        public DefenseComponent(ModifierManager modifierManager)
        {
            _modifierManager = modifierManager;
            _modifierManager.ParameterModifiersChanged += OnParameterChanges;
        }

        private void OnParameterChanges(Parameter parameter)
        {
            switch (parameter)
            {
                case Parameter.Armor:
                    CurrentArmor = Calculations.CalculateFloatValue(BaseArmor, _modifierManager.GetCombinedModifiers(parameter));
                    break;
                case Parameter.Dodge:
                    CurrentDodge = Calculations.CalculateFloatValue(BaseDodge, _modifierManager.GetCombinedModifiers(parameter));
                    break;
                case Parameter.EnergyBarrier:
                    CurrentEnergyBarrier = Calculations.CalculateFloatValue(BaseEnergyBarrier, _modifierManager.GetCombinedModifiers(parameter));
                    break;
                default:
                    break;
            }
        }
    }
}
