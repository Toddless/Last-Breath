namespace PlaygroundTest
{
    using Playground;

    [TestClass]
    public class HealthComponentTest
    {

        private HealthComponent? _healthComponent;


        [TestInitialize]
        public void Initialize()
        {
            _healthComponent = new HealthComponent();
        }

        [TestMethod]
        public void InitializeTest()
        {
            Assert.IsNotNull(_healthComponent);
        }

        [TestMethod]
        public void MaxHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            Assert.IsTrue(_healthComponent.MaxHealth == 100);
        }

        [TestMethod]
        public void CurrentHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            Assert.IsTrue(_healthComponent.CurrentHealth == 100);
        }

        [TestMethod]
        public void HealthPercentTest()
        {
            Assert.IsNotNull(_healthComponent);
            Assert.IsTrue(_healthComponent.TotalPercentHealthIncreases == 1f);
        }

        [TestMethod]
        public void AdditionalHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            Assert.IsTrue(_healthComponent.AdditionalHealth == 0);
        }

        [TestMethod]
        public void ReduceCurrentHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth += 100;
            Assert.IsFalse(_healthComponent.CurrentHealth > _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void ReducingCurrentHealthLowerZeroTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth -= 200;
            Assert.IsFalse(_healthComponent.CurrentHealth == -100);
        }

        [TestMethod]
        public void ReducingMaxHealthReduceCurrentHealth()
        {
            Assert.IsNotNull(_healthComponent);
        }
    }
}
