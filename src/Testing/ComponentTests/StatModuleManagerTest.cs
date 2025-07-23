namespace PlaygroundTest.ComponentTests
{
    using Playground.Components;
    using Playground.Script.BattleSystem.Decorators;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;
    using PlaygroundTest.ComponentTests.TestData;

    [TestClass]
    public class StatModuleManagerTest
    {
        private const float BaseValue = 10;

        [TestMethod]
        [DataRow(StatModule.Damage)]
        [DataRow(StatModule.CritChance)]
        public void BaseModulesInitialized_Test(StatModule key)
        {
            var manager = CreateManager();
            var module = manager.GetModule(key);
            Assert.IsNotNull(module);
        }

        [TestMethod]
        public void AvgCritChanceBiggerWithDecorator_Test()
        {
            var manager = CreateManager();
            var critDecorator = new LuckyCritDecoratorTest(DecoratorPriority.Weak);
            float avgCrit = 0;

            for (int i = 0; i < 10; i++)
            {
                avgCrit += critDecorator.GetValue();
            }
            avgCrit /= 10;

            manager.AddDecorator(critDecorator);

            var moduleWithDecorator = manager.GetModule(StatModule.CritChance);

            float avgWithDecorator = 0;
            for (int i = 0; i < 10; i++)
            {
                avgWithDecorator += moduleWithDecorator.GetValue();
            }

            avgWithDecorator /= 10;

            Assert.IsTrue(avgWithDecorator > avgCrit);
        }

        [TestMethod]
        public void DecoratorPriority_Test()
        {
            var manager = CreateManager();
            var weakAddDecorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Weak, 15);
            var strongAddDecorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Strong, 25);

            var weakIncDecorator = new IncreaseDamageDecoratorTest(DecoratorPriority.Weak, 0.75f);
            var strongIncDecorator = new IncreaseDamageDecoratorTest(DecoratorPriority.Strong, 1.25f);

            manager.AddDecorator(strongAddDecorator);
            manager.AddDecorator(weakAddDecorator);
            manager.AddDecorator(weakIncDecorator);
            manager.AddDecorator(strongIncDecorator);

            var moduleWithDecorators = manager.GetModule(StatModule.Damage);
            var value = moduleWithDecorators.GetValue();

            Assert.IsTrue(value == 55);

        }

        private ModuleManager<StatModule, IStatModule, StatModuleDecorator> CreateManager()
        {
            var baseModule = new Dictionary<StatModule, IStatModule>
            {
                [StatModule.Damage] = new DamageModuleTest(BaseValue),
                [StatModule.CritChance] = new CritModuleTest()
            };

            return new ModuleManager<StatModule, IStatModule, StatModuleDecorator>(baseModule);
        }
    }
}
