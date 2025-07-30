namespace PlaygroundTest.ComponentTests.TestData
{
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.Enums;

    public class IncreaseDamageDecoratorTest(DecoratorPriority priority, float value) : StatModuleDecorator(type: StatModule.Damage, priority)
    {
        private float _value = value;
        public override float GetValue() => base.GetValue() * _value;
    }
}
