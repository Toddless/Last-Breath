namespace LastBreathTest.ComponentTests.TestData
{
    using LastBreath.Script.BattleSystem.Decorators;
    using LastBreath.Script.Enums;

    public class AdditionalDamageDecoratorTest(DecoratorPriority priority, float value) : StatModuleDecorator(type: StatModule.Damage, priority)
    {
        private readonly float _value = value;

        public override float GetValue() => base.GetValue() + _value;
    }
}
