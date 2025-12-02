namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;

    public class IncreaseDamageDecoratorTest(DecoratorPriority priority, float value) : EntityParameterModuleDecorator(parameter: EntityParameter.Damage, priority, "IncreaseDamageDecorator")
    {
        private float _value = value;
        public override float GetValue() => base.GetValue() * _value;
    }
}
