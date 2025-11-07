namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public class DamageModuleTest(float value) : IParameterModule
    {
        private float _value = value;
        public Parameter Parameter => Parameter.Damage;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _value;
    }
}
