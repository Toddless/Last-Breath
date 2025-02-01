namespace PlaygroundTest
{
    using Moq;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

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
        public void UpdateProperties_Raised_Test()
        {
            Assert.IsNotNull(_effectManager);
            var mockUpdateProperties = new Mock<Action>();

            _effectManager.UpdateProperties = mockUpdateProperties.Object;
            var mockEffect = new Mock<IEffect>();

            _effectManager.Effects.Add(mockEffect.Object);

            mockUpdateProperties.Verify(x=>x(), Times.Once);
        }
    }
}
