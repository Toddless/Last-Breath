namespace PlaygroundTest.ComponentTests.TestData
{
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.Enums;

    public class ModuleDecoratorTest(StatModule statModule, float value) : StatModuleDecorator(statModule, priority: DecoratorPriority.Strong)
    {
        private readonly float _value = value;

        public override float GetValue() => base.GetValue() + _value;
    }
}
