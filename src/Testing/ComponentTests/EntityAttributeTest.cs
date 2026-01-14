namespace LastBreathTest.ComponentTests
{
    using TestData;
    using Core.Enums;
    using Core.Modifiers;
    using Battle.Source.Decorators;

    [TestClass]
    public class EntityAttributeTest
    {
        [TestMethod]
        public void DefaultValueForDexterity_Test()
        {
            var entity = new EntityTest();

            Assert.IsTrue(entity.Dexterity.Total == 1);
        }


        [TestMethod]
        public void IncreasingDexterityIncreaseCriticalChance_Test()
        {
            var entity = new EntityTest();

            entity.Dexterity.IncreasePointsByAmount(3);
            float criticalChanceWithIncreasedDex = entity.Parameters.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChanceWithIncreasedDex - 0.06f) < 0.00001f, $"Value is: {criticalChanceWithIncreasedDex}");
        }


        [TestMethod]
        public void IncreaseDexterityChangeCriticalChanceValue_Test()
        {
            var entity = new EntityTest();
            var dexModifier = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Increase, 0.3f, this);
            var dexModifierMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 0.2f, this);
            entity.Modifiers.AddPermanentModifier(dexModifier);
            entity.Modifiers.AddPermanentModifier(dexModifierMulti);
            entity.Dexterity.IncreasePointsByAmount(5);

            float criticalChance = entity.Parameters.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChance - 0.0725f) < 0.0001f, $"Value is: {criticalChance}, total dex: {entity.Dexterity.Total}");
        }

        [TestMethod]
        public void ModifiersChangeTotalDex_Test()
        {
            var entity = new EntityTest();

            var dexModifierFlat = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Flat, 2f, this);
            var dexModifierInc = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Increase, 0.5f, this);
            var dexModifierMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 0.2f, this);

            entity.Modifiers.AddPermanentModifier(dexModifierFlat);
            entity.Modifiers.AddPermanentModifier(dexModifierInc);
            entity.Modifiers.AddPermanentModifier(dexModifierMulti);

            entity.Dexterity.IncreasePointsByAmount(7);

            Assert.IsTrue(entity.Dexterity.Total == 18, $"Value is: {entity.Dexterity.Total}");
        }

        [TestMethod]
        public void DecoratorChangesTotalDex_Test()
        {
            var entity = new EntityTest();

            var dexMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 0.5f, this);
            entity.Modifiers.AddPermanentModifier(dexMulti);
            entity.Dexterity.IncreasePointsByAmount(19);

            var decorator = new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_80_percent_decorator", 0.2f);
            entity.Parameters.AddModuleDecorator(decorator);

            Assert.IsTrue(entity.Dexterity.Total == 6, $"Value is: {entity.Dexterity.Total}");
        }

        [TestMethod]
        public void DecoratorChangesCriticalChance_Test()
        {
            var entity = new EntityTest();

            var dexMulti = new ModifierInstance(EntityParameter.Dexterity, ModifierType.Multiplicative, 0.5f, this);
            entity.Modifiers.AddPermanentModifier(dexMulti);
            entity.Dexterity.IncreasePointsByAmount(19);
            float criticalChangeBefore = entity.Parameters.CriticalChance;
            var decorator = new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_80_percent_decorator", 0.2f);
            entity.Parameters.AddModuleDecorator(decorator);

            float criticalChangeAfter = entity.Parameters.CriticalChance;

            Assert.IsTrue(Math.Abs(criticalChangeBefore - 0.125f) < 0.0001f, $"Critical chance before: {criticalChangeBefore} ");
            Assert.IsTrue(Math.Abs(criticalChangeAfter - 0.065f) < 0.0001f, $"Critical chance after: {criticalChangeAfter} ");
        }

        [TestMethod]
        public void ReduceDexterityDoNotAffectCriticalChanceModifier_Test()
        {
            var entity = new EntityTest();
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Flat, 0.05f, this));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Increase, 0.15f, this));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Increase, 0.2f, this));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Multiplicative, 0.2f, this));
            entity.Dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = entity.Parameters.CriticalChance; // should be 0.282f
            entity.Parameters.AddModuleDecorator(new ChangeValueDecorator(EntityParameter.Dexterity, DecoratorPriority.Strong, "reduce_dexterity_by_90_percent_decorator", 0.1f));

            float criticalChanceAfter = entity.Parameters.CriticalChance; // should be 0.174f

            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.282f) < 0.00001f, $"Value before: {criticalChanceBefore} ");
            Assert.IsTrue(MathF.Abs(criticalChanceAfter - 0.174f) < 0.00001f, $"Value after: {criticalChanceAfter}");
        }

        [TestMethod]
        public void ReduceOverallCriticalChange_Test()
        {
            var entity = new EntityTest();
            entity.Dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = entity.Parameters.CriticalChance; // should be 0.10f
            entity.Parameters.AddModuleDecorator(new ChangeValueDecorator(EntityParameter.CriticalChance, DecoratorPriority.Strong,
                "reduce_critical_chance_by_80_percent_decorator",
                0.2f));

            float criticalChange = entity.Parameters.CriticalChance; // should be 0.02

            Assert.IsTrue(MathF.Abs(criticalChange - 0.02f) < 0.0001f, $"Value before: {criticalChanceBefore}");
            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.10f) < 0.0001f, $"Value after : {criticalChange}");
        }


        [TestMethod]
        public void RemovingDexterityPointChangesCriticalChange_Test()
        {
            var entity = new EntityTest();
            entity.Dexterity.IncreasePointsByAmount(19);
            float criticalChanceBefore = entity.Parameters.CriticalChance; // should be 0.10f

            entity.Dexterity.DecreasePointsByAmount(10);
            float criticalChangeAfter = entity.Parameters.CriticalChance; // 0.75f

            Assert.IsTrue(MathF.Abs(criticalChanceBefore - 0.10f) < 0.0001f, $"Value before: {criticalChanceBefore}");
            Assert.IsTrue(MathF.Abs(criticalChangeAfter - 0.075f) < 0.0001f, $"Value after: {criticalChangeAfter}");
        }
    }
}
