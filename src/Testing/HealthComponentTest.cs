namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Playground;
    using Playground.Script.Effects.Debuffs;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives.Buffs;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class HealthComponentTest
    {
        private HealthComponent? _healthComponent;

        [TestInitialize]
        public void Initialize()
        {
            var effects = new ObservableCollection<IEffect>();
            _healthComponent = new HealthComponent(effects);
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
            Assert.IsTrue(healthBevorBuff < _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void TwoBuffsAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorBuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthBuff("Some Buff", "Empty", 0.1f, 3));
            _healthComponent.Effects?.Add(new HealthBuff("Some f", "Empty", 0.3f, 3));
            Assert.IsTrue(healthBevorBuff < _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void OneBuffAndOneDebuffAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorBuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthBuff("Some Buff", "Empty", 0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Some Debuff", "Empty", -0.1f, 3));
            Assert.IsTrue(healthBevorBuff > _healthComponent.MaxHealth);
        }


        [TestMethod]
        public void OneDebuffAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var healthBevorDebuffs = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            Assert.IsTrue(_healthComponent.MaxHealth < healthBevorDebuffs);
        }

        [TestMethod]
        public void TwoDebuffsAppliedTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            var healthAfterDebuff = _healthComponent.MaxHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            Assert.IsTrue(healthAfterDebuff == 90 && _healthComponent.MaxHealth == 70);
        }

        [TestMethod]
        public void OneDebuffRemovedTest()
        {
            Assert.IsNotNull(_healthComponent);
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            var healthBevorRemove = _healthComponent.MaxHealth;
            _healthComponent.Effects?.RemoveAt(1);
            Assert.IsTrue(healthBevorRemove < _healthComponent.MaxHealth);
        }

        [TestMethod]
        public void CurrentHealthChangedAfterDebuffApplied()
        {
            Assert.IsNotNull(_healthComponent);
            var currentHealthBevorDebuff = _healthComponent.CurrentHealth;
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.1f, 3));
            _healthComponent.Effects?.Add(new HealthDebuf("Crushed Bones", "Empty", -0.2f, 3));
            Assert.IsTrue(currentHealthBevorDebuff > _healthComponent.CurrentHealth);
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
        public void PoisonEffectTest()
        {
            Assert.IsNotNull(_healthComponent);
            var poison = new PoisonEffect(string.Empty, string.Empty, 15, 2);
            _healthComponent.Effects?.Add(poison);
            for (int i = 0; i < 2; i++)
                _healthComponent.HandleAppliedEffects();
            Assert.IsTrue(_healthComponent.CurrentHealth == 70);
        }

        [TestMethod]
        public void BleedEffectTest()
        {
            Assert.IsNotNull(_healthComponent);
            var bleed = new BleedEffect(string.Empty, string.Empty, 15, 2);
            _healthComponent.Effects?.Add(bleed);
            for (int i = 0; i < 2; i++)
                _healthComponent.HandleAppliedEffects();
            Assert.IsTrue(_healthComponent.CurrentHealth == 70);
        }

        [TestMethod]
        public void EffectDeletedAfterNoDurationLeftTest()
        {
            Assert.IsNotNull(_healthComponent);
            var buff = new StrikeDamageBuff(string.Empty, string.Empty, 15, 2);
            for (int i = 0; i < 2; i++)
                _healthComponent.HandleAppliedEffects();
            Assert.IsTrue(_healthComponent.Effects?.Count == 0);
        }

        [TestMethod]
        public void TwoDifferentEffectsDealDamageTest()
        {
            Assert.IsNotNull(_healthComponent);
            var bleed = new BleedEffect(string.Empty, string.Empty, 15, 2);
            var poison = new PoisonEffect(string.Empty, string.Empty, 15, 2);
            _healthComponent.Effects?.Add(bleed);
            _healthComponent.Effects?.Add(poison);
            for (int i = 0; i < 2; i++)
                _healthComponent.HandleAppliedEffects();
            Assert.IsTrue(_healthComponent.CurrentHealth == 40);
        }

        [TestMethod]
        public void TwoSimilarEffectsDealDamageAndRemovedTest()
        {
            Assert.IsNotNull(_healthComponent);
            var firstPoison = new PoisonEffect(string.Empty, string.Empty, 15, 2);
            var secondPoison = new PoisonEffect(string.Empty, string.Empty, 15, 2);
            _healthComponent.Effects?.Add(firstPoison);
            _healthComponent.Effects?.Add(secondPoison);
            for(int i = 0;i < 2;i++)
                _healthComponent.HandleAppliedEffects();

            Assert.IsTrue(_healthComponent.CurrentHealth == 40);
            Assert.IsTrue(_healthComponent.Effects?.Count == 0);

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
