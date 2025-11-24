namespace LastBreathTest.ComponentTests
{
    using Battle;
    using Core.Enums;
    using Core.Modifiers;
    using Battle.Attribute;
    using Battle.Components;
    using Battle.Source.Decorators;
    using Core.Interfaces.Components;

    [TestClass]
    public class EntityAttributeTest
    {
        [TestMethod]
        public void DefaultValueForDexterity_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);

            Assert.IsTrue(dexterity.Total == 1);
        }


        [TestMethod]
        public void IncreasingDexterityIncreaseCriticalChance_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);

            dexterity.IncreasePointsByAmount(3);
            float criticalChanceWithIncreasedDex = component.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChanceWithIncreasedDex - 0.06f) < 0.00001f, $"Value is: {criticalChanceWithIncreasedDex}");
        }


        [TestMethod]
        public void IncreaseDexterityChangeCriticalChanceValue_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);
            var dexModifier = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Increase, 0.3f, this);
            var dexModifierMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 1.2f, this);
            manager.AddPermanentModifier(dexModifier);
            manager.AddPermanentModifier(dexModifierMulti);
            dexterity.IncreasePointsByAmount(5);

            float criticalChance = component.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChance - 0.0725f) < 0.0001f, $"Value is: {criticalChance}, total dex: {dexterity.Total}");
        }

        [TestMethod]
        public void ModifiersChangeTotalDex_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);

            var dexModifierFlat = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Flat, 2f, this);
            var dexModifierInc = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Increase, 0.5f, this);
            var dexModifierMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 1.2f, this);

            manager.AddPermanentModifier(dexModifierFlat);
            manager.AddPermanentModifier(dexModifierInc);
            manager.AddPermanentModifier(dexModifierMulti);

            dexterity.IncreasePointsByAmount(7);

            Assert.IsTrue(dexterity.Total == 18, $"Value is: {dexterity.Total}");
        }

        [TestMethod]
        public void DecoratorChangesTotalDex_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);

            var dexMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 1.5f, this);
            manager.AddPermanentModifier(dexMulti);
            dexterity.IncreasePointsByAmount(19);

            var decorator = new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_80_percent_decorator", 0.2f);
            component.AddModuleDecorator(decorator);

            Assert.IsTrue(dexterity.Total == 6, $"Value is: {dexterity.Total}");
        }

        [TestMethod]
        public void DecoratorChangesCriticalChance_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);

            var dexMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 1.5f, this);
            manager.AddPermanentModifier(dexMulti);
            dexterity.IncreasePointsByAmount(19);
            float criticalChangeBefore = component.CriticalChance;
            var decorator = new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_80_percent_decorator", 0.2f);
            component.AddModuleDecorator(decorator);

            float criticalChangeAfter = component.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChangeBefore - 0.125f) < 0.0001f, $"Critical chance before: {criticalChangeBefore} ");
            Assert.IsTrue(Math.Abs(criticalChangeAfter - 0.065f) < 0.0001f, $"Critical chance after: {criticalChangeAfter} ");
        }

        [TestMethod]
        public void ReduceDexterityDoNotAffectCriticalChanceModifier_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);
            manager.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Flat, 0.05f, this));
            manager.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Increase, 0.15f, this));
            manager.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Increase, 0.2f, this));
            manager.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Multiplicative, 1.2f, this));
            dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = component.CriticalChance; // should be 0.282f
            component.AddModuleDecorator(new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_90_percent_decorator", 0.1f));

            float criticalChanceAfter = component.CriticalChance; // should be 0.174f

            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.282f) < 0.00001f, $"Value before: {criticalChanceBefore} ");
            Assert.IsTrue(MathF.Abs(criticalChanceAfter - 0.174f) < 0.00001f, $"Value after: {criticalChanceAfter}");
        }

        [TestMethod]
        public void ReduceOverallCriticalChange_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);
            dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = component.CriticalChance; // should be 0.10f
            component.AddModuleDecorator(new ChangeValueDecorator(EntityParameter.CriticalChance, DecoratorPriority.Strong, "reduce_critical_chance_by_80_percent_decorator",
                0.2f));

            float criticalChange = component.CriticalChance; // should be 0.02

            Assert.IsTrue(MathF.Abs(criticalChange - 0.02f) < 0.0001f, $"Value before: {criticalChanceBefore}");
            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.10f) < 0.0001f, $"Value after : {criticalChange}");
        }


        [TestMethod]
        public void RemovingDexterityPointChangesCriticalChange_Test()
        {
            PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity);
            dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = component.CriticalChance; // should be 0.10f

            dexterity.DecreasePointsByAmount(10);
            float criticalChangeAfter = component.CriticalChance; // 0.75f

            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.10f) < 0.0001f, $"Value before: {criticalChanceBefore}");
            Assert.IsTrue(MathF.Abs(criticalChangeAfter - 0.075f) < 0.0001f, $"Value after: {criticalChangeAfter}");
        }


        private static void PrepareEntities(out IModifierManager manager, out EntityParametersComponent component, out Dexterity dexterity)
        {
            manager = new ModifierManager();
            component = new EntityParametersComponent();
            manager.ModifiersChanged += component.OnModifiersChange;
            dexterity = new Dexterity(manager);
            component.ParameterChanged += dexterity.OnParameterChanges;
            component.Initialize();

            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
            {
                float value = parameter switch
                {
                    EntityParameter.CriticalChance => 0.05f,
                    EntityParameter.CriticalDamage => 1.2f,
                    EntityParameter.Damage => 50f,
                    EntityParameter.Health => 250f,
                    _ => 0,
                };

                if (value == 0) continue;
                manager.AddPermanentModifier(new ModifierInstance(parameter, ModifierType.Flat, value, component));
            }
        }
    }
}
