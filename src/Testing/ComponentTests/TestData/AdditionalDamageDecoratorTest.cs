namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;

    public class AdditionalDamageDecoratorTest(DecoratorPriority priority, float value)
        : EntityParameterModuleDecorator(parameter: EntityParameter.Damage, priority, "AdditionalDamageDecorator")
    {
        public override float GetValue() => base.GetValue() + value;

        public override float ApplyDecoratorsForValue(float baseValue) => base.ApplyDecoratorsForValue(baseValue) + value;
    }
}
