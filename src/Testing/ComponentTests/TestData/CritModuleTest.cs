namespace LastBreathTest.ComponentTests.TestData
{
    using Contracts.Enums;
    using LastBreath.Script.BattleSystem.Module;

    public class CritModuleTest : IStatModule
    {
        private readonly Random _random = new();
        public StatModule SkillType => StatModule.CritChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => (float)_random.NextDouble();
    }
}
