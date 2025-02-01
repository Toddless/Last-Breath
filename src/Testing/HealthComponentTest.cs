namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Moq;
    using Playground;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

    [TestClass]
    public class HealthComponentTest
    {
        private HealthComponent? _healthComponent;

        [TestInitialize]
        public void Initialize()
        {
            var mock = new Mock<EffectManager>(new Mock<ObservableCollection<IEffect>>().Object).Object;
            _healthComponent = new HealthComponent(mock.CalculateValues);
        }

        [TestMethod]
        public void ReducePercent_ReduceMaxHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.IncreaseHealth += 0.1f;
            var healthWithAdditionalPercent = _healthComponent.MaxHealth;
            _healthComponent.IncreaseHealth -= 0.1f;
            Assert.IsTrue(healthWithAdditionalPercent > _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void CurrentHealth_BiggerThanMaxHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth += 100;
            Assert.IsFalse(_healthComponent.MaxHealth < _healthComponent.CurrentHealth);
        }

        [TestMethod]
        public void ReduceAdditionalHealth_ReducesCurrentHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            var healthBevorReduceAdditionalHealth = _healthComponent.CurrentHealth;
            _healthComponent.AdditionalHealth -= 50;
            Assert.IsTrue(_healthComponent.CurrentHealth < healthBevorReduceAdditionalHealth);
        }

        [TestMethod]
        public void Difference_AfterTakingDamage_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            _healthComponent.TakeDamage(20);
            var healthAfterTakingDamage = _healthComponent.CurrentHealth;
            Assert.IsTrue(_healthComponent.CurrentHealth == 180);
        }

        [TestMethod]
        public void IncreaseAdditionalHealth_IncreasesCurrentHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.AdditionalHealth += 100;
            Assert.IsTrue(_healthComponent.CurrentHealth == 200);
        }

        [TestMethod]
        public void ReducingCurrentHealth_LowerZero_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.CurrentHealth -= 200;
            Assert.IsFalse(_healthComponent.CurrentHealth == -100);
        }

        [TestMethod]
        public void IncreaseTotalPercent_IncreasesMaxHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorIncrease = _healthComponent.MaxHealth;
            _healthComponent.IncreaseHealth += 0.2f;
            Assert.IsTrue(_healthComponent.MaxHealth > healthBevorIncrease);
        }

        [TestMethod]
        public void IncreaseAdditionalHealth_IncreasesMaxHealth_Test()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorIncrease = _healthComponent.MaxHealth;
            _healthComponent.AdditionalHealth += 150;
            Assert.IsTrue(_healthComponent.MaxHealth > healthBevorIncrease);
        }

        [TestMethod]
        public void Heal_CorrectApplied_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.TakeDamage(60);
            _healthComponent.Heal(50);
            Assert.IsTrue(_healthComponent.CurrentHealth == 90);
        }

        [TestMethod]
        public void Heal_NegativeValue_Test()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.TakeDamage(60);
            _healthComponent.Heal(-50);
            Assert.IsTrue(_healthComponent.CurrentHealth >= 0);
        }
    }
}
