namespace LastBreathTest.Combat
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
            var context = Damage(DamageCause.Ability, (DamageType.Physical, 5000f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(488f, remaining, 0.001f, "only health above the floor may burn");
            Assert.AreEqual(4512f, context.PreventedByStageGuard, 0.001f, "overkill is reported, not applied");
        }

        [TestMethod]
        public void StageGuardLetsSubFloorDamageThrough()
        {
            var target = CreateTarget(currentHealth: 750, barrier: 0, effects: [new StageGuardEffect(floorHealth: 262f)]);
            var context = Damage(DamageCause.Ability, (DamageType.Physical, 100f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(100f, remaining, 0.001f);
            Assert.AreEqual(0f, context.PreventedByStageGuard);
        }

        [TestMethod]
        public void LayersApplyInOrderShieldBarrierGuard()
        {
            // 1000 damage: shield eats 300, barrier eats 200, the guard allows only 100 above its floor.
            var target = CreateTarget(currentHealth: 500, barrier: 200,
                effects: [new ShieldEffect(strength: 300), new StageGuardEffect(floorHealth: 400f)]);
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 1000f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(300f, context.AbsorbedByShield, 0.001f);
            Assert.AreEqual(200f, context.AbsorbedByBarrier, 0.001f);
            Assert.AreEqual(100f, remaining, 0.001f);
            Assert.AreEqual(400f, context.PreventedByStageGuard, 0.001f);
        }

        [TestMethod]
        public void FullyAbsorbedDamageStopsTheChainEarly()
        {
            var target = CreateTarget(currentHealth: 500, barrier: 0, effects: [new ShieldEffect(strength: 300)]);
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 250f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(0f, remaining);
            Assert.AreEqual(250f, context.AbsorbedByShield, 0.001f);
        }

        [TestMethod]
        public void BlightWalksPastTheShieldAndTheBarrierUntouched()
        {
            var shield = new ShieldEffect(strength: 300);
            var target = CreateTarget(currentHealth: 500, barrier: 200, effects: [shield]);
            var context = Damage(DamageCause.Ability, (DamageType.Blight, 250f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(250f, remaining, 0.001f, "blight damages health directly");
            Assert.AreEqual(0f, context.AbsorbedByShield, "no absorption may report a bite of blight");
            Assert.AreEqual(0f, context.AbsorbedByBarrier);
            Assert.AreEqual(300f, shield.Strength, 0.001f, "the shield is left whole");
            Assert.AreEqual(200f, target.Object.CurrentBarrier, 0.001f, "the barrier is left whole");
        }

        [TestMethod]
        public void BlightDoesNotWalkPastTheStageGuard()
        {
            // A boss 88 health above his transition floor: a 5000 blight blow lands him exactly on it.
            var target = CreateTarget(currentHealth: 750, barrier: 0, effects: [new StageGuardEffect(floorHealth: 662f)]);
            var context = Damage(DamageCause.Ability, (DamageType.Blight, 5000f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(88f, remaining, 0.001f, "the anti-oneshot floor holds against blight too");
            Assert.AreEqual(4912f, context.PreventedByStageGuard, 0.001f);
        }

        [TestMethod]
        public void MixedHitSplits_TheBarrierEatsThePhysicalHalfOnly()
        {
            var target = CreateTarget(currentHealth: 500, barrier: 1000, effects: []);
            var context = Damage(DamageCause.Ability, (DamageType.Physical, 100f), (DamageType.Blight, 60f));

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(60f, remaining, 0.001f, "only the blight part reaches health");
            Assert.AreEqual(100f, context.AbsorbedByBarrier, 0.001f, "the barrier soaks the physical part");
            Assert.AreEqual(900f, target.Object.CurrentBarrier, 0.001f);
        }

        [TestMethod]
        public void IgnoreBarrierTakesTheWholeHitPastTheBarrierAsBefore()
        {
            var target = CreateTarget(currentHealth: 500, barrier: 1000, effects: []);
            var context = Damage(DamageCause.Attack, (DamageType.Physical, 100f), (DamageType.Fire, 40f));
            context.IgnoreBarrier = true;

            float remaining = DamageResolutionChain.CreateDefault().Apply(context, target.Object);

            Assert.AreEqual(140f, remaining, 0.001f, "the attacker's mark carries every component past the barrier");
            Assert.AreEqual(0f, context.AbsorbedByBarrier);
            Assert.AreEqual(1000f, target.Object.CurrentBarrier, 0.001f);
        }

        private static DamageContext Damage(DamageCause cause, params (DamageType Type, float Amount)[] components)
        {
            var context = new DamageContext { Source = Mock.Of<IFightable>(), Cause = cause };
            foreach ((DamageType type, float amount) in components)
                context.Add(type, amount);
            return context;
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
