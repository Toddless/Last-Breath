namespace LastBreathTest.PassiveTree
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Context;
    using Core.Session;
    using Moq;

    /// <summary>
    /// The context half of the tree's contribution. A pipeline applies its modifiers one after another
    /// instead of adding them up, so the tree hands over one modifier per knob reading the total of every
    /// node behind it — a modifier per node would compound into a factor nobody authored, and a
    /// whole-turn knob would round each node's share away to nothing before any of them counted.
    /// </summary>
    [TestClass]
    public class PassiveTreeContextSourceTests
    {
        private const string Seed = "start";
        private const float Heal = 100f;
        private const float Tolerance = 0.01f;

        [TestMethod]
        public void TwentyNodesOfTenPercentHealing_AddUpInsteadOfCompounding()
        {
            IPassiveTreeService service = Allocation(20, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            // 100 × (1 + 20 × 0.1) = 300. Twenty modifiers each scaling by 1.1 would land on 672.75.
            Assert.AreEqual(300f, Healed(fighter), Tolerance);
        }

        [TestMethod]
        public void TwentyNodesOfTenPercentDuration_ScaleAnEffectOnce()
        {
            IPassiveTreeService service = Allocation(20, ContextParameter.EffectDurationScale, ModifierValueType.Increase, 0.1f);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            // 4 × (1 + 20 × 0.1) = 12. Twenty separate scalings compound to 4 × 1.1²⁰ ≈ 27 instead.
            Assert.AreEqual(12, AppliedDuration(fighter, 4));
        }

        [TestMethod]
        public void AWholeTurnKnob_RoundsTheTotalAndNotEveryNode()
        {
            IPassiveTreeService service = Allocation(20, ContextParameter.BleedDuration, ModifierValueType.Flat, 0.5f);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            // Twenty half-turns are ten turns. Floored node by node they are twenty zeroes and the
            // effect never moves off its own duration.
            Assert.AreEqual(14, AppliedDuration(fighter, 4));
        }

        [TestMethod]
        public void OneKnobWrittenInTwoBuckets_IsStillOneKnobAndOneTotal()
        {
            // A bucket decides nothing about what a context line does: bindings are chosen by the
            // parameter and read the line's value, so "+10% healing" behaves the same written as Flat or
            // as Increase — the bucket only picks the words the line is printed in. Both spellings pass
            // the editor and the file reader, and splitting twenty nodes across them must not buy a
            // second modifier for the pipeline to apply after the first.
            IPassiveTreeService service = Allocation(
                ContextParameter.HealingEfficiency,
                0.1f,
                [.. Buckets(10, ModifierValueType.Flat), .. Buckets(10, ModifierValueType.Increase)]);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            Assert.AreEqual(1, service.ContextSource.Knobs.Count, "the buckets bought a modifier each");

            // 100 × (1 + 20 × 0.1) = 300. Ten and ten compounding land on 100 × 2² = 400.
            Assert.AreEqual(300f, Healed(fighter), Tolerance);
        }

        [TestMethod]
        public void EveryNodeFeedingOneKnob_LeavesTheFighterWithASingleModifier()
        {
            IPassiveTreeService service = Allocation(20, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            var handler = new Mock<IModifierHandlerComponent>();

            service.ContextSource.Attach(Fighter(handler));

            handler.Verify(mock => mock.Add(It.IsAny<IHealModifier>()), Times.Once);
        }

        [TestMethod]
        public void TheTreeRunsAheadOfTheNormalSlot()
        {
            IPassiveTreeService service = Allocation(1, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 1f);
            IFightable fighter = Fighter();
            List<float> seen = [];

            // Registered first on purpose: what decides the order has to be the scale and not the order
            // things were added, which is the whole reason the tree asks for a slot of its own.
            fighter.ModifierHandler.Add(new RecordingHealModifier(ContextModifierPriority.Normal, seen));
            service.ContextSource.Attach(fighter);
            Healed(fighter);

            CollectionAssert.AreEqual(new[] { 200f }, seen, "the tree ran after the Normal slot instead of before it");
        }

        [TestMethod]
        public void TheSlotTheTreeAsksFor_IsForTheKnobsThatPickedNone()
        {
            // The tree asks its modifiers to run at Innate so what the build IS stands ahead of the
            // passing tweaks at Normal instead of landing among them in attachment order. The ask is a
            // default and not an override: a conversion is written to be the last word on a hit — nothing
            // may run after it — and the node that grants one must not drag it to the front of the queue.
            Assert.AreEqual(
                ContextModifierPriority.Absolute,
                SlotOf(ContextParameter.PhysicalToFire),
                "a taken node moved a conversion off the slot its own class picked");

            Assert.AreEqual(
                ContextModifierPriority.Innate,
                SlotOf(ContextParameter.HealingEfficiency),
                "a knob that picked no slot of its own lost the one the tree asks for");
        }

        [TestMethod]
        public void AWholeUnitKnobsTotal_IsAnsweredAsThePipelineReadsIt()
        {
            // A reader of an allocation and the fighter carrying it ask one method, so the panel that
            // reports on five half-turns of bleed cannot credit the build with a turn no fight grants.
            List<ContextModifierLine> halves = [.. Enumerable.Range(0, 5).Select(_ =>
                new ContextModifierLine { Parameter = ContextParameter.BleedDuration, ValueType = ModifierValueType.Flat, Value = 0.5f })];

            Assert.AreEqual(2f, ContextKnobTotals.AsRead(ContextParameter.BleedDuration, halves), Tolerance);

            // A knob that is not counted in whole units keeps every bit of its total: the fraction is the
            // whole point of it.
            Assert.AreEqual(2.5f, ContextKnobTotals.AsRead(ContextParameter.HealingEfficiency, halves), Tolerance);
        }

        [TestMethod]
        public void TheModifierAKnobStandsBehind_ReadsTheTotalTheWayThePipelineDoes()
        {
            // The seam between the knob and the answer above it. A knob handing over the raw sum would
            // grant the same two turns in a fight — the binding floors what it reads either way — so
            // nothing in a battle would ever show it; what it would quietly change is every reading of
            // what the build is worth, which is the one place the fraction is visible.
            IPassiveTreeService service = Allocation(5, ContextParameter.BleedDuration, ModifierValueType.Flat, 0.5f);

            Assert.AreEqual(2f, service.ContextSource.ValueOf(ContextParameter.BleedDuration), Tolerance,
                "the knob's modifier reads the bare sum — 2.5 turns of bleed no pipeline will ever grant");
            Assert.AreEqual(0f, service.ContextSource.ValueOf(ContextParameter.HealingEfficiency), Tolerance,
                "a knob the allocation feeds nothing to answered with something");
        }

        [TestMethod]
        public void RefundingShrinksTheTotal_AndTheLastRefundTakesTheKnobAway()
        {
            IPassiveTreeService service = Allocation(3, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            Assert.AreEqual(130f, Healed(fighter), Tolerance);

            Assert.AreEqual(AllocationResult.Success, service.Refund(NodeId(3)));
            Assert.AreEqual(120f, Healed(fighter), Tolerance, "the refund did not come off the total");

            service.Respec();

            Assert.AreEqual(Heal, Healed(fighter), Tolerance, "the knob survived the last node that fed it");
            Assert.AreEqual(0, service.ContextSource.Knobs.Count);
        }

        [TestMethod]
        public void ANodeTakenAfterTheFighterWasWired_ReachesItWithoutBeingReAttached()
        {
            var service = new PassiveTreeService(new TreeProviderStub(
                Chain(ContextParameter.HealingEfficiency, 0.5f, Buckets(1, ModifierValueType.Increase))), ConditionCatalogs.Empty());
            service.SetTotalPoints(1);
            IFightable fighter = Fighter();

            service.ContextSource.Attach(fighter);
            Assert.AreEqual(Heal, Healed(fighter), Tolerance);

            Assert.AreEqual(AllocationResult.Success, service.Take(NodeId(1)));

            Assert.AreEqual(150f, Healed(fighter), Tolerance);
        }

        [TestMethod]
        public void ANewSession_TakesTheContributionOffTheFighter()
        {
            // The service is a singleton and outlives the scene: a second "New game" in one process
            // would otherwise leave the fresh character wearing the previous playthrough's knobs.
            IPassiveTreeService service = Allocation(3, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable fighter = Fighter();
            service.ContextSource.Attach(fighter);

            ((ISessionResettable)service).ResetSession();

            Assert.AreEqual(Heal, Healed(fighter), Tolerance);
        }

        [TestMethod]
        public void AttachingToASecondFighter_MovesTheContributionInsteadOfCopyingIt()
        {
            // World and arena build their own player objects out of one tree service.
            IPassiveTreeService service = Allocation(3, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable first = Fighter(id: "first");
            IFightable second = Fighter(id: "second");

            service.ContextSource.Attach(first);
            service.ContextSource.Attach(second);

            Assert.AreEqual(Heal, Healed(first), Tolerance, "the replaced fighter kept the tree's modifier");
            Assert.AreEqual(130f, Healed(second), Tolerance);
        }

        [TestMethod]
        public void DetachingByAFighterThatWasReplaced_LeavesTheCurrentOneAlone()
        {
            IPassiveTreeService service = Allocation(3, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable first = Fighter(id: "first");
            IFightable second = Fighter(id: "second");

            service.ContextSource.Attach(first);
            service.ContextSource.Attach(second);
            service.ContextSource.Detach(first);

            Assert.AreEqual(130f, Healed(second), Tolerance);
        }

        [TestMethod]
        public void Detach_LeavesTheFighterAsCleanAsItWasFound()
        {
            IPassiveTreeService service = Allocation(3, ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f);
            IFightable fighter = Fighter();

            service.ContextSource.Attach(fighter);
            service.ContextSource.Detach(fighter);

            Assert.AreEqual(Heal, Healed(fighter), Tolerance);

            // The allocation goes on changing after the fighter is gone; nothing of it may find its way back.
            service.Respec();

            Assert.AreEqual(Heal, Healed(fighter), Tolerance);
        }

        private static float Healed(IFightable fighter)
        {
            var context = new HealContext(fighter, fighter) { Amount = Heal };
            fighter.ModifierHandler.Apply(context);

            return context.Amount;
        }

        private static int AppliedDuration(IFightable fighter, int duration)
        {
            var effect = new Mock<IEffect>();
            effect.SetupProperty(mock => mock.Duration, duration);
            effect.SetupGet(mock => mock.Status).Returns(StatusEffects.Bleed);
            fighter.ModifierHandler.Apply(new EffectApplicationContext(fighter, fighter, effect.Object));

            return effect.Object.Duration;
        }

        /// <summary>The slot of the single modifier one taken node leaves on a fighter. Whichever pipeline
        /// the knob belongs to: the one thing handed to the handler is the modifier that stands for it.</summary>
        private static ContextModifierPriority SlotOf(ContextParameter parameter)
        {
            IPassiveTreeService service = Allocation(1, parameter, ModifierValueType.Increase, 0.5f);
            var handler = new Mock<IModifierHandlerComponent>();

            service.ContextSource.Attach(Fighter(handler));

            var modifier = handler.Invocations.Single().Arguments[0] as ContextModifier;
            Assert.IsNotNull(modifier, $"{parameter} reached no pipeline as a context modifier");

            return modifier!.Priority;
        }

        private static IFightable Fighter(Mock<IModifierHandlerComponent>? handler = null, string id = "fighter")
        {
            var fighter = new Mock<IFightable>();
            fighter.SetupGet(mock => mock.ModifierHandler).Returns(handler?.Object ?? new ModifierHandlerComponent());
            fighter.SetupGet(mock => mock.InstanceId).Returns(id);

            return fighter.Object;
        }

        /// <summary>A chain of identical nodes, all of it bought.</summary>
        private static IPassiveTreeService Allocation(int nodes, ContextParameter parameter, ModifierValueType valueType, float value) =>
            Allocation(parameter, value, Buckets(nodes, valueType));

        /// <summary>A chain of nodes all worth the same, one per bucket named — the way to write one knob
        /// in several buckets at once.</summary>
        private static IPassiveTreeService Allocation(ContextParameter parameter, float value, IReadOnlyList<ModifierValueType> buckets)
        {
            var service = new PassiveTreeService(new TreeProviderStub(Chain(parameter, value, buckets)), ConditionCatalogs.Empty());
            service.SetTotalPoints(buckets.Count);

            for (int index = 1; index <= buckets.Count; index++)
                Assert.AreEqual(AllocationResult.Success, service.Take(NodeId(index)), NodeId(index));

            return service;
        }

        /// <summary>A seed with a chain of small nodes hanging off it, one per bucket, every one carrying
        /// the same amount.</summary>
        private static PassiveTreeDocument Chain(ContextParameter parameter, float value, IReadOnlyList<ModifierValueType> buckets)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            string previous = Seed;
            for (int index = 1; index <= buckets.Count; index++)
            {
                var node = new PassiveNode { Id = NodeId(index), Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
                node.ContextModifiers.Add(new ContextModifierLine { Parameter = parameter, ValueType = buckets[index - 1], Value = value });
                document.AddNode(node);
                document.Link(previous, node.Id);
                previous = node.Id;
            }

            return document;
        }

        private static ModifierValueType[] Buckets(int nodes, ModifierValueType valueType) =>
            [.. Enumerable.Repeat(valueType, nodes)];

        private static string NodeId(int index) => $"small_{index}";

        private sealed class RecordingHealModifier(ContextModifierPriority priority, List<float> seen)
            : ContextModifier(priority, "Test_Heal_Recorder"), IHealModifier
        {
            public void Apply(IHealContext context) => seen.Add(context.Amount);
        }

        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
