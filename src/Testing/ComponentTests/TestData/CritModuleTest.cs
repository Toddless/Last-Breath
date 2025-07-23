namespace PlaygroundTest.ComponentTests.TestData
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class CritModuleTest : IStatModule
    {
        private readonly Random _random = new();
        public StatModule Type => StatModule.CritChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => (float)_random.NextDouble();
    }
}
