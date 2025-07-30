namespace LastBreathTest.ComponentTests.TestData
{
    using LastBreath.Script.BattleSystem.Module;
    using LastBreath.Script.Enums;

    public class DamageModuleTest(float value) : IStatModule
    {
        private float _value = value;
        public StatModule SkillType => StatModule.Damage;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _value;
    }
}
