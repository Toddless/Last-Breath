namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using LastBreath.Script.BattleSystem.Decorators;

    public class AdditionalDamageDecoratorTest(DecoratorPriority priority, float value) : StatModuleDecorator(type: StatModule.Damage, priority)
    {
        private readonly float _value = value;

        public override float GetValue() => base.GetValue() + _value;
    }
}
