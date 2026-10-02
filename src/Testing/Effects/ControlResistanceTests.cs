namespace LastBreathTest.Effects
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;

    [TestClass]
    public class ControlResistanceTests
    {
        private static readonly ControlResistanceRules Rules = new(
            StatusEffects.Stun | StatusEffects.Freeze,
            [1f, 0.5f, 0.25f],
            new HashSet<EntityType> { EntityType.Boss, EntityType.Archon },
            ResistanceDecayTurns: 3);

        [TestMethod]
        public void HardControlDurationsDiminishThenResist()
        {
            var modifier = new ControlResistanceModifier(Rules);

            // 2-turn stuns: full → half → quarter (ceil) → resisted outright.
            int[] durations = [2, 2, 2];
            var results = new List<int>();
            foreach (int duration in durations)
            {
                var context = CreateContext(StatusEffects.Stun, duration);
                modifier.Apply(context.Context);
                Assert.IsFalse(context.Context.Rejected);
                results.Add(context.Effect.Object.Duration);
            }

            CollectionAssert.AreEqual(new[] { 2, 1, 1 }, results);

            var fourth = CreateContext(StatusEffects.Freeze, 2);
            modifier.Apply(fourth.Context);
            Assert.IsTrue(fourth.Context.Rejected, "4th hard CC must be resisted outright");
        }

        [TestMethod]
        public void ResistanceDecaysBackOverTurns()
        {
            var modifier = new ControlResistanceModifier(Rules);
            // Push to full resistance (3 applications), then wait it out: decay is list.Count/decayTurns per turn.
            for (int i = 0; i < 3; i++)
                modifier.Apply(CreateContext(StatusEffects.Stun, 2).Context);

            var blocked = CreateContext(StatusEffects.Stun, 2);
            modifier.Apply(blocked.Context);
            Assert.IsTrue(blocked.Context.Rejected);

            // 3 decay turns (decayTurns = 3) bring resistance back to zero → full-duration stun again.
            for (int turn = 0; turn < 3; turn++)
                modifier.DecayTick();

            var fresh = CreateContext(StatusEffects.Stun, 2);
            modifier.Apply(fresh.Context);
            Assert.IsFalse(fresh.Context.Rejected);
            Assert.AreEqual(2, fresh.Effect.Object.Duration);
        }

        [TestMethod]
        public void NonControlEffectsPassUntouched()
        {
            var modifier = new ControlResistanceModifier(Rules);
            var poison = CreateContext(StatusEffects.Poison, 5);

            modifier.Apply(poison.Context);

            Assert.IsFalse(poison.Context.Rejected);
            Assert.AreEqual(5, poison.Effect.Object.Duration);
            poison.Effect.VerifySet(e => e.Duration = It.IsAny<int>(), Times.Never);
        }

        private static (IncomingEffectContext Context, Mock<IEffect> Effect) CreateContext(StatusEffects status, int duration)
        {
            var effect = new Mock<IEffect>();
            effect.SetupGet(e => e.Status).Returns(status);
            effect.SetupProperty(e => e.Duration, duration);
            var context = new IncomingEffectContext(Mock.Of<Core.Entity.IFightable>(), Mock.Of<Core.Entity.IFightable>(), effect.Object);
            return (context, effect);
        }
    }
}
