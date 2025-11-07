namespace LastBreathTest.ComponentTests
{
    using Battle.Source;
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;
    using Core.Interfaces.Battle.Module;
    using LastBreathTest.ComponentTests.TestData;

    [TestClass]
    public class StatModuleManagerTest
    {
        private const float BaseValue = 10;

        [TestMethod]
        [DataRow(Parameter.Damage)]
        [DataRow(Parameter.CriticalChance)]
        public void BaseModulesInitialized_Test(Parameter key)
        {
            var manager = CreateManager();
            var module = manager.GetModule(key);
            Assert.IsNotNull(module);
        }

        [TestMethod]
        public void AvgCritChanceBiggerWithLuckyDecorator_Test()
        {
            var manager = CreateManager();
            var critModuleWithoutDecorator = manager.GetModule(Parameter.CriticalChance);
            float avgCrit = 0;

            for (int i = 0; i < 10; i++)
            {
                avgCrit += critModuleWithoutDecorator.GetValue();
            }
            avgCrit /= 10;

            var critDecorator = new LuckyCritDecoratorTest(DecoratorPriority.Weak);
            manager.AddDecorator(critDecorator);

            var moduleWithDecorator = manager.GetModule(Parameter.CriticalChance);

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

            var moduleWithDecorators = manager.GetModule(Parameter.Damage);
            var value = moduleWithDecorators.GetValue();

            Assert.IsTrue(value > 54);

        }

        [TestMethod]
        public void TryToRemoveWrongDecorator_Test()
        {
            var manager = CreateManager();
            bool activated = false;

            var decorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Weak, 15);
            manager.AddDecorator(decorator);

            var secondDecorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Strong, 25);
            manager.ModuleDecoratorChanges += OnModuleChanges;
            manager.RemoveDecorator(secondDecorator.Id, secondDecorator.Parameter);

            void OnModuleChanges(Parameter stat, IParameterModule module)
            {
                activated = true;
            }

            Assert.IsFalse(activated);
        }

        private ModuleManager<Parameter, IParameterModule, StatModuleDecorator> CreateManager() => new(new Dictionary<Parameter, IParameterModule>
        {
            [Parameter.Damage] = new DamageModuleTest(BaseValue),
            [Parameter.CriticalChance] = new CritModuleTest()
        });
    }
}
