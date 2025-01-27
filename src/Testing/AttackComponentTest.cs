namespace PlaygroundTest
{
    using Playground;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class AttackComponentTest
    {
        private AttackComponent? _attackComponent;

        [TestInitialize]
        public void Initialize()
        {
            _attackComponent = new AttackComponent();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _attackComponent?.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        public void OneDebuffAppliedTest()
        {
            Assert.IsNotNull(_attackComponent);
            var maxAttackBevorDebuff = _attackComponent.CurrentMaxStrikeDamage;
            var minAttackBevorDebuff = _attackComponent.CurrentMinStrikeDamage;
            var debuff = new StrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3);
            _attackComponent.Effects?.Add(debuff);
            Assert.IsTrue(maxAttackBevorDebuff > _attackComponent.CurrentMaxStrikeDamage);
            Assert.IsTrue(minAttackBevorDebuff > _attackComponent.CurrentMinStrikeDamage);
        }

        [TestMethod]
        public void TwoDifferentDebuffsAppliedTest()
        {
            Assert.IsNotNull(_attackComponent);
            var criticalStrikeChanceBevorDebuff = _attackComponent.CurrentCriticalStrikeChance;
            var criticalStrikeDamageBevorDebuff = _attackComponent.CurrentCriticalStrikeDamage;
            var additionalStrikeChanceBevorDebuff = _attackComponent.CurrentAdditionalStrikeChance;

            var criticalStrikeChanceDebuff = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);
            var criticalStrikeDamageDebuff = new CriticalStrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3);
            var additionalStrikeChanceDebuff = new AdditionalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);

            _attackComponent.Effects?.Add(criticalStrikeDamageDebuff);
            _attackComponent.Effects?.Add(criticalStrikeChanceDebuff);
            _attackComponent.Effects?.Add(additionalStrikeChanceDebuff);

            Assert.IsTrue(criticalStrikeChanceBevorDebuff > _attackComponent.CurrentCriticalStrikeChance);
            Assert.IsTrue(criticalStrikeDamageBevorDebuff > _attackComponent.CurrentCriticalStrikeDamage);
            Assert.IsTrue(additionalStrikeChanceBevorDebuff > _attackComponent.CurrentAdditionalStrikeChance);
        }

        [TestMethod]
        public void TwoSimilarDebuffsAppliedTest()
        {
            Assert.IsNotNull( _attackComponent );

            var criticalStrikeDebuffFromFristAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.05f, 3);
            var criticalStrikeDebuffFromSecondAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);
            _attackComponent.Effects?.Add(criticalStrikeDebuffFromFristAbility);
            var criticalStrikeChanceBevorSecondDebuff = _attackComponent.CurrentCriticalStrikeChance;
            _attackComponent.Effects?.Add(criticalStrikeDebuffFromSecondAbility);

            Assert.IsTrue(criticalStrikeChanceBevorSecondDebuff > _attackComponent.CurrentCriticalStrikeChance);
        }

        [TestMethod]
        public void GettingDebuffedPropertyHaveCorrectValue()
        {
            Assert.IsNotNull(_attackComponent);
            var criticalStrikeDebuffFromFristAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.05f, 3);
            var firstGet = _attackComponent.CurrentCriticalStrikeChance;
            var secondGet = _attackComponent.CurrentCriticalStrikeChance;
            var thirdGet = _attackComponent.CurrentCriticalStrikeChance;
        }
    }
}
