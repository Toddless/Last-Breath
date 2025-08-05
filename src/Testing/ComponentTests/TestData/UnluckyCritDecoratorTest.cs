namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using LastBreath.Script.BattleSystem.Decorators;

    public class UnluckyCritDecoratorTest(DecoratorPriority priority) : StatModuleDecorator(type: StatModule.CritChance, priority)
    {
        public override float GetValue()
        {
            var firstRoll = base.GetValue();
            var secondRoll = base.GetValue();

            return MathF.Min(firstRoll, secondRoll);
        }
    }
}
