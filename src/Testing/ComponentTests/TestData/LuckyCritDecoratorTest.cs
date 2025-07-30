namespace LastBreathTest.ComponentTests.TestData
{
    using LastBreath.Script.BattleSystem.Decorators;
    using LastBreath.Script.Enums;

    public class LuckyCritDecoratorTest(DecoratorPriority priority) : StatModuleDecorator(type: StatModule.CritChance, priority)
    {
        public override float GetValue()
        {
            var firstRoll = base.GetValue();
            var secondRoll = base.GetValue();
            return MathF.Max(firstRoll, secondRoll);
        }
    }
}
