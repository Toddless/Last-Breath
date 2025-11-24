namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class UnluckyCritDecoratorTest(DecoratorPriority priority) : EntityParameterModuleDecorator(parameter: EntityParameter.CriticalChance, priority, "UnluckyCritDecorator")
    {
        public override float GetValue()
        {
            var firstRoll = base.GetValue();
            var secondRoll = base.GetValue();

            return MathF.Min(firstRoll, secondRoll);
        }
    }
}
