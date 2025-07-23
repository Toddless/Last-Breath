namespace PlaygroundTest.ComponentTests.TestData
{
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.Enums;

    public class AdditionalDamageDecoratorTest(DecoratorPriority priority, float value) : StatModuleDecorator(type: StatModule.Damage, priority)
    {
        private readonly float _value = value;

        public override float GetValue() => base.GetValue() + _value;
    }
}
