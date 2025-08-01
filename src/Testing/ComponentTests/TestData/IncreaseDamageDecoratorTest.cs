namespace LastBreathTest.ComponentTests.TestData
{
    using Contracts.Enums;
    using LastBreath.Script.BattleSystem.Decorators;

    public class IncreaseDamageDecoratorTest(DecoratorPriority priority, float value) : StatModuleDecorator(type: StatModule.Damage, priority)
    {
        private float _value = value;
        public override float GetValue() => base.GetValue() * _value;
    }
}
