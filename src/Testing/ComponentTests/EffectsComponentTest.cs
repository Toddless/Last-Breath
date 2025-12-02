namespace LastBreathTest.ComponentTests
{
    using Moq;
    using TestData;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Battle.TestData.Abilities;
    using Battle.TestData.Abilities.Effects;

    [TestClass]
    public class EffectsComponentTest
    {
        [TestMethod]
        public void FireballAppyBurning_Test()
        {
            IEntity entity = Entity(out Fireball fireball);
            fireball.Activate([entity]);

            Assert.IsTrue(entity.StatusEffects == StatusEffects.Burning);
        }

        [TestMethod]
        public void FireballDealDamage_Test()
        {
            IEntity entity = Entity(out Fireball fireball);
            fireball.Activate([entity]);

            Assert.IsTrue(Math.Abs(entity.CurrentHealth - 540) < 0.0001f, $"Actual value: {entity.CurrentHealth}");
        }

        [TestMethod]
        public void BurningEffectDealDamage_Test()
        {
            IEntity entity = Entity(out Fireball fireball);

            fireball.Activate([entity]);

            for (int i = 0; i < 4; i++)
                entity.OnTurnEnd();

            Assert.IsTrue(Math.Abs(entity.CurrentHealth - 393) < 0.0001f, $"Actual value: {entity.CurrentHealth}");
        }

        [TestMethod]
        public void BurningEffectRemoved_Test()
        {
            IEntity entity = Entity(out Fireball fireball);

            fireball.Activate([entity]);

            for (int i = 0; i < 4; i++)
                entity.OnTurnEnd();

            Assert.IsTrue(entity.StatusEffects == StatusEffects.None);
        }

        [TestMethod]
        public void BurningEffectStacks_Test()
        {
            IEntity entity = Entity(out Fireball fireball);

            for (int i = 0; i < 2; i++)
            {
                fireball.Activate([entity]);

                entity.OnTurnEnd();
            }

            Assert.IsTrue(Math.Abs(entity.CurrentHealth - 323) < 0.0001f, $"Actual value: {entity.CurrentHealth}");
        }


        private static Fireball CreateAbility() => new(3f, 1f, 10, 0f, [], 50,25,
            [new DamageOverTurnEffect(3, 4, 0.7f, StatusEffects.Burning)], [], [],
            new Mock<IStanceMastery>().Object);

        private static IEntity Entity(out Fireball fireball)
        {
            var entity = new EntityTest();
            var secondEntity = new EntityTest();
            fireball = CreateAbility();
            fireball.SetOwner(secondEntity);
            return entity;
        }
    }
}
