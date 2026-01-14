namespace LastBreathTest.ComponentTests
{
    using Battle.Source;
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using TestData;

    [TestClass]
    public class ModuleManagerTest
    {
        private const float BaseValue = 10;

        [TestMethod]
        [DataRow(EntityParameter.Damage)]
        [DataRow(EntityParameter.CriticalChance)]
        public void BaseModulesInitialized_Test(EntityParameter key)
        {
            var manager = CreateManager();
            var module = manager.GetModule(key);
            Assert.IsNotNull(module);
        }

        [TestMethod]
        public void AvgCritChanceBiggerWithLuckyDecorator_Test()
        {
            var manager = CreateManager();
            var critModuleWithoutDecorator = manager.GetModule(EntityParameter.CriticalChance);
            float avgCrit = 0;

            for (int i = 0; i < 30; i++)
            {
                avgCrit += critModuleWithoutDecorator.GetValue();
            }

            avgCrit /= 30;

            var critDecorator = new LuckyCritDecoratorTest(DecoratorPriority.Weak);
            manager.AddDecorator(critDecorator);

            var moduleWithDecorator = manager.GetModule(EntityParameter.CriticalChance);

            float avgWithDecorator = 0;
            for (int i = 0; i < 30; i++)
            {
                avgWithDecorator += moduleWithDecorator.GetValue();
            }

            avgWithDecorator /= 30;

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

            var moduleWithDecorators = manager.GetModule(EntityParameter.Damage);
            float value = moduleWithDecorators.GetValue();

            Assert.IsTrue(value > 54);
        }

        [TestMethod]
        public void TryToRemoveWrongDecorator_Test()
        {
            var manager = CreateManager();
            bool activated = false;
            manager.ModuleChanges += OnModuleChanges;

            var decorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Weak, 15);

            manager.AddDecorator(decorator);
            var secondDecorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Strong, 25);
            manager.RemoveDecorator(secondDecorator.Id, secondDecorator.Parameter);

            void OnModuleChanges(EntityParameter stat)
            {
                activated = true;
            }

            Assert.IsTrue(activated, $"Activated: {activated}");
        }

        [TestMethod]
        public void SetValueAsBase_Test()
        {
            var manager = CreateManager();

            var decorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Weak, 15);
            var secondDecorator = new AdditionalDamageDecoratorTest(DecoratorPriority.Strong, 25);
            float valueAsBase = 25;

            manager.AddDecorator(decorator);
            manager.AddDecorator(secondDecorator);

            float finalValue = manager.GetModule(EntityParameter.Damage).ApplyDecoratorsForValue(valueAsBase);

            Assert.IsTrue(Math.Abs(finalValue - 65f) < 0.00001f);
        }

        private ModuleManager<EntityParameter, IParameterModule<EntityParameter>, EntityParameterModuleDecorator> CreateManager() => new(
            new Dictionary<EntityParameter, IParameterModule<EntityParameter>>
            {
                [EntityParameter.Damage] = new DamageModuleTest(BaseValue), [EntityParameter.CriticalChance] = new CritModuleTest()
            });
    }
}
