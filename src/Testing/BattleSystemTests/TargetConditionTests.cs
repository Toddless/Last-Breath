namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Moq;

    /// <summary>
    /// The second family of predicates: the ones read about the fighter the owner is hitting instead of
    /// about the owner. They have no subject of their own — who the target is exists only while an attack
    /// is being resolved — so what has to be proven is both halves of that: the line counts while the hit
    /// it was written for is in flight, and it counts NOWHERE else. A predicate that cannot be answered
    /// answering "true" would put every such line on permanently, which is the failure this whole family
    /// is shaped around.
    /// </summary>
    [TestClass]
    public class TargetConditionTests
    {
        private const string Seed = "start";
        private const string Node = "small_1";
        private const string WhileTargetWounded = "Target_Below_Half";
        private const string WhileTargetHeld = "Target_Held";
        private const string WhileTargetWhole = "Target_Not_Below_Half";
        private const string WhileTargetsBarrierIsThin = "Target_Barrier_Below_Half";
        private const string WhileTargetClean = "Target_Carries_No_Debuff";
        private const float WoundedShare = 0.5f;
        private const float MaxHealth = 100f;
        private const float BaseStrength = 10f;
        private const float Grant = 6f;
        private const float Hit = 100f;
        private const float AddedShare = 0.5f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void ATargetLine_ReachesTheHitItWasWrittenForAndNothingElse()
        {
            // The knob is read while the damage of the attack is being shaped, which is inside the attack
            // the predicate was armed by. Outside it there is no target, so the same fighter swinging at
            // nobody carries none of the bonus.
            ConditionOwner attacker = Attacker(out IPassiveTreeService service, WhileTargetWounded);
            ConditionOwner target = Fighter(WoundedShare - 0.1f);

            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "the line counted before there was a target to read");

            Join(attacker, target);
            Assert.AreEqual(Hit * (1 + AddedShare), Dealt(attacker), Tolerance,
                "the attack named a wounded target and the line never reached the hit");

            Spend(attacker, target);
            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "the line kept counting after the attack it was armed by was over");

            Assert.IsTrue(service.IsTaken(Node), "reading a predicate must not touch the allocation");
        }

        [TestMethod]
        public void ATargetLine_ReadsTheTargetAndNeverTheOwner()
        {
            // The trap the family exists to avoid: reading "wounded" off whoever the line is attached to
            // would make "while the target is wounded" fire on a wounded attacker hitting a healthy enemy.
            ConditionOwner attacker = Attacker(out _, WhileTargetWounded);
            attacker.CurrentHealth = MaxHealth * (WoundedShare - 0.1f);

            Join(attacker, Fighter(1f));

            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "the predicate answered from the fighter carrying it");
        }

        [TestMethod]
        public void ATargetLine_IsAnsweredAfreshByEveryAttackOfTheSeries()
        {
            ConditionOwner attacker = Attacker(out _, WhileTargetWounded);
            ConditionOwner target = Fighter(1f);

            Join(attacker, target);
            Assert.AreEqual(Hit, Dealt(attacker), Tolerance);
            Spend(attacker, target);

            target.CurrentHealth = MaxHealth * (WoundedShare - 0.1f);
            Join(attacker, target);

            Assert.AreEqual(Hit * (1 + AddedShare), Dealt(attacker), Tolerance,
                "the second hit of a series read the verdict the first one was answered with");
        }

        [TestMethod]
        public void AnInvertedTargetLine_StaysOffWhileThereIsNobodyToRead()
        {
            // Inversion turns a verdict around; it must not turn "there is nothing to read" into one.
            ConditionOwner attacker = Attacker(out _, WhileTargetWhole);

            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "an inverted predicate counted with no target at all");

            Join(attacker, Fighter(1f));

            Assert.AreEqual(Hit * (1 + AddedShare), Dealt(attacker), Tolerance, "the inverted predicate never answered its target");
        }

        [TestMethod]
        public void AnInvertedTargetStatusLine_StaysOffWhileThereIsNobodyToRead()
        {
            // "While the target carries no debuff" is the shape that turns into a permanent bonus the
            // moment silence is read as a verdict: nobody carries a debuff when there is no target at all.
            ConditionOwner attacker = Attacker(out _, WhileTargetClean);

            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "an inverted status predicate counted with no target at all");

            ConditionOwner target = Fighter(1f);
            Join(attacker, target);
            Assert.AreEqual(Hit * (1 + AddedShare), Dealt(attacker), Tolerance, "the clean target was never read");

            Spend(attacker, target);
            target.ApplyStatus(StatusEffects.Poison);
            Join(attacker, target);
            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "the line counted on a target the record excludes");
        }

        [TestMethod]
        public void ATargetLineOverAVitalTheTargetDoesNotHave_IsMetByNothing()
        {
            // The owner-side rule, held on the target side: nobody is low on a barrier he never had, and
            // "no barrier" must not read as "an empty one" for a threshold.
            ConditionOwner attacker = Attacker(out _, WhileTargetsBarrierIsThin);

            Join(attacker, Fighter(1f));

            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "a target with no barrier was counted as having a thin one");
        }

        [TestMethod]
        public void ATargetStatusLine_ReadsTheStatusesOfTheOneBeingHit()
        {
            ConditionOwner attacker = Attacker(out _, WhileTargetHeld);
            ConditionOwner target = Fighter(1f);

            Join(attacker, target);
            Assert.AreEqual(Hit, Dealt(attacker), Tolerance, "the line counted on a target carrying no status it names");

            Spend(attacker, target);
            target.ApplyStatus(StatusEffects.Stun);
            Join(attacker, target);

            Assert.AreEqual(Hit * (1 + AddedShare), Dealt(attacker), Tolerance, "the held target was never read");
        }

        [TestMethod]
        public void ATargetIdOnAParameterLine_CountsForTheAttackAndNotBeyondIt()
        {
            // The same catalog id on the other kind of consumer. A line carries an id and nothing else, so
            // the parametric channel must take a target-side predicate without knowing there are two
            // families — and it must not be left counting once the hit is over.
            ConditionOwner attacker = Fighter(1f);
            attacker.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            var line = new ConditionalModifier(1f, ModifierValueType.Flat, EntityParameter.Strength, Grant, Resolved(WhileTargetWounded), "test");
            line.ApplyTo(attacker);
            ConditionOwner target = Fighter(WoundedShare - 0.1f);

            Assert.AreEqual(BaseStrength, Strength(attacker), Tolerance, "the parameter carried the line with no target to read");

            Join(attacker, target);
            Assert.AreEqual(BaseStrength + Grant, Strength(attacker), Tolerance, "the flip never reached the parameter");

            Spend(attacker, target);
            Assert.AreEqual(BaseStrength, Strength(attacker), Tolerance, "the parameter kept the line after the attack was over");
        }

        [TestMethod]
        public void LettingTheOwnerGo_TakesTheTargetAndTheWatchWithIt()
        {
            ConditionOwner attacker = Fighter(1f);
            int alone = attacker.ListenerCount;
            ICondition condition = Resolved(WhileTargetWounded);

            condition.Attach(attacker);
            Assert.IsTrue(attacker.ListenerCount > alone, "the predicate never subscribed to the fighter it reads attacks off");

            Join(attacker, Fighter(WoundedShare - 0.1f));
            Assert.IsTrue(condition.IsMet, "the fixture never got the predicate on");

            condition.Detach();

            Assert.IsFalse(condition.IsMet, "a released predicate still answers from the last fighter its owner swung at");
            Assert.AreEqual(alone, attacker.ListenerCount, "the released predicate is still listening for attacks");
        }

        [TestMethod]
        public void ACopyOfAnArmedPredicate_ArrivesWithNobodyToRead()
        {
            // Copies are handed out while a fight is running (a line rebuilt mid-battle), and a clone that
            // kept the original's target would answer about a fighter it was never pointed at.
            ConditionOwner attacker = Fighter(1f);
            ICondition original = Resolved(WhileTargetWounded);
            original.Attach(attacker);
            Join(attacker, Fighter(WoundedShare - 0.1f));
            Assert.IsTrue(original.IsMet, "the fixture never armed the original");

            ICondition copy = original.Copy();
            copy.Attach(Fighter(1f));

            Assert.IsFalse(copy.IsMet, "the clone answered from the target the original was pointed at");
        }

        /// <summary>What the attacker's hit comes to: an attack-caused damage context of his, run through
        /// his own pipelines the way a defender runs it. The tree's knob adds a share of it back as fire,
        /// so the total says whether the conditional line was counted.</summary>
        private static float Dealt(IFightable attacker)
        {
            var context = new DamageContext { Source = attacker, Cause = DamageCause.Attack };
            context.Add(DamageType.Physical, Hit);
            attacker.ModifierHandler.Apply(context);

            return context.TotalDamage;
        }

        private static float Strength(ConditionOwner fighter) => fighter.Parameters.GetValueForParameter(EntityParameter.Strength);

        /// <summary>The attack joined, as the attacker's own bus announces it.</summary>
        private static void Join(ConditionOwner attacker, IFightable target) =>
            attacker.CombatEvents.Publish(new BeforeAttackEvent(Attack(target)));

        private static void Spend(ConditionOwner attacker, IFightable target) =>
            attacker.CombatEvents.Publish(new AfterAttackEvent(Attack(target)));

        private static IAttackContext Attack(IFightable target)
        {
            var context = new Mock<IAttackContext>();
            context.SetupGet(attack => attack.Target).Returns(target);

            return context.Object;
        }

        private static ICondition Resolved(string id)
        {
            Assert.IsTrue(Catalog().TryResolve(id, out ICondition? condition), $"the fixture catalog refused '{id}'");

            return condition!;
        }

        /// <summary>A fighter carrying one taken node whose context line is gated by the given id.</summary>
        private static ConditionOwner Attacker(out IPassiveTreeService service, string condition)
        {
            service = new PassiveTreeService(new TreeStub(Tree(condition)), Catalog());
            service.SetTotalPoints(1);
            Assert.AreEqual(AllocationResult.Success, service.Take(Node), "the fixture never bought its node");

            ConditionOwner attacker = Fighter(1f);
            service.ContextSource.Attach(attacker);

            return attacker;
        }

        private static ConditionOwner Fighter(float healthShare)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            fighter.CurrentHealth = MaxHealth * healthShare;

            return fighter;
        }

        /// <summary>A seed with one bought node hanging off it, carrying an outgoing-damage knob: the
        /// cheapest line whose effect on a hit can be read off the damage the attacker deals.</summary>
        private static PassiveTreeDocument Tree(string condition)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.ContextModifiers.Add(new ContextModifierLine
            {
                Parameter = ContextParameter.AddedFireDamage,
                ValueType = ModifierValueType.Increase,
                Value = AddedShare,
                Condition = condition
            });
            document.AddNode(node);
            document.Link(Seed, Node);

            return document;
        }

        private static ConditionProvider Catalog() => ConditionCatalogs.Of(
            $$"""{ "id": "{{WhileTargetWounded}}", "type": "{{ConditionTypes.TargetResourceThreshold}}", "resource": "Health", "value": {{WoundedShare}} }""",
            $$"""{ "id": "{{WhileTargetWhole}}", "type": "{{ConditionTypes.TargetResourceThreshold}}", "resource": "Health", "value": {{WoundedShare}}, "negate": true }""",
            $$"""{ "id": "{{WhileTargetsBarrierIsThin}}", "type": "{{ConditionTypes.TargetResourceThreshold}}", "resource": "Barrier", "value": {{WoundedShare}} }""",
            $$"""{ "id": "{{WhileTargetHeld}}", "type": "{{ConditionTypes.TargetStatus}}", "statuses": [ "{{nameof(StatusMasks.Control)}}" ] }""",
            $$"""{ "id": "{{WhileTargetClean}}", "type": "{{ConditionTypes.TargetStatus}}", "statuses": [ "{{nameof(StatusMasks.Debuff)}}" ], "negate": true }""");

        private sealed class TreeStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
