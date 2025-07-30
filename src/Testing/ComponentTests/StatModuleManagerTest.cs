namespace LastBreathTest.ComponentTests
{
    using LastBreath.Components;
    using LastBreath.Script.BattleSystem.Decorators;
    using LastBreath.Script.BattleSystem.Module;
    using LastBreath.Script.Enums;
    using LastBreathTest.ComponentTests.TestData;

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
        public void AvgCritChanceBiggerWithLuckyDecorator_Test()
        {
            var manager = CreateManager();
            var critModuleWithoutDecorator = manager.GetModule(StatModule.CritChance);
            float avgCrit = 0;

            for (int i = 0; i < 10; i++)
            {
                avgCrit += critModuleWithoutDecorator.GetValue();
            }
            avgCrit /= 10;

            var critDecorator = new LuckyCritDecoratorTest(DecoratorPriority.Weak);
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
            manager.RemoveDecorator(secondDecorator);

            void OnModuleChanges(StatModule stat, IStatModule module)
            {
                activated = true;
            }

            Assert.IsFalse(activated);
        }

        private ModuleManager<StatModule, IStatModule, StatModuleDecorator> CreateManager() => new(new Dictionary<StatModule, IStatModule>
        {
            [StatModule.Damage] = new DamageModuleTest(BaseValue),
            [StatModule.CritChance] = new CritModuleTest()
        });
    }
}
