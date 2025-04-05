namespace Playground.Components
{
    public class DefenseComponent(ModifierManager modifierManager) 
    {
        private const float BaseArmor = 100f;
        private const float BaseDodge = 1f;
        private const float BaseEnergyBarrier = 0f;

        private readonly ModifierManager _modifierManager = modifierManager;

        public float GetArmor() => _modifierManager.CalculateFloatValue(BaseArmor, Script.Enums.Parameter.Armor);
        public float GetDodgeChance() => _modifierManager.CalculateFloatValue(BaseDodge, Script.Enums.Parameter.Dodge);
        public float GetEnergyBarrier() => _modifierManager.CalculateFloatValue(BaseEnergyBarrier, Script.Enums.Parameter.EnergyBarrier);
    }
}
