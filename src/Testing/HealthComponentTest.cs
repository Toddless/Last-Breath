namespace PlaygroundTest
{
    using Playground;
    using Playground.Script.Passives.Buffs;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class HealthComponentTest
    {
        private HealthComponent? _healthComponent;

        [TestInitialize]
        public void Initialize()
        {
            _healthComponent = new HealthComponent();
        }

        [TestCleanup]
        public void CleanUp()
        {
            _healthComponent?.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
            Assert.IsTrue(_healthComponent.IncreaseHealth == 1f);
        }

        [TestMethod]
        public void AdditionalHealthTest()
        {
            Assert.IsNotNull(_healthComponent);
            Assert.IsTrue(_healthComponent.AdditionalHealth == 0);
        }

        [TestMethod]
        public void ReducePercentReduceMaxHealth()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.IncreaseHealth += 0.1f;
            var healthWithAdditionalPercent = _healthComponent.MaxHealth;
            var currentHealth = _healthComponent.CurrentHealth;
            _healthComponent.IncreaseHealth -= 0.1f;
            var currentHealthAfter = _healthComponent.CurrentHealth;
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
        public void OneBuffAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorBuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthBuff("Some Buff", "Empty", 0.1f, 3));
            var healthAfterBuff = _healthComponent.MaxHealth;
            Assert.IsTrue(healthBevorBuff < healthAfterBuff);
        }

        [TestMethod]
        public void TwoBuffsAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorBuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthBuff("Some Buff", "Empty", 0.1f, 3));
            _healthComponent.Effects?.Add(new HealthBuff("Some f", "Empty", 0.3f, 3));
            var healthAfterBuff = _healthComponent.MaxHealth;
            Assert.IsTrue(healthBevorBuff < healthAfterBuff);
        }

        [TestMethod]
        public void OneBuffAndOneDebuffAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorBuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthBuff("Some Buff", "Empty", 0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Some Debuff", "Empty", -0.1f, 3));
            var healthAfterBuff = _healthComponent.MaxHealth;
            Assert.IsTrue(healthBevorBuff > healthAfterBuff);
        }


        [TestMethod]
        public void OneDebuffAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorDebuffs = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            var healthAfterDebuff = _healthComponent.MaxHealth;
            Assert.IsTrue(healthAfterDebuff < healthBevorDebuffs);
        }

        [TestMethod]
        public void TwoDebuffsAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            var healthAfterDebuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            var healthAfterSecondDebuff = _healthComponent.MaxHealth;
            Assert.IsTrue(healthAfterDebuff == 90 && healthAfterSecondDebuff == 70);
        }

        [TestMethod]
        public void OneDebuffRemovedTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            var healthBevorRemove = _healthComponent.MaxHealth;
            _healthComponent.Effects?.RemoveAt(1);
            var healthAfterRemove = _healthComponent.MaxHealth;
            Assert.IsTrue(healthBevorRemove < healthAfterRemove);
        }

        [TestMethod]
        public void CurrentHealthChangedAfterDebuffApplied()
        {
            Assert.IsNotNull(_healthComponent);
            var currentHealthBevorDebuff = _healthComponent.CurrentHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            var currentHealthAfterDebuff = _healthComponent.CurrentHealth;
            Assert.IsTrue(currentHealthBevorDebuff > currentHealthAfterDebuff);
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

        [TestMethod]
        public void HealthComponentIsCollectedAfterDispose()
        {
            WeakReference? weakReference = null;

            void CreateAndDispose()
            {
                var health = new HealthComponent();
                weakReference = new WeakReference(health);
                health.Dispose();
            }

            CreateAndDispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.IsFalse(weakReference?.IsAlive ?? true, "HealthComponent was not collected");
        }

        
    }
}
