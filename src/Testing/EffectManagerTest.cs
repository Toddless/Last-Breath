namespace PlaygroundTest
{
    using System.Collections.Specialized;
    using Moq;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;

    [TestClass]
    public class EffectManagerTest
    {
        private EffectManager? _effectManager;


        [TestInitialize]
        public void Initialize()
        {
            _effectManager = new EffectManager([]);
        }

        [TestMethod]
        public void UpdateProperties_Raised_WhenEffectIsAddedTest()
        {
            Assert.IsNotNull(_effectManager);
            var mockUpdateProperties = new Mock<Action>();

            _effectManager.UpdateProperties = mockUpdateProperties.Object;
            var mockEffect = new Mock<IEffect>();

            _effectManager.Effects.Add(mockEffect.Object);

            mockUpdateProperties.Verify(x => x(), Times.Once);
        }

        [TestMethod]
        public void UpdateProperties_Raised_WhenEffectIsRemovedTest()
        {
            Assert.IsNotNull(_effectManager);
            var mockEffect = new Mock<IEffect>();
            _effectManager.Effects.Add(mockEffect.Object);
            var mockUpdateProperties = new Mock<Action>();

            _effectManager.UpdateProperties = mockUpdateProperties.Object;

            _effectManager.Effects.Remove(mockEffect.Object);

            mockUpdateProperties.Verify(x => x(), Times.Once);
        }

        [TestMethod]
        public void HealRaised_IfEffectsHas_RegenerationEffectType_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockHealAction = new Mock<Action<float>>();
            var mockHealEffect = new Mock<IEffect>();

            _effectManager.Heal = mockHealAction.Object;
            mockHealEffect.Setup(x => x.EffectType).Returns(EffectType.Regeneration);
            mockHealEffect.Setup(x => x.Modifier).Returns(10);
            _effectManager.Effects.Add(mockHealEffect.Object);

            _effectManager.HandleAppliedEffects();
            mockHealAction.Verify(x => x(10), Times.Once);
        }

        [TestMethod]
        public void TakeDamageRaised_IfEffectsHas_PoisonEffectType_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockTakeDamage = new Mock<Action<float>>();
            var mockPoisonEffect = new Mock<IEffect>();

            _effectManager.TakeDamage = mockTakeDamage.Object;
            mockPoisonEffect.Setup(x => x.EffectType).Returns(EffectType.Poison);
            mockPoisonEffect.Setup(x => x.Modifier).Returns(10);
            _effectManager.Effects.Add(mockPoisonEffect.Object);

            _effectManager.HandleAppliedEffects();
            mockTakeDamage.Verify(x => x(10), Times.Once);
        }

        [TestMethod]
        public void TakeDamageRaised_IfEffectsHas_BleedEffectType_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockTakeDamage = new Mock<Action<float>>();
            var mockBleedEffect = new Mock<IEffect>();

            _effectManager.TakeDamage = mockTakeDamage.Object;
            mockBleedEffect.Setup(x => x.EffectType).Returns(EffectType.Bleeding);
            mockBleedEffect.Setup(x => x.Modifier).Returns(10);
            _effectManager.Effects.Add(mockBleedEffect.Object);

            _effectManager.HandleAppliedEffects();
            mockTakeDamage.Verify(x => x(10), Times.Once);
        }

        [TestMethod]
        public void CalculateValues_WhenEffectsHas_DebuffEffectType_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockDebufEffect = new Mock<IEffect>();

            mockDebufEffect.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockDebufEffect.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeChance);
            mockDebufEffect.Setup(x => x.Modifier).Returns(-0.1f);

            _effectManager.Effects.Add(mockDebufEffect.Object);
            var result = _effectManager.CalculateValues(10, 0, 1, Parameter.CriticalStrikeChance);
            Assert.IsTrue(result == 9f);

        }

        [TestMethod]
        public void CalculateValues_WhenEffectsHas_BuffEffectType_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockBuffEffect = new Mock<IEffect>();

            mockBuffEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockBuffEffect.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeDamage);
            mockBuffEffect.Setup(x => x.Modifier).Returns(0.15f);

            _effectManager.Effects.Add(mockBuffEffect.Object);
            var result = _effectManager.CalculateValues(1.5f, 0, 1, Parameter.CriticalStrikeDamage);
            Assert.IsTrue(result >= 1.72f);
        }

        [TestMethod]
        public void EffectRemoved_WhenExpired_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockEffect = new Mock<IEffect>();
            mockEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockEffect.Setup(x => x.Duration).Returns(0);

            _effectManager.Effects.Add(mockEffect.Object);
            _effectManager.HandleAppliedEffects();

            Assert.IsTrue(_effectManager.Effects.Count == 0);
        }

        [TestMethod]
        public void DebuffSum_BelowZero_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockDebufEffect = new Mock<IEffect>();
            mockDebufEffect.Setup(x => x.EffectType).Returns(EffectType.Debuff);
            mockDebufEffect.Setup(x => x.Parameter).Returns(Parameter.CriticalStrikeChance);
            mockDebufEffect.Setup(x => x.Modifier).Returns(-1f);

            _effectManager.Effects.Add(mockDebufEffect.Object);
            var result = _effectManager.CalculateValues(0.05f, 0, 1, Parameter.CriticalStrikeChance);

            Assert.IsTrue(result < 0.001f);
        }

        [TestMethod]
        public void AddingNewAbility_AddedEffect_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockAbility = new Mock<IAbility>();
            var mockEffect = new Mock<IEffect>();
            var addEventsArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, mockAbility.Object);

            mockEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockAbility.Setup(x => x.Effects).Returns([mockEffect.Object]);

            _effectManager.OnChangeAbility(new object(), addEventsArgs);

            Assert.IsTrue(_effectManager.Effects.Count > 0);
        }

        [TestMethod]
        public void RemoveOldAbility_RemovedEffect_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockAbility = new Mock<IAbility>();
            var mockEffect = new Mock<IEffect>();
            var addEventsArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, mockAbility.Object);

            mockEffect.Setup(x => x.EffectType).Returns(EffectType.Buff);
            mockAbility.Setup(x => x.Effects).Returns([mockEffect.Object]);
            _effectManager.Effects.Add(mockEffect.Object);
            _effectManager.OnChangeAbility(new object(), addEventsArgs);

            Assert.IsTrue(_effectManager.Effects.Count == 0);
        }
    }
}
