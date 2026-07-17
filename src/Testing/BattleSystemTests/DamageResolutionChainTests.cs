namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using Battle.Source.Effects;
    using Core.Battle.Abilities;
    using Core.Battle.DamageResolution;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    [TestClass]
    public class DamageResolutionChainTests
    {
        [TestMethod]
        public void StageGuardClampsLethalDamageToTheFloor()
        {
            var target = CreateTarget(currentHealth: 750, barrier: 0, effects: [new StageGuardEffect(floorHealth: 262f)]);
            var context = new DamageContext { Source = Mock.Of<IFightable>(), Cause = DamageCause.Ability };

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object, 5000f);

            Assert.AreEqual(488f, remaining, 0.001f, "only health above the floor may burn");
            Assert.AreEqual(4512f, context.PreventedByStageGuard, 0.001f, "overkill is reported, not applied");
        }

        [TestMethod]
        public void StageGuardLetsSubFloorDamageThrough()
        {
            var target = CreateTarget(currentHealth: 750, barrier: 0, effects: [new StageGuardEffect(floorHealth: 262f)]);
            var context = new DamageContext { Source = Mock.Of<IFightable>(), Cause = DamageCause.Ability };

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object, 100f);

            Assert.AreEqual(100f, remaining, 0.001f);
            Assert.AreEqual(0f, context.PreventedByStageGuard);
        }

        [TestMethod]
        public void LayersApplyInOrderShieldBarrierGuard()
        {
            // 1000 damage: shield eats 300, barrier eats 200, the guard allows only 100 above its floor.
            var target = CreateTarget(currentHealth: 500, barrier: 200,
                effects: [new ShieldEffect(strength: 300), new StageGuardEffect(floorHealth: 400f)]);
            var context = new DamageContext { Source = Mock.Of<IFightable>(), Cause = DamageCause.Attack };

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object, 1000f);

            Assert.AreEqual(300f, context.AbsorbedByShield, 0.001f);
            Assert.AreEqual(200f, context.AbsorbedByBarrier, 0.001f);
            Assert.AreEqual(100f, remaining, 0.001f);
            Assert.AreEqual(400f, context.PreventedByStageGuard, 0.001f);
        }

        [TestMethod]
        public void FullyAbsorbedDamageStopsTheChainEarly()
        {
            var target = CreateTarget(currentHealth: 500, barrier: 0, effects: [new ShieldEffect(strength: 300)]);
            var context = new DamageContext { Source = Mock.Of<IFightable>(), Cause = DamageCause.Attack };

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object, 250f);

            Assert.AreEqual(0f, remaining);
            Assert.AreEqual(250f, context.AbsorbedByShield, 0.001f);
        }

        private static Mock<IFightable> CreateTarget(float currentHealth, float barrier, List<IEffect> effects)
        {
            var effectsComponent = new Mock<IEffectsComponent>();
            effectsComponent.Setup(e => e.GetBy(It.IsAny<System.Func<IEffect, bool>>()))
                .Returns((System.Func<IEffect, bool> filter) => effects.FindAll(e => filter(e)));

            var target = new Mock<IFightable>();
            target.SetupGet(t => t.CurrentHealth).Returns(currentHealth);
            target.SetupProperty(t => t.CurrentBarrier, barrier);
            target.SetupGet(t => t.Effects).Returns(effectsComponent.Object);
            return target;
        }
    }
}
