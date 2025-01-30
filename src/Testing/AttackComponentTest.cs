namespace PlaygroundTest
{
    using Moq;
    using Playground;
    using Playground.Components;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class AttackComponentTest
    {
        private AttackComponent? _attackComponent;

        [TestInitialize]
        public void Initialize()
        {
            _attackComponent = new AttackComponent(new EffectManager([]).CalculateValues);
        }
    }
}
