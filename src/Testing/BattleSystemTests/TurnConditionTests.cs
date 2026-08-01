namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Moq;

    /// <summary>
    /// The third family of predicates: the ones read about the turn the owner is in. They hold state the
    /// owner does not — a tally that empties every turn — so what has to be proven is where that tally is
    /// emptied and where it is not: the owner's own turn empties it, anybody else's turn does not, and the
    /// end of a battle takes it away entirely.
    /// <para>The other half is the shape of the family: "the first strike of the turn" is written as the
    /// inverted form of a tally, so a tally that never fills makes such a line permanent. A predicate with
    /// no turn behind it — out of combat, before the owner's first turn — must therefore be met by nothing,
    /// inverted or not, and every action the family claims to count has to actually be counted.</para>
    /// </summary>
    [TestClass]
    public class TurnConditionTests
    {
        private const string WhileFirstAttack = "Turn_Before_Any_Attack";
        private const string AfterAnAttack = "Turn_After_An_Attack";
        private const string WhileFirstAbility = "Turn_Before_Any_Ability";
        private const string WhileUndamaged = "Turn_Not_Damaged";
        private const string WhileUnattacked = "Turn_Not_Attacked";
        private const string WhileOpeningTurns = "Battle_Before_Its_Fourth_Turn";
        private const string WhileDraggingOn = "Battle_From_Its_Fourth_Turn";
        private const int Boundary = 4;
        private const float MaxHealth = 100f;
        private const float BaseStrength = 10f;
        private const float Grant = 6f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void ATurnLine_IsMetByNothingUntilTheOwnerIsGivenATurn()
        {
            // The shape the whole family is written in: "the first strike of the turn" is a tally read
            // backwards, and out of combat the tally is empty. Answering from it there would hand the bonus
            // to a character standing in a field — and to every fighter waiting for his first turn.
            var owner = new ConditionOwner();

            foreach (string id in new[] { WhileFirstAttack, AfterAnAttack, WhileFirstAbility, WhileUndamaged, WhileUnattacked, WhileOpeningTurns, WhileDraggingOn })
                Assert.IsFalse(Attached(id, owner).IsMet, $"'{id}' answered with no turn to read");
        }

        [TestMethod]
        public void TheFirstAbilityOfATurn_IsTheOneCastBeforeAnyHasFinished()
        {
            var owner = new ConditionOwner();
            ICondition first = Attached(WhileFirstAbility, owner);

            StartTurn(owner);
            Assert.IsTrue(first.IsMet, "the turn started and the line never armed");

            Cast(owner);
            Assert.IsFalse(first.IsMet, "the line kept counting after the cast it was written for");

            StartTurn(owner);
            Assert.IsTrue(first.IsMet, "the next turn never gave the line back");
        }

        [TestMethod]
        public void APlainRecord_CountsUpToWhatItAsksForInsteadOfReadingBackwards()
        {
            // The uninverted half of the family: the tally itself, not the turn before it. Both halves are
            // one record apart, so the count has to mean the same thing whichever way the flag reads.
            var owner = new ConditionOwner();
            ICondition spent = Attached(AfterAnAttack, owner);
            StartTurn(owner);

            Assert.IsFalse(spent.IsMet, "a tally of one held on a turn nothing was spent in");

            Attack(owner);
            Assert.IsTrue(spent.IsMet, "the attack never reached the tally");

            StartTurn(owner);
            Assert.IsFalse(spent.IsMet, "the new turn started with the previous turn's attack still counted");
        }

        [TestMethod]
        public void TheFirstAttackOfATurn_IsTheOneMadeBeforeAnyIsSpent()
        {
            var owner = new ConditionOwner();
            ICondition first = Attached(WhileFirstAttack, owner);

            StartTurn(owner);
            Assert.IsTrue(first.IsMet, "the turn started and the line never armed");

            Attack(owner);
            Assert.IsFalse(first.IsMet, "the line kept counting after the attack it was written for");

            Attack(owner);
            Assert.IsFalse(first.IsMet);
        }

        [TestMethod]
        public void AnAttackJoinedButNotYetSpent_IsStillTheFirstOne()
        {
            // What shapes the numbers of an attack is read while the attack is in flight. A tally filled on
            // the way in would leave the first attack of the turn without the line written for it and hand
            // it to the second.
            var owner = new ConditionOwner();
            ICondition first = Attached(WhileFirstAttack, owner);
            StartTurn(owner);

            owner.CombatEvents.Publish(new BeforeAttackEvent(Mock.Of<IAttackContext>()));

            Assert.IsTrue(first.IsMet, "the attack was counted before it was resolved");
        }

        [TestMethod]
        public void TheOwnersNextTurn_EmptiesTheTally()
        {
            var owner = new ConditionOwner();
            ICondition first = Attached(WhileFirstAttack, owner);
            StartTurn(owner);
            Attack(owner);

            StartTurn(owner);

            Assert.IsTrue(first.IsMet, "a new turn left the previous turn's attack on the tally");
        }

        [TestMethod]
        public void ATurnThatIsNotTheOwners_LeavesTheTallyAlone()
        {
            // TurnStartEvent reaches three buses and the other two carry the turn of whoever is playing.
            // A reset taken from either would rearm the line on every enemy's turn as well.
            var owner = new ConditionOwner();
            var other = new ConditionOwner();
            ICondition first = Attached(WhileFirstAttack, owner);
            StartTurn(owner);
            Attack(owner);

            owner.CombatEvents.Publish(new TurnStartEvent(other));
            Assert.IsFalse(first.IsMet, "somebody else's turn emptied the owner's tally");

            other.CombatEvents.Publish(new TurnStartEvent(owner));
            Assert.IsFalse(first.IsMet, "the predicate listens to a bus other than the owner's own");
        }

        [TestMethod]
        public void TakingDamage_SpendsTheUndamagedLineForTheRestOfTheTurn()
        {
            var owner = new ConditionOwner();
            ICondition untouched = Attached(WhileUndamaged, owner);
            StartTurn(owner);
            Assert.IsTrue(untouched.IsMet);

            Hurt(owner, DamageCause.Effect);
            Assert.IsFalse(untouched.IsMet, "damage landed and the line kept counting");

            StartTurn(owner);
            Assert.IsTrue(untouched.IsMet, "the next turn never gave the line back");
        }

        [TestMethod]
        public void AnAttackThatNeverLanded_StillCountsAsBeingAttacked()
        {
            // The difference between the two incoming tallies, and the whole point of the one behind the
            // evasion line: a swing that was evaded hurt nobody, but it was still a swing at the owner. A
            // line keyed to damage would survive every evaded attack and become a permanent bonus.
            foreach (Action<ConditionOwner> swing in new Action<ConditionOwner>[] { Evade, Block, owner => Hurt(owner, DamageCause.Attack) })
            {
                var owner = new ConditionOwner();
                ICondition unattacked = Attached(WhileUnattacked, owner);
                StartTurn(owner);
                Assert.IsTrue(unattacked.IsMet);

                swing(owner);

                Assert.IsFalse(unattacked.IsMet, "an attack resolved against the owner left the line counting");
            }
        }

        [TestMethod]
        public void DamageNobodySwung_LeavesTheAttackedLineAlone()
        {
            var owner = new ConditionOwner();
            ICondition unattacked = Attached(WhileUnattacked, owner);
            StartTurn(owner);

            Hurt(owner, DamageCause.Effect);

            Assert.IsTrue(unattacked.IsMet, "poison ticking on the owner was counted as an attack on him");
        }

        [TestMethod]
        public void TheEndOfTheBattle_TakesTheTurnAndTheVerdictWithIt()
        {
            // The battle bus dies with the battle; this family never touches it. What it hears is the end of
            // the fight republished on the owner's own bus, and that has to put it back to having no turn —
            // otherwise the last turn's verdict follows the fighter into the world and into the next fight.
            var owner = new ConditionOwner();
            ICondition first = Attached(WhileFirstAttack, owner);
            StartTurn(owner);
            int flips = 0;
            first.StateChanged += met => flips++;

            owner.CombatEvents.Publish(new BattleEndEvent(BattleResults.PlayerWon));

            Assert.IsFalse(first.IsMet, "the line survived the battle it was counted in");
            Assert.AreEqual(1, flips, "the parameter behind the line was never told the line went out");
        }

        [TestMethod]
        public void ANewBattleCountsItsTurnsFromTheStart()
        {
            var owner = new ConditionOwner();
            ICondition dragging = Attached(WhileDraggingOn, owner);
            for (int turn = 0; turn < Boundary; turn++) StartTurn(owner);
            Assert.IsTrue(dragging.IsMet, "the fixture never got the line on");

            owner.CombatEvents.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            StartTurn(owner);

            Assert.IsFalse(dragging.IsMet, "the new battle started on the turn count of the previous one");
        }

        [TestMethod]
        public void TheOpeningTurns_CoverEverythingBeforeTheTurnTheRecordNames()
        {
            var owner = new ConditionOwner();
            ICondition opening = Attached(WhileOpeningTurns, owner);
            ICondition dragging = Attached(WhileDraggingOn, owner);

            for (int turn = 1; turn < Boundary; turn++)
            {
                StartTurn(owner);
                Assert.IsTrue(opening.IsMet, $"turn {turn} is before the boundary and the opening line was off");
                Assert.IsFalse(dragging.IsMet, $"turn {turn} is before the boundary and the drawn-out line was on");
            }

            StartTurn(owner);

            Assert.IsFalse(opening.IsMet, "the opening line covered the turn the record draws its line at");
            Assert.IsTrue(dragging.IsMet, "the drawn-out line never armed on the turn the record names");
        }

        [TestMethod]
        public void ATurnIdOnAParameterLine_CountsForTheTurnAndNotBeyondIt()
        {
            // The same catalog id on a consumer that knows nothing of families: a line carries an id, and
            // the parametric channel has to take a turn-scoped predicate and let go of it on time.
            var owner = new ConditionOwner();
            owner.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            var line = new ConditionalModifier(1f, ModifierValueType.Flat, EntityParameter.Strength, Grant, Resolved(WhileFirstAttack), "test");
            line.ApplyTo(owner);

            Assert.AreEqual(BaseStrength, Strength(owner), Tolerance, "the parameter carried the line with no turn to read");

            StartTurn(owner);
            Assert.AreEqual(BaseStrength + Grant, Strength(owner), Tolerance, "the flip never reached the parameter");

            Attack(owner);
            Assert.AreEqual(BaseStrength, Strength(owner), Tolerance, "the parameter kept the line after the attack was spent");
        }

        [TestMethod]
        public void EveryActionTheFamilySaysItCounts_IsWiredEndToEnd()
        {
            // An action that is claimed but follows nothing keeps its tally at zero forever, which turns
            // every "I have not ..." line written on it into a permanent bonus. The table is the claim and
            // this is the proof, so a member added without a signal fails here instead of in a fight.
            foreach (TurnAction action in Enum.GetValues<TurnAction>())
            {
                Assert.IsTrue(TurnActionCondition.CanCount(action), $"'{action}' is a turn action the family cannot count");

                var owner = new ConditionOwner();
                var condition = new TurnActionCondition(action, count: 1);
                condition.Attach(owner);
                StartTurn(owner);

                Spend(action, owner);

                Assert.IsTrue(condition.IsMet, $"'{action}' was spent and the tally never moved");
            }
        }

        [TestMethod]
        public void ARecordThatWouldCountNothing_IsRefusedInsteadOfBecomingAlwaysOn()
        {
            var provider = ConditionCatalogs.Of(
                $$"""{ "id": "No_Such_Action", "type": "{{ConditionTypes.TurnAction}}", "action": "Whistling" }""",
                $$"""{ "id": "Countless", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.Attack)}}", "count": 0 }""",
                $$"""{ "id": "No_Boundary", "type": "{{ConditionTypes.BattleTurn}}" }""",
                $$"""{ "id": "First_Turn_Boundary", "type": "{{ConditionTypes.BattleTurn}}", "turn": 1 }""");

            foreach (string id in new[] { "No_Such_Action", "Countless", "No_Boundary", "First_Turn_Boundary" })
                Assert.IsFalse(provider.TryResolve(id, out _),
                    $"'{id}' is a record that draws no line and it became a live condition anyway");
        }

        [TestMethod]
        public void LettingTheOwnerGo_TakesEverySubscriptionWithIt()
        {
            var owner = new ConditionOwner();
            int alone = owner.ListenerCount;
            ICondition unattacked = Resolved(WhileUnattacked);

            unattacked.Attach(owner);
            Assert.IsTrue(owner.ListenerCount > alone, "the predicate never subscribed to the fighter whose turns it counts");

            StartTurn(owner);
            Assert.IsTrue(unattacked.IsMet, "the fixture never got the predicate on");

            unattacked.Detach();

            Assert.IsFalse(unattacked.IsMet, "a released predicate still answers from the turn it was let go in");
            Assert.AreEqual(alone, owner.ListenerCount, "the released predicate is still listening to the fighter");
        }

        [TestMethod]
        public void ACopyOfALivePredicate_ArrivesWithNoTurnAndLeavesTheOriginalWhereItWas()
        {
            // Copies are handed out while a fight is running. The clone must start with nothing — no turn,
            // no tally, nothing subscribed — and it must not take the original's subscriptions with it: the
            // original is still counting a fighter's turns and still has to be releasable.
            var owner = new ConditionOwner();
            int alone = owner.ListenerCount;
            ICondition original = Resolved(WhileFirstAttack);
            original.Attach(owner);
            StartTurn(owner);
            Assert.IsTrue(original.IsMet, "the fixture never armed the original");

            ICondition copy = original.Copy();
            var second = new ConditionOwner();
            copy.Attach(second);

            Assert.IsFalse(copy.IsMet, "the clone answered from the turn the original was counting");
            original.Detach();
            Assert.AreEqual(alone, owner.ListenerCount, "the clone carried off what the original had to release");

            copy.Detach();
            Assert.AreEqual(0, second.ListenerCount - alone, "the clone left its own subscriptions behind");
        }

        [TestMethod]
        public void TheShippedTurnLinesBehaveAsTheirIdsRead()
        {
            // Catalog entry to predicate to behaviour, on the ids the game actually ships: the whole point
            // of the catalog is that a line carries an id, so an id whose record says something other than
            // its name is a bonus nobody can find.
            IConditionProvider catalog = ShippedCatalog();
            var owner = new ConditionOwner();
            ICondition firstAttack = Shipped(catalog, "Turn_First_Attack", owner);
            ICondition undamaged = Shipped(catalog, "Turn_Undamaged", owner);
            ICondition unattacked = Shipped(catalog, "Turn_Unattacked", owner);

            Assert.IsFalse(firstAttack.IsMet, "the shipped line counted out of combat");
            Assert.IsFalse(undamaged.IsMet, "the shipped line counted out of combat");

            StartTurn(owner);
            Assert.IsTrue(firstAttack.IsMet);
            Assert.IsTrue(undamaged.IsMet);
            Assert.IsTrue(unattacked.IsMet);

            Attack(owner);
            Hurt(owner, DamageCause.Effect);
            Evade(owner);

            Assert.IsFalse(firstAttack.IsMet, "the shipped first-strike line survived the strike");
            Assert.IsFalse(undamaged.IsMet, "the shipped undamaged line survived the damage");
            Assert.IsFalse(unattacked.IsMet, "the shipped unattacked line survived the swing");
        }

        [TestMethod]
        public void TheEvasionNodeHoldsItsBonusUntilSomebodySwingsAtTheOwner()
        {
            // The node grants a flat pile of evasion "until getting an attack", which is a turn-scoped
            // predicate and nothing else: a line on the owner's health would give the bonus back on a heal
            // and take it away on a scratch nobody swung.
            ModifierLine line = EvasionNodeLine();
            var owner = new ConditionOwner();
            ICondition condition = Shipped(ShippedCatalog(), line.Condition, owner);

            Assert.IsFalse(condition.IsMet, "the node's bonus counted out of combat");

            StartTurn(owner);
            Assert.IsTrue(condition.IsMet, "the node's bonus was not there at the start of the owner's turn");

            Evade(owner);
            Assert.IsFalse(condition.IsMet, "the node kept its bonus through an attack on its owner");

            StartTurn(owner);
            Assert.IsTrue(condition.IsMet, "the node never gave the bonus back on the next turn");
        }

        private static float Strength(ConditionOwner owner) => owner.Parameters.GetValueForParameter(EntityParameter.Strength);

        private static void StartTurn(ConditionOwner owner) => owner.CombatEvents.Publish(new TurnStartEvent(owner));

        private static void Attack(ConditionOwner owner) => owner.CombatEvents.Publish(new AfterAttackEvent(Mock.Of<IAttackContext>()));

        private static void Cast(ConditionOwner owner) =>
            owner.CombatEvents.Publish(new AbilityExecutedEvent(Mock.Of<IAbility>(), owner, "cast"));

        private static void Evade(ConditionOwner owner) => owner.CombatEvents.Publish(new AttackEvadedEvent(Mock.Of<IAttackContext>()));

        private static void Block(ConditionOwner owner) => owner.CombatEvents.Publish(new AttackBlockedEvent(Mock.Of<IAttackContext>()));

        private static void Hurt(ConditionOwner owner, DamageCause cause) =>
            owner.CombatEvents.Publish(new DamageTakenEvent(new DamageContext { Source = owner, Cause = cause }, owner, default));

        /// <summary>The one event of the owner's that spends each action, so the coverage check drives every
        /// member of the enum through the signal its record promises.</summary>
        private static void Spend(TurnAction action, ConditionOwner owner)
        {
            switch (action)
            {
                case TurnAction.Attack:
                    Attack(owner);
                    break;
                case TurnAction.Ability:
                    Cast(owner);
                    break;
                case TurnAction.DamageTaken:
                    Hurt(owner, DamageCause.Effect);
                    break;
                case TurnAction.AttackReceived:
                    Evade(owner);
                    break;
                default:
                    Assert.Fail($"'{action}' has no signal in the fixture: the family counts it and nothing here spends it");
                    break;
            }
        }

        private static ICondition Attached(string id, ConditionOwner owner)
        {
            ICondition condition = Resolved(id);
            condition.Attach(owner);

            return condition;
        }

        private static ICondition Resolved(string id)
        {
            Assert.IsTrue(Catalog().TryResolve(id, out ICondition? condition), $"the fixture catalog refused '{id}'");

            return condition!;
        }

        private static ICondition Shipped(IConditionProvider catalog, string id, ConditionOwner owner)
        {
            Assert.IsTrue(catalog.TryResolve(id, out ICondition? condition), $"the shipped catalog holds no '{id}'");
            Assert.IsNotNull(condition, $"the shipped entry '{id}' produced no predicate");
            condition.Attach(owner);

            return condition;
        }

        private static IConditionProvider ShippedCatalog()
        {
            var catalog = new ConditionProvider(ConditionParser.Default());
            new GameDataService(new FileSystemDataSource(SharedData.Root()), [catalog]).LoadAll();

            return catalog;
        }

        /// <summary>The conditional evasion line of the shipped tree's Evasion node.</summary>
        private static ModifierLine EvasionNodeLine()
        {
            var tree = new PassiveTreeProvider();
            new GameDataService(new FileSystemDataSource(SharedData.Root()), [tree]).LoadAll();

            List<ModifierLine> gated = [.. tree.Tree.Nodes
                .SelectMany(node => node.Modifiers)
                .Where(line => line.Parameter == EntityParameter.Evade && line.IsConditional)];

            Assert.AreEqual(1, gated.Count, "the shipped tree holds no single conditional evasion line to read");

            return gated[0];
        }

        private static ConditionProvider Catalog() => ConditionCatalogs.Of(
            $$"""{ "id": "{{WhileFirstAttack}}", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.Attack)}}", "negate": true }""",
            $$"""{ "id": "{{AfterAnAttack}}", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.Attack)}}" }""",
            $$"""{ "id": "{{WhileFirstAbility}}", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.Ability)}}", "negate": true }""",
            $$"""{ "id": "{{WhileUndamaged}}", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.DamageTaken)}}", "negate": true }""",
            $$"""{ "id": "{{WhileUnattacked}}", "type": "{{ConditionTypes.TurnAction}}", "action": "{{nameof(TurnAction.AttackReceived)}}", "negate": true }""",
            $$"""{ "id": "{{WhileOpeningTurns}}", "type": "{{ConditionTypes.BattleTurn}}", "turn": {{Boundary}} }""",
            $$"""{ "id": "{{WhileDraggingOn}}", "type": "{{ConditionTypes.BattleTurn}}", "turn": {{Boundary}}, "negate": true }""");
    }
}
