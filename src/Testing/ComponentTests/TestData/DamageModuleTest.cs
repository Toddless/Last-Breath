namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using LastBreath.Script.BattleSystem.Module;

    public class DamageModuleTest(float value) : IStatModule
    {
        private float _value = value;
        public StatModule SkillType => StatModule.Damage;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _value;
    }
}
