namespace PlaygroundTest
{
    using Moq;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.ScenesHandlers;

    [TestClass]
    public class BattleBehaviorTest
    {
        private BattleBehavior? _behavior;
        private IBattleContext? _battleContext;
        private IConditionsFactory? _conditionsFactory;

        [TestInitialize]
        public void Initialize()
        {
            // i rly need that all
            _behavior = new BattleBehavior();

            var mockFunc = new Mock<Func<float, float, float, Parameter, float>>();
            var mockDebuf = new Mock<IEffect>();
            var mockRegeneration = new Mock<IEffect>();
            var mockAbilityRegen = new Mock<IAbility>();
            var mockAbilityDebuf = new Mock<IAbility>();

            mockAbilityRegen.Setup(x => x.Cooldown).Returns(4);
            mockAbilityRegen.Setup(x => x.Effects).Returns([mockRegeneration.Object]);

            mockAbilityDebuf.Setup(x => x.Cooldown).Returns(4);
            mockAbilityDebuf.Setup(x => x.Effects).Returns([mockDebuf.Object]);

            var mockBattleContext = new Mock<IBattleContext>();
            mockBattleContext.Setup(x => x.Self.HealthComponent).Returns(new HealthComponent(mockFunc.Object));
            mockBattleContext.Setup(x => x.Self.Abilities).Returns(SetupAbilities());
            mockBattleContext.Setup(x => x.Self.EffectManager).Returns(new EffectManager([]));
            mockBattleContext.Setup(x => x.Opponent.HealthComponent).Returns(new HealthComponent(mockFunc.Object));
            mockBattleContext.Setup(x => x.Opponent.EffectManager).Returns(new EffectManager([]));
            _battleContext = mockBattleContext.Object;

            var mockFactory = new Mock<IConditionsFactory>();
            mockFactory.Setup(x => x.SetNewConditions(mockBattleContext.Object)).Returns(SetupConditions);
            _conditionsFactory = mockFactory.Object;
            _behavior.SetDependencies(_battleContext, _conditionsFactory);
        }

        private List<IAbility> SetupAbilities()
        {
            var mockRegeneration = new Mock<IEffect>();
            var mockPoison = new Mock<IEffect>();
            var mockDebuf = new Mock<IEffect>();
            var mockCriticalChanceDebuf = new Mock<IEffect>();
            var mockCriticalChanceBuff = new Mock<IEffect>();
            var mockDamageDebufEffect = new Mock<IEffect>();
            var mockDamageBufEffect = new Mock<IEffect>();
            var mockHealth = new Mock<IEffect>();
            var mockAttackChanceBuff = new Mock<IEffect>();
            var mockAttackChanceDebuff = new Mock<IEffect>();
            var mockCriticalDamageDebuffEffect = new Mock<IEffect>();
            var mockCriticalDamageBuffEffect = new Mock<IEffect>();


            var mockPoisonAbility = new Mock<IAbility>();
            var mockHealthBuff = new Mock<IAbility>(MockBehavior.Loose);
            var mockAttackDebuff = new Mock<IAbility>(MockBehavior.Loose);
            var mockAttackBuff = new Mock<IAbility>(MockBehavior.Loose);
            var mockCriticalChanceDebufAbility = new Mock<IAbility>(MockBehavior.Loose);
            var mockCriticalChanceBuffAbility = new Mock<IAbility>(MockBehavior.Loose);
            var mockDamageDebuf = new Mock<IAbility>(MockBehavior.Loose);
            var mockDamageBuff = new Mock<IAbility>(MockBehavior.Loose);
            var mockRegen = new Mock<IAbility>(MockBehavior.Loose);
            var mockCriticalDamageDebuf = new Mock<IAbility>(MockBehavior.Loose);
            var mockCriticalDamageBuff = new Mock<IAbility>(MockBehavior.Loose);

            mockHealth.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockHealth.Setup(x => x.Parameter).Returns(Parameter.Health);
            mockHealth.Setup(x => x.Modifier).Returns(0.1f);

            mockAttackChanceBuff.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockAttackChanceBuff.Setup(x => x.Parameter).Returns(Parameter.StrikeDamage);
            mockAttackChanceBuff.Setup(x => x.Modifier).Returns(0.15f);

            mockCriticalChanceDebuf.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockCriticalChanceDebuf.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeChance);
            mockCriticalChanceDebuf.Setup(x => x.Modifier).Returns(-0.1f);

            mockCriticalChanceBuff.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockCriticalChanceBuff.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeChance);
            mockCriticalChanceBuff.Setup(x => x.Modifier).Returns(0.1f);

            mockAttackChanceDebuff.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockAttackChanceDebuff.Setup(x => x.Parameter).Returns(Parameter.AdditionalStrikeChance);
            mockAttackChanceDebuff.Setup(x => x.Modifier).Returns(-0.2f);

            mockCriticalDamageBuffEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockCriticalDamageBuffEffect.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeDamage);
            mockCriticalDamageBuffEffect.Setup(x => x.Modifier).Returns(0.1f);

            mockCriticalDamageDebuffEffect.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockCriticalDamageDebuffEffect.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeDamage);
            mockCriticalDamageDebuffEffect.Setup(x => x.Modifier).Returns(-0.1f);

            mockDamageDebufEffect.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockDamageDebufEffect.Setup(x => x.Parameter).Returns(Parameter.StrikeDamage);
            mockDamageDebufEffect.Setup(x => x.Modifier).Returns(-0.3f);

            mockDamageBufEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockDamageBufEffect.Setup(x => x.Parameter).Returns(Parameter.StrikeDamage);
            mockDamageBufEffect.Setup(x => x.Modifier).Returns(0.1f);


            mockRegeneration.Setup(x => x.EffectType).Returns(EffectType.Regeneration);
            mockRegeneration.Setup(x => x.Parameter).Returns(Parameter.Health);
            mockRegeneration.Setup(x => x.Modifier).Returns(50);

            mockPoison.Setup(x => x.EffectType).Returns(EffectType.Poison);
            mockPoison.Setup(x => x.Modifier).Returns(10);

            mockDebuf.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockDebuf.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeDamage);
            mockDebuf.Setup(x => x.Modifier).Returns(-0.3f);

            mockPoisonAbility.Setup(x => x.Effects).Returns([mockPoison.Object, mockCriticalChanceDebuf.Object]);
            mockHealthBuff.Setup(x => x.Effects).Returns([mockHealth.Object]);
            mockAttackDebuff.Setup(x => x.Effects).Returns([mockAttackChanceDebuff.Object]);
            mockAttackBuff.Setup(x => x.Effects).Returns([mockAttackChanceBuff.Object]);
            mockCriticalChanceDebufAbility.Setup(x => x.Effects).Returns([mockCriticalChanceDebuf.Object]);
            mockCriticalChanceBuffAbility.Setup(x => x.Effects).Returns([mockCriticalChanceBuff.Object]);
            mockDamageDebuf.Setup(x => x.Effects).Returns([mockDamageDebufEffect.Object]);
            mockDamageBuff.Setup(x => x.Effects).Returns([mockDamageBufEffect.Object]);
            mockRegen.Setup(x => x.Effects).Returns([mockRegeneration.Object]);
            mockCriticalDamageDebuf.Setup(x => x.Effects).Returns([mockCriticalDamageDebuffEffect.Object]);
            mockCriticalDamageBuff.Setup(x => x.Effects).Returns([mockCriticalDamageBuffEffect.Object]);

            return [mockPoisonAbility.Object, mockHealthBuff.Object, mockAttackDebuff.Object, mockAttackBuff.Object, mockCriticalChanceDebufAbility.Object, mockCriticalChanceBuffAbility.Object, mockDamageDebuf.Object, mockDamageBuff.Object, mockRegen.Object, mockCriticalDamageDebuf.Object, mockCriticalDamageBuff.Object];
        }

        public List<ICondition> SetupConditions()
        {
            var mockHealthCondition = new Mock<ICondition>();
            var mockDebuffCondition = new Mock<ICondition>();
            var mockPoisonCondition = new Mock<ICondition>();
            var mockFuncCheck = new Mock<Func<bool>>();
            mockFuncCheck.Setup(x => x.Invoke()).Returns(true);

            mockHealthCondition.Setup(x => x.Weight).Returns(5);
            mockHealthCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Regeneration | EffectType.Buff);
            mockHealthCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);

            mockDebuffCondition.Setup(x => x.Weight).Returns(7);
            mockDebuffCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Buff | EffectType.Cleans);
            mockDebuffCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);

            mockPoisonCondition.Setup(x => x.Weight).Returns(6);
            mockPoisonCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Buff | EffectType.Cleans | EffectType.Regeneration);
            mockPoisonCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);

            return [mockHealthCondition.Object, mockDebuffCondition.Object, mockPoisonCondition.Object];
        }

        [TestMethod]
        public void SetDependency_Throws_NoExceptions_Test()
        {
            Assert.IsNotNull(_behavior);
            Assert.IsNotNull(_conditionsFactory);
            Assert.IsNotNull(_battleContext);
            _behavior.SetDependencies(_battleContext, _conditionsFactory);
        }

        [TestMethod]
        public void MakeDecision_ReturnAbility_Test()
        {
            Assert.IsNotNull(_behavior);
            Assert.IsNotNull(_behavior.MakeDecision());
        }

        [TestMethod]
        public void Test()
        {
            Assert.IsNotNull(_behavior);

        }
    }
}
