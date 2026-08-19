namespace LastBreathTest.BattleSystemTests
{
    using System;
    using Battle.Source.Abilities;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Moq;

    /// <summary>
    /// Which reactions are ANSWERS, down the road the fight uses: <c>CreateReaction</c> spawns the swing
    /// and <c>ToImpact</c> hands it to the riders as a genus. Both halves are the real thing.
    ///
    /// <para><b>The rule, and why depth cannot express it.</b> A counter is the man who was hit hitting
    /// back; an extra swing off the chain passive is the attacker swinging again at his own target. Both
    /// are reactions and both sit at depth one, so the depth tells them apart not at all — what does is
    /// WHO is swinging. It matters because a record bought for a series of attacks is bought for the
    /// attacks the player aimed: counting the swings he was provoked into would pay it out for being
    /// attacked, and reading a chain hit as a stranger would stop paying for a swing he did buy.</para>
    ///
    /// <para><b>Why the two passives are not run here.</b> Both gate on a chance rolled through the
    /// attack's own generator, and that generator is a Godot object no test host can build — the reason
    /// <c>ChanceRoll</c> carries a bare-draw overload at all, and the reason the whole process dies on
    /// the attempt rather than failing an assertion. What the walks reproduce instead is the exact call
    /// each passive makes, named after it: the counter reacts with <c>Context.Target</c> as the swinger
    /// (<c>CounterAttackPassiveSkill</c>), the chain hit with <c>Context.Attacker</c>
    /// (<c>ChainAttackPassiveSkill</c>). Those two argument shapes ARE the difference under test.</para>
    /// </summary>
    [TestClass]
    public class ReactionImpactKindTests
    {
        [TestMethod]
        public void TheCounterIsAnAnswerAndArrivesAsAReaction()
        {
            // CounterAttackPassiveSkill: the evader — the TARGET of the swing — hits the attacker back.
            IAttackContext swing = Swing();
            IAttackContext counter = swing.CreateReaction(swing.Target, swing.Attacker, 10f);

            Assert.AreEqual(1, counter.ReactionDepth, "the counter is not a reaction at all, so the walk measures nothing");
            Assert.IsTrue(counter.IsAnswer, "the man who was hit hitting back was not read as an answer");
            Assert.AreEqual(ImpactKind.Reaction, KindOf(counter), "the counter arrived at the riders as an ordinary attack");
        }

        [TestMethod]
        public void TheChainHitIsNoAnswerAndStaysAnAttack()
        {
            // ChainAttackPassiveSkill: the ATTACKER swings again at the same target. Same road, same
            // depth, opposite answer — it is the series he bought carrying on.
            IAttackContext swing = Swing();
            IAttackContext extra = swing.CreateReaction(swing.Attacker, swing.Target, 10f);

            Assert.AreEqual(1, extra.ReactionDepth, "the chain hit stopped being a reaction, so the walk no longer measures the case");
            Assert.IsFalse(extra.IsAnswer, "the attacker swinging again at his own target was read as somebody answering him");
            Assert.AreEqual(ImpactKind.Attack, KindOf(extra), "a swing of the attacker's own series stopped counting as an attack");
        }

        [TestMethod]
        public void AnAnswerToAnAnswerIsStillAnAnswer()
        {
            // Depth two, and the reason the question is put to the swinger rather than to the chain: the
            // first attacker striking back at the man who countered him is answering in his turn, and his
            // own attack-series records may not be paid for it.
            IAttackContext swing = Swing();
            IAttackContext counter = swing.CreateReaction(swing.Target, swing.Attacker, 10f);
            IAttackContext back = counter.CreateReaction(counter.Target, counter.Attacker, 10f);

            Assert.AreEqual(2, back.ReactionDepth, "the second answer is not two deep, so the walk measures nothing");
            Assert.IsTrue(back.IsAnswer, "answering an answer stopped being an answer");
            Assert.AreEqual(ImpactKind.Reaction, KindOf(back), "the answer to an answer arrived as an ordinary attack");
        }

        [TestMethod]
        public void APlannedSwingAnswersNobody()
        {
            // The floor the three above stand on: an attack nobody provoked is not an answer, so a book
            // with no reactions in it at all reads exactly as it always did.
            IAttackContext swing = Swing();

            Assert.AreEqual(0, swing.ReactionDepth, "a planned attack came out of a reaction chain");
            Assert.IsFalse(swing.IsAnswer, "an attack nobody provoked was read as an answer");
            Assert.AreEqual(ImpactKind.Attack, KindOf(swing), "a planned attack stopped arriving as an attack");
        }

        /// <summary>The genus the delivery would hand the riders for that swing.</summary>
        private static ImpactKind KindOf(IAttackContext context) =>
            context.ToImpact(Mock.Of<IBattleField>(), Mock.Of<IAbility>()).Kind;

        /// <summary>One ordinary planned attack. The generator is never touched — nothing below rolls
        /// anything — and it is the one thing here that could not be built at all.</summary>
        private static IAttackContext Swing() =>
            new AttackContext(Fighter(), Fighter(), 10f, null!, Mock.Of<IAttackContextScheduler>());

        private static IFightable Fighter()
        {
            var fighter = new Mock<IFightable>();
            fighter.SetupGet(entity => entity.InstanceId).Returns(Guid.NewGuid().ToString());
            fighter.SetupGet(entity => entity.IsAlive).Returns(true);
            fighter.SetupGet(entity => entity.Parameters).Returns(Mock.Of<IEntityParametersComponent>());

            return fighter.Object;
        }
    }
}
