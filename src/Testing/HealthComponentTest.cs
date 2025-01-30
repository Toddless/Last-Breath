namespace PlaygroundTest
{
    using Moq;
    using Playground;
    using Playground.Components;

    [TestClass]
    public class HealthComponentTest
    {
        private HealthComponent? _healthComponent;

        [TestInitialize]
        public void Initialize()
        {
            _healthComponent = new HealthComponent(new EffectManager([]).ModifierSum);
        }

        [TestMethod]
        public void ReducePercentReduceMaxHealth()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.IncreaseHealth += 0.1f;
            var healthWithAdditionalPercent = _healthComponent.MaxHealth;
            _healthComponent.IncreaseHealth -= 0.1f;
            Assert.IsTrue(healthWithAdditionalPercent > _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void CurrentHealthBiggerThanMaxHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth += 100;
            Assert.IsFalse(_healthComponent.MaxHealth < _healthComponent.CurrentHealth);
        }

        [TestMethod]
        public void ReduceAdditionalHealthReducesCurrentHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            var healthBevorReduceAdditionalHealth = _healthComponent.CurrentHealth;
            _healthComponent.AdditionalHealth -= 50;
            Assert.IsTrue(_healthComponent.CurrentHealth < healthBevorReduceAdditionalHealth);
        }

        [TestMethod]
        public void DifferenceAfterTakingDamage()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            _healthComponent.TakeDamage(20);
            var healthAfterTakingDamage = _healthComponent.CurrentHealth;
            Assert.IsTrue(_healthComponent.CurrentHealth == 180);
        }

        [TestMethod]
        public void IncreaseAdditionalHealthIncreasesCurrentHealth()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            Assert.IsTrue(_healthComponent.CurrentHealth == 200);
        }

        [TestMethod]
        public void ReducingCurrentHealthLowerZeroTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth -= 200;
            Assert.IsFalse(_healthComponent.CurrentHealth == -100);
        }

        [TestMethod]
        public void IncreaseTotalPercentIncreasesMaxHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorIncrease = _healthComponent.MaxHealth;
            _healthComponent.IncreaseHealth += 0.2f;
            Assert.IsTrue(_healthComponent.MaxHealth > healthBevorIncrease);
        }

        [TestMethod]
        public void IncreaseAdditionalHealthIncreasesMaxHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorIncrease = _healthComponent.MaxHealth;
            _healthComponent.AdditionalHealth += 150;
            Assert.IsTrue(_healthComponent.MaxHealth > healthBevorIncrease);
        }

        [TestMethod]
        public void HealCorrectAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.TakeDamage(60);
            _healthComponent.Heal(50);
            Assert.IsTrue(_healthComponent.CurrentHealth == 90);
        }

        [TestMethod]
        public void HealthHealNegativeValueTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.TakeDamage(60);
            _healthComponent.Heal(-50);
            Assert.IsTrue(_healthComponent.CurrentHealth >= 0);
        }
    }
}
