namespace LastBreathTest.ComponentTests.TestData
{
    using LastBreath.Script.BattleSystem.Module;
    using LastBreath.Script.Enums;

    public class CritModuleTest : IStatModule
    {
        private readonly Random _random = new();
        public StatModule SkillType => StatModule.CritChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => (float)_random.NextDouble();
    }
}
