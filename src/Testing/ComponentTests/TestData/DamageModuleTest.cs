namespace LastBreathTest.ComponentTests.TestData
{
    using Core.Enums;
    using Core.Interfaces.Components.Module;

    public class DamageModuleTest(float value) : IParameterModule<EntityParameter>
    {
        private float _value = value;
        public EntityParameter Parameter => EntityParameter.Damage;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _value;
        public float ApplyDecoratorsForValue(float value) => value;
    }
}
