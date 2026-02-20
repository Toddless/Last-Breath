namespace LastBreathTest.ComponentTests
{
    using Core.Enums;
    using Core.Modifiers;
    using Battle.Attribute;
    using Battle.Components;
    using Battle.Source.Decorators;
    using Core.Interfaces.Components;
    using TestData;
    using Battle.Attribute.Components;

    [TestClass]
    public class EntityParametersComponentTest
    {
        [TestMethod]
        public void AddedHealthBonuses()
        {
            PrepareEntities(out IModifiersComponent manager, out EntityParametersComponent component, out Strength strength);

            var healthPercent = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "reduce_health_by_10_percent", 0.9f);
            var second = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "increase_all_health_by_30_percent", 1.3f);

            component.AddModuleDecorator(healthPercent);
            float afterFirstDecorator = component.MaxHealth;
            component.AddModuleDecorator(second);
            float afterSecondDecorator = component.MaxHealth;
            component.RemoveModuleDecorator(second.Id, second.Parameter);

            Assert.IsTrue(afterSecondDecorator > afterFirstDecorator);
        }

        [TestMethod]
        public void AddedHealthModifiers()
        {
            var healthComponent = new EntityParametersComponent();

            var healthPercent = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "change_value", 0.9f);
            var second = new ChangeValueDecorator(EntityParameter.Health, DecoratorPriority.Weak, "second", 1.3f);
            var modifiers = new List<IModifierInstance>
            {
                new ModifierInstance(EntityParameter.Health, ModifierType.Flat, 250, this),
                new ModifierInstance(EntityParameter.Health, ModifierType.Increase, 0.3f, this),
                new ModifierInstance(EntityParameter.Health, ModifierType.Multiplicative, 1.3f, this)
            };
            healthComponent.OnModifiersChange(null, new TestModifiersChangedEventArgs(EntityParameter.Health, modifiers));

            float health = healthComponent.MaxHealth;

            healthComponent.AddModuleDecorator(healthPercent);
            healthComponent.AddModuleDecorator(second);


            float healthAfterModule = healthComponent.MaxHealth;

            Assert.IsTrue(health < healthAfterModule);
        }

        private static void PrepareEntities(out IModifiersComponent manager, out EntityParametersComponent component, out Strength strength)
        {
            manager = new ModifiersComponent();
            component = new EntityParametersComponent();
            manager.ModifiersChanged += component.OnModifiersChange;
            strength = new Strength(manager);
            component.ParameterChanged += strength.OnParameterChanges;
            component.Initialize(manager.GetModifiers);

            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
            {
                float value = parameter switch
                {
                    EntityParameter.AdditionalHitChance or EntityParameter.CriticalChance or EntityParameter.Suppress => 0.05f,
                    EntityParameter.CriticalDamage => 1.2f,
                    EntityParameter.BlockChance => 0.05f,
                    EntityParameter.Damage => 50f,
                    EntityParameter.Intelligence or EntityParameter.Strength => 1f,
                    EntityParameter.SpellDamage => 20f,
                    EntityParameter.Mana => 50f,
                    EntityParameter.ManaRecovery => 5f,
                    EntityParameter.MoveSpeed => 125f,
                    EntityParameter.Health or EntityParameter.Armor or EntityParameter.Barrier or EntityParameter.Evade => 250f,
                    EntityParameter.HealthRecovery => 50f,
                    _ => 0,
                };

                if(value == 0) continue;
                manager.AddPermanentModifier(new ModifierInstance(parameter, ModifierType.Flat, value, component));
            }

        }
    }
}
