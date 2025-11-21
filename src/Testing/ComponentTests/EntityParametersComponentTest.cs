namespace LastBreathTest.ComponentTests
{
    using Battle.Components;
    using Battle.Source.Decorators;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Modifiers;

    [TestClass]
    public class EntityParametersComponentTest
    {
        [TestMethod]
        public void AddedHealthBonuses()
        {
            var healthComponent = new EntityParametersComponent();
            var healthPercent = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "change_value", 0.9f);
            var second = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "second", 1.3f);

            healthComponent.AddModuleDecorator(healthPercent);
            var afterFirstDecorator = healthComponent.MaxHealth;
            healthComponent.AddModuleDecorator(second);
            var afterSecondDecorator = healthComponent.MaxHealth;
            healthComponent.RemoveModuleDecorator(second.Id, second.Parameter);


            Assert.IsTrue(afterSecondDecorator > afterFirstDecorator);
        }

        [TestMethod]
        public void AddedHealthModifiers()
        {
            var healthComponent = new EntityParametersComponent();

            var healthPercent = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "change_value", 0.9f);
            var second = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "second", 1.3f);
            var modifiers = new List<IModifierInstance>()
            {
                new ModifierInstance(EntityParameter.Health, ModifierType.Flat, 250, this),
                new ModifierInstance(EntityParameter.Health, ModifierType.Increase, 0.3f, this),
                new ModifierInstance(EntityParameter.Health, ModifierType.Multiplicative, 1.3f, this)
            };
            healthComponent.OnParameterChanges(null, new TestModifiersChangedEventArgs(EntityParameter.Health, modifiers));

            var health = healthComponent.MaxHealth;

            healthComponent.AddModuleDecorator(healthPercent);
            healthComponent.AddModuleDecorator(second);


            var healthAfterModule = healthComponent.MaxHealth;

            Assert.IsTrue(health < healthAfterModule);
        }
    }
}
