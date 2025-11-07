namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public class CritModuleTest : IParameterModule
    {
        private readonly Random _random = new();
        public Parameter Parameter => Parameter.CriticalChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => (float)_random.NextDouble();
    }
}
