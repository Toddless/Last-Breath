namespace PlaygroundTest.ComponentTests
{
    using Playground.Components;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;
    using PlaygroundTest.ComponentTests.TestData;

    [TestClass]
    public class StatModuleManagerTest
    {
        private readonly StatModule _key = StatModule.Damage;
        private const float BaseValue = 10;
       


        [TestInitialize]
        public void Setup()
        {

        }


        [TestMethod]
        public void FirstTest()
        {

        }

        private ModuleManager<StatModule, IStatModule, ModuleDecoratorTest> CreateManager()
        {
            var baseModule = new Dictionary<StatModule, IStatModule>
            {
                [_key] = new StatModuleTest(BaseValue)
            };

            return new ModuleManager<StatModule, IStatModule, ModuleDecoratorTest>(baseModule);
        }
    }
}
