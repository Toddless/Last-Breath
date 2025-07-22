namespace PlaygroundTest.ComponentTests
{
    using Moq;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    [TestClass]
    public class ModuleManagerTest
    {

        [TestInitialize]
        public void Setup()
        {

        }


        [TestMethod]
        public void FirstTest()
        {
            var module = new Mock<IStatModule>();
            module.Setup(x => x.Type).Returns(StatModule.CritChance);
            module.Setup(x => x.Priority).Returns(DecoratorPriority.Base);
            var moduleDecorator = new Mock<IModuleDecorator<StatModule, IStatModule>>();
            moduleDecorator.Setup(x => x.Priority).Returns(DecoratorPriority.Strong);
            moduleDecorator.Setup(x => x.Type).Returns(StatModule.CritChance);

        }
    }
}
