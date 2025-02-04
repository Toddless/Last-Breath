namespace PlaygroundTest
{
    using Moq;
    using Playground;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Scenes;

    [TestClass]
    public class BattleBehaviorTest
    {
        private BattleBehavior? _behavior;
        private IBattleContext _battleContext;
        private IConditionsFactory _conditionsFactory;

        [TestInitialize]
        public void Initialize()
        {
            // i rly need that all
            _behavior = new BattleBehavior();
            var mockBattleContext = new Mock<IBattleContext>();
            var mockPoison = new Mock<IEffect>();
            var mockDebuf = new Mock<IEffect>();
            var mockRegeneration = new Mock<IEffect>();
            var mockFunc = new Mock<Func<float, float, float, Parameter, float>>();
            var mockHealthCondition = new Mock<ICondition>();
            var mockDebuffCondition = new Mock<ICondition>();
            var mockPoisonCondition = new Mock<ICondition>();
            var mockAbilityRegen = new Mock<IAbility>();
            var mockAbilityDebuf = new Mock<IAbility>();
            var mockFuncCheck = new Mock<Func<bool>>();
            mockFuncCheck.Setup(x => x.Invoke()).Returns(true);


            mockAbilityRegen.Setup(x => x.Cooldown).Returns(4);
            mockAbilityRegen.Setup(x => x.Effects).Returns([mockRegeneration.Object]);
            mockAbilityDebuf.Setup(x => x.Cooldown).Returns(4);
            mockAbilityDebuf.Setup(x => x.Effects).Returns([mockDebuf.Object]);

            mockHealthCondition.Setup(x => x.Weight).Returns(5);
            mockHealthCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Regeneration);
            mockHealthCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);

            mockDebuffCondition.Setup(x => x.Weight).Returns(7);
            mockDebuffCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Buff);
            mockDebuffCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);

            mockPoisonCondition.Setup(x => x.Weight).Returns(6);
            mockPoisonCondition.Setup(x => x.EffectNeeded).Returns(EffectType.Poison);
            mockPoisonCondition.Setup(x => x.CheckCondition).Returns(mockFuncCheck.Object);


            mockRegeneration.Setup(x => x.EffectType).Returns(EffectType.Regeneration);
            mockRegeneration.Setup(x => x.Modifier).Returns(50);
            mockPoison.Setup(x => x.EffectType).Returns(EffectType.Poison);
            mockPoison.Setup(x => x.Modifier).Returns(10);
            mockDebuf.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockDebuf.Setup(x => x.Modifier).Returns(0.3f);

            mockBattleContext.Setup(x => x.Self.HealthComponent).Returns(new HealthComponent(mockFunc.Object));
            mockBattleContext.Setup(x => x.Self.Abilities).Returns([mockAbilityRegen.Object, mockAbilityDebuf.Object]);
            mockBattleContext.Setup(x => x.Opponent.HealthComponent).Returns(new HealthComponent(mockFunc.Object));
            mockBattleContext.Setup(x => x.Self.EffectManager).Returns(new EffectManager([mockPoison.Object, mockDebuf.Object, mockRegeneration.Object]));
            _battleContext = mockBattleContext.Object;

            var mockFactory = new Mock<IConditionsFactory>();
            mockFactory.Setup(x => x.SetNewConditions(mockBattleContext.Object)).Returns([mockHealthCondition.Object, mockDebuffCondition.Object, mockPoisonCondition.Object]);
            _conditionsFactory = mockFactory.Object;
        }

        [TestMethod]
        public void SetDependency_Throws_NoExceptions_Test()
        {
            Assert.IsNotNull(_behavior);
            _behavior.SetDependencies(_battleContext, _conditionsFactory);
        }

        [TestMethod]
        public void MakeDecision_ReturnAbility_Test()
        {
            Assert.IsNotNull(_behavior);
            _behavior.SetDependencies(_battleContext, _conditionsFactory);

            Assert.IsNotNull(_behavior.MakeDecision());
        }
    }
}
