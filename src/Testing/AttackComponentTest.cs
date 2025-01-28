namespace PlaygroundTest
{
    using Playground;
    using Playground.Components.EffectTypeHandlers;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class AttackComponentTest
    {
        private AttackComponent? _attackComponent;

        [TestInitialize]
        public void Initialize()
        {
            _attackComponent = new AttackComponent([], new EffectHandlerFactory());
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
            var maxAttackBevorDebuff = _attackComponent.CurrentMaxDamage;
            var minAttackBevorDebuff = _attackComponent.CurrentMinDamage;
            var debuff = new StrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3);
            _attackComponent.Effects?.Add(debuff);
            Assert.IsTrue(maxAttackBevorDebuff > _attackComponent.CurrentMaxDamage);
            Assert.IsTrue(minAttackBevorDebuff > _attackComponent.CurrentMinDamage);
        }

        [TestMethod]
        public void TwoDifferentDebuffsAppliedTest()
        {
            Assert.IsNotNull(_attackComponent);
            var criticalStrikeChanceBevorDebuff = _attackComponent.CurrentCriticalChance;
            var criticalStrikeDamageBevorDebuff = _attackComponent.CurrentCriticalDamage;
            var additionalStrikeChanceBevorDebuff = _attackComponent.CurrentExtraHitChance;

            var criticalStrikeChanceDebuff = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);
            var criticalStrikeDamageDebuff = new CriticalStrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3);
            var additionalStrikeChanceDebuff = new AdditionalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);

            _attackComponent.Effects?.Add(criticalStrikeDamageDebuff);
            _attackComponent.Effects?.Add(criticalStrikeChanceDebuff);
            _attackComponent.Effects?.Add(additionalStrikeChanceDebuff);

            Assert.IsTrue(criticalStrikeChanceBevorDebuff > _attackComponent.CurrentCriticalChance);
            Assert.IsTrue(criticalStrikeDamageBevorDebuff > _attackComponent.CurrentCriticalDamage);
            Assert.IsTrue(additionalStrikeChanceBevorDebuff > _attackComponent.CurrentExtraHitChance);
        }

        [TestMethod]
        public void TwoSimilarDebuffsAppliedTest()
        {
            Assert.IsNotNull( _attackComponent );

            var criticalStrikeDebuffFromFristAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.05f, 3);
            var criticalStrikeDebuffFromSecondAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.01f, 3);
            _attackComponent.Effects?.Add(criticalStrikeDebuffFromFristAbility);
            var criticalStrikeChanceBevorSecondDebuff = _attackComponent.CurrentCriticalChance;
            _attackComponent.Effects?.Add(criticalStrikeDebuffFromSecondAbility);

            Assert.IsTrue(criticalStrikeChanceBevorSecondDebuff > _attackComponent.CurrentCriticalChance);
        }

        [TestMethod]
        public void GettingDebuffedPropertyHaveCorrectValue()
        {
            Assert.IsNotNull(_attackComponent);
            var criticalStrikeDebuffFromFristAbility = new CriticalStrikeChanceDebuff(string.Empty, string.Empty, -0.05f, 3);
            var firstGet = _attackComponent.CurrentCriticalChance;
            var secondGet = _attackComponent.CurrentCriticalChance;
            var thirdGet = _attackComponent.CurrentCriticalChance;
        }
    }
}
