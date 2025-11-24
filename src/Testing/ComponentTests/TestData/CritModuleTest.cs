namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public class CritModuleTest : IParameterModule<EntityParameter>
    {
        private readonly Random _random = new();
        public EntityParameter Parameter => EntityParameter.CriticalChance;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => (float)_random.NextDouble();
        public float ApplyDecoratorsForValue(float value) => value;
    }
}
