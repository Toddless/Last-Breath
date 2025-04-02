namespace PlaygroundTest
{
    using Playground.Components;

    [TestClass]
    public class AttackComponentTest
    {
        private DamageComponent? _attackComponent;

        //[TestInitialize]
        //public void Initialize()
        //{
        //    var mock = new Mock<EffectManager>(new Mock<ObservableCollection<IEffect>>().Object).Object;
        //    _attackComponent = new AttackComponent(mock.CalculateValues);
        //}

        //[TestMethod]
        //public void Reduce_IncreasesProperties_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseCriticalChance += -3;
        //    Assert.IsTrue(_attackComponent.IncreaseCriticalChance == 0);
        //}

        //[TestMethod]
        //public void Reduce_IncreaseExtraHitChance_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseExtraHitChance += -3;
        //    Assert.IsTrue(_attackComponent.IncreaseExtraHitChance == 0);
        //}

        //[TestMethod]
        //public void Reduce_IncreaseDamage_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseDamage += -3;
        //    Assert.IsTrue(_attackComponent.IncreaseDamage == 0);
        //}

        //[TestMethod]
        //public void Reduce_AdditionalCriticalChance_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalCriticalChance += -3;
        //    Assert.IsTrue(_attackComponent.AdditionalCriticalChance == 0);
        //}

        //[TestMethod]
        //public void Reduce_AdditionalCriticalDamage_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalCriticalDamage += -3;
        //    Assert.IsTrue(_attackComponent.AdditionalCriticalDamage == 0);
        //}

        //[TestMethod]
        //public void Reduce_AdditionalDamage_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalMinDamage += -3;
        //    _attackComponent.AdditionalMaxDamage += -3;
        //    Assert.IsTrue(_attackComponent.AdditionalMinDamage == 0);
        //    Assert.IsTrue(_attackComponent.AdditionalMaxDamage == 0);
        //}

        //[TestMethod]
        //public void Reduce_AdditionalHitChance_BelowZero_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalExtraHitChance += -3;
        //    Assert.IsTrue(_attackComponent.AdditionalExtraHitChance == 0);
        //}

        //[TestMethod]
        //public void IncreaseDamage_CorrectChanged_CurrentDamage_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseDamage += 0.3f;
        //    Assert.IsTrue(_attackComponent.CurrentMinDamage == 52);
        //    Assert.IsTrue(_attackComponent.CurrentMaxDamage == 130);
        //}

        //[TestMethod]
        //public void AdditionalDamage_CorrectChanged_CurrentDamage_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalMaxDamage += 10;
        //    _attackComponent.AdditionalMinDamage += 10;
        //    Assert.IsTrue(_attackComponent.CurrentMinDamage == 50);
        //    Assert.IsTrue(_attackComponent.CurrentMaxDamage == 110);
        //}

        //[TestMethod]
        //public void IncreaseCriticalChance_CorrectChanging_CurrentCriticalChance_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseCriticalChance += 0.1f;
        //    Assert.IsTrue(_attackComponent.CurrentCriticalChance > 0.05f);
        //}

        //[TestMethod]
        //public void AdditionalCriticalDamage_CorrectChanging_CurrentCriticalDamage_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalCriticalDamage += 0.1f;
        //    Assert.IsTrue(_attackComponent.CurrentCriticalDamage > 1.5f);
        //}

        //[TestMethod]
        //public void IncreaseExtraHit_CorrectChanging_CurrentExtraHitChance_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.IncreaseExtraHitChance += 0.1f;
        //    Assert.IsTrue(_attackComponent.CurrentExtraHitChance > 0.05f);
        //}

        //[TestMethod]
        //public void AdditionalExtraHit_CorrectChanging_CurrentExtraHitChance_Test()
        //{
        //    Assert.IsNotNull(_attackComponent);
        //    _attackComponent.AdditionalExtraHitChance += 0.1f;
        //    Assert.IsTrue(_attackComponent.CurrentExtraHitChance > 0.05f);
        //}
    }
}
