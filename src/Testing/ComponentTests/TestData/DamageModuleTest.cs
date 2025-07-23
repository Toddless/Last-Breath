namespace PlaygroundTest.ComponentTests.TestData
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public class DamageModuleTest(float value) : IStatModule
    {
        private float _value = value;
        public StatModule Type => StatModule.Damage;
        public DecoratorPriority Priority => DecoratorPriority.Base;

        public float GetValue() => _value;
    }
}
