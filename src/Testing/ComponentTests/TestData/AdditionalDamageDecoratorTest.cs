namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class AdditionalDamageDecoratorTest(DecoratorPriority priority, float value)
        : StatModuleDecorator(abilityParameter: EntityParameter.Damage, priority, "AdditionalDamageDecorator")
    {
        public override float GetValue() => base.GetValue() + value;

        public override float ApplyDecorators(float baseValue) => base.ApplyDecorators(baseValue) + value;
    }
}
