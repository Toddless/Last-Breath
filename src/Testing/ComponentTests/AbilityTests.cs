namespace LastBreathTest.ComponentTests
{
    using Moq;
    using TestData;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.Battle;
    using Battle.TestData.Abilities;
    using Core.Interfaces.Abilities;
    using Battle.TestData.Abilities.Effects;
    using Battle.TestData.Abilities.Upgrades;

    [TestClass]
    public class AbilityTests
    {
        [TestMethod]
        public void CriticalChanceCalculationsIncludeEntityModifiers_Test()
        {
            var entity = new EntityTest();
            var fireball = new Fireball(3, 1, 20, 0.05f,
                [], 50, 25, [], [], [],
                new Mock<IStanceMastery>().Object);
            fireball.SetOwner(entity);

            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Flat, 0.05f, entity));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Flat, 0.05f, entity));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Increase, 0.25f, entity));
            entity.Modifiers.AddPermanentModifier(new ModifierInstance(EntityParameter.CriticalChance, ModifierType.Multiplicative, 0.3f, entity));

            float criticalChange = fireball.CriticalChanceValue;

            Assert.IsTrue(Math.Abs(criticalChange - 0.2535f) < 0.00001f, $"Actual: {criticalChange}");
        }

        [TestMethod]
        public void EntityHasNotEnoughResource_Test()
        {
            var entity = new EntityTest();

            var fireball = new Fireball(3, 1, 20, 0.05f,
                [], 50, 25, [], [], [], new Mock<IStanceMastery>().Object);
            fireball.SetOwner(entity);

            bool isEnough = fireball.IsEnoughResource();
            Assert.IsTrue(isEnough,
                $"Is enough: {isEnough}. Actual entity resource: Mana - {entity.CurrentMana}, Health - {entity.CurrentHealth}, Barrier - {entity.CurrentBarrier}");
        }

        [TestMethod]
        public void AbilitySpendResource_Test()
        {
            var entity = new EntityTest();

            var fireball = new Fireball(3, 1, 20, 0.05f,
                [], 50, 25, [], [], [], new Mock<IStanceMastery>().Object);
            fireball.SetOwner(entity);

            fireball.Activate([]);

            Assert.IsTrue(Math.Abs(entity.CurrentMana - fireball.Cost) <= 25f, $"Entity mana: {entity.CurrentMana}");
        }

        [TestMethod]
        public void AbilitySpendHealthAfterChangeCostToHealth_Test()
        {
            var entity = new EntityTest();

            var fireball = new Fireball(3, 1, 20, 0.05f,
                [], 50, 25, [], [], [],
                new Mock<IStanceMastery>().Object);
            fireball.SetOwner(entity);

            var upgrade = new ChangeAbilityCostTypeUpgrade("Ability_Upgrade_Change_Cost_Type", [], Costs.Health, 3, 15, 1);
            upgrade.ApplyUpgrade(fireball);
            fireball.Activate([]);

            var type = fireball.Type;
            Assert.IsTrue(type == Costs.Health, $"Actual: {type}");
            Assert.IsTrue(Math.Abs(entity.CurrentHealth - 585) < 0.00001f, $"Entity health: {entity.CurrentHealth}");
        }

        [TestMethod]
        public void AbilitySpendManaAfterHealthUpgradeRemoved_Test()
        {
            var entity = new EntityTest();

            var fireball = new Fireball(3, 1, 20, 0.05f,
                [], 50, 25, [], [], [],
                new Mock<IStanceMastery>().Object);
            fireball.SetOwner(entity);

            var upgrade = new ChangeAbilityCostTypeUpgrade("Ability_Upgrade_Change_Cost_Type", [], Costs.Health, 3, 15, 1);
            upgrade.ApplyUpgrade(fireball);

            var typeBefore = fireball.Type;

            upgrade.RemoveUpgrade(fireball);
            Assert.IsTrue(typeBefore == Costs.Health, $"Actual: {typeBefore}");
            var typeAfter = fireball.Type;
            Assert.IsTrue(typeAfter == Costs.Mana, $"Actual type after: {typeAfter}");
        }


        [TestMethod]
        public void ParameterEqualsEffectApply_Test()
        {
            var entity = new EntityTest();
            var effect = new ParameterEqualsEffect("Health_Equals_One", 3, EntityParameter.Health, 1);
            effect.OnApply(new EffectApplyingContext { Caster = entity, Target = entity, Source = entity });

            Assert.IsTrue(Math.Abs(entity.CurrentHealth - 1) < 0.00001f, $"Actual: {entity.CurrentHealth}");
        }

        [TestMethod]
        public void DexterityEqualsOneAffectParameters_Test()
        {
            var entity = new EntityTest();
            entity.Dexterity.IncreasePointsByAmount(5);

            var effect = new ParameterEqualsEffect("Dexterity_Equals_One", 3, EntityParameter.Dexterity, 1);
            effect.OnApply(new EffectApplyingContext { Caster = entity, Target = entity, Source = entity });

            Assert.IsTrue(Math.Abs(entity.Parameters.CriticalChance - 0.0525f) < 0.00001f, $"Actual: {entity.Parameters.CriticalChance}");
        }

        [TestMethod]
        public void StrengthEqualsOneAffectParameters_Test()
        {
            var entity = new EntityTest();

            entity.Strength.IncreasePointsByAmount(100);

            float healthWithStrength = entity.CurrentHealth;
            var effect = new ParameterEqualsEffect("Strength_Equals_One", 3, EntityParameter.Strength, 1);
            effect.OnApply(new EffectApplyingContext { Caster = entity, Target = entity, Source = entity });

            Assert.IsTrue(MathF.Abs(healthWithStrength - 1610) < 0.00001f, $"Actual: {healthWithStrength}");
            Assert.IsTrue(MathF.Abs(entity.CurrentHealth - 610) < 0.00001f, $"Actual: {entity.CurrentHealth}");
        }

        [TestMethod]
        public void MultiplyDecoratorsWithSamePriority_Test()
        {
            var entity = new EntityTest();
        }
    }
}
