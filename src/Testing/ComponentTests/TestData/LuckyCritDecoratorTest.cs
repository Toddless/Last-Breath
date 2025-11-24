namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class LuckyCritDecoratorTest(DecoratorPriority priority) : EntityParameterModuleDecorator(parameter: EntityParameter.CriticalChance, priority, "LuckyCritDecorator")
    {
        public override float GetValue()
        {
            var firstRoll = base.GetValue();
            var secondRoll = base.GetValue();
            return MathF.Max(firstRoll, secondRoll);
        }
    }
}
