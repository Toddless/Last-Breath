namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Battle.Source;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Moq;

    /// <summary>
    /// What a fighter is wired with while it lives and what has to come back off it when it is
    /// destroyed. Three wirings share this file because they share the four duplicated combat files:
    /// the exhaustion surcharge (attached to every combatant), the passive tree (attached to the
    /// player through a modifier source) and the roll stream the defensive rolls burn.
    /// The fighters themselves are Godot nodes and cannot be built outside the engine, so the
    /// contracts their wiring stands on are pinned here rather than the nodes.
    /// </summary>
    [TestClass]
    public class CombatantWiringTests
    {
        private static readonly ExhaustionRules Rules = new(CostIncreasePerStack: 0.25f, DecayPerTurn: 3);

        private const float NodeStrength = 2f;
        private const float BaseStrength = 10f;
        private const string Seed = "start";
        private const string Node = "small_1";

        [TestMethod]
        public void DetachTakesBackTheSurchargeAndEverySubscription()
        {
            Combatant fighter = Wired();

            fighter.Activate();
            Assert.AreEqual(125f, fighter.CostOf(100f), "the fixture never got its surcharge");

            ExhaustionGrant.Detach(fighter.Owner);

            Assert.AreEqual(100f, fighter.CostOf(100f), "the modifier stayed in the handler after the detach");
            Assert.AreEqual(0, fighter.Bus.HandlerCount, "the detach left a subscription on the combat bus");

            // A leaked subscription would keep stacking on a modifier nobody can reach any more.
            fighter.Activate();
            Assert.AreEqual(100f, fighter.CostOf(100f));
        }

        [TestMethod]
        public void AllThreeReactionsAreSubscribedAndAllThreeComeBack()
        {
            Combatant fighter = Wired();

            Assert.AreEqual(3, fighter.Bus.HandlerCount, "activation, own turn end and battle end");
            CollectionAssert.AreEquivalent(
                new[] { typeof(AbilityActivatedEvent), typeof(TurnEndEvent), typeof(BattleEndEvent) },
                fighter.Bus.SubscribedEvents);

            ExhaustionGrant.Detach(fighter.Owner);

            Assert.AreEqual(0, fighter.Bus.HandlerCount);
        }

        [TestMethod]
        public void ARepeatedAttachKeepsTheSingleWiringItAlreadyGave()
        {
            Combatant fighter = Wired();

            ExhaustionGrant.Attach(fighter.Owner, Rules);

            Assert.AreEqual(3, fighter.Bus.HandlerCount, "the second attach doubled the subscriptions");

            fighter.Activate();
            Assert.AreEqual(125f, fighter.CostOf(100f), "one activation must cost one stack, not two");
        }

        [TestMethod]
        public void DetachingACombatantThatWasNeverWiredIsANoOp()
        {
            Combatant fighter = new();

            ExhaustionGrant.Detach(fighter.Owner);

            Assert.AreEqual(100f, fighter.CostOf(100f));
            Assert.AreEqual(0, fighter.Bus.HandlerCount);
        }

        [TestMethod]
        public void AZeroedSurchargeWiresNothingAtAll()
        {
            Combatant fighter = new();

            ExhaustionGrant.Attach(fighter.Owner, new ExhaustionRules(CostIncreasePerStack: 0f, DecayPerTurn: 3));

            Assert.AreEqual(0, fighter.Bus.HandlerCount, "data that switches the rule off must not leave subscriptions behind");
        }

        [TestMethod]
        public void EveryChangeOfTheCountLeavesTheFighterAsAnEvent()
        {
            Combatant fighter = Wired();

            fighter.Activate();
            fighter.Activate();

            ExhaustionChangedEvent[] reported = fighter.Bus.Published<ExhaustionChangedEvent>();
            CollectionAssert.AreEqual(new[] { 1, 2 }, reported.Select(report => report.Stacks).ToArray(),
                "the count changed without leaving the fighter — the interface has nothing to draw");
            Assert.AreSame(fighter.Owner, reported[^1].Fighter, "the report has to name whose count it is");
        }

        [TestMethod]
        public void TheReportedSurchargeIsTheOneTheCastIsCharged()
        {
            Combatant fighter = Wired();

            fighter.Activate();
            fighter.Activate();
            fighter.Activate();

            float reported = fighter.Bus.Published<ExhaustionChangedEvent>()[^1].Surcharge;
            Assert.AreEqual(100f * (1f + reported), fighter.CostOf(100f), 0.0001f,
                "the number on screen has to be the number the caster pays");
        }

        [TestMethod]
        public void DecayAndBattleEndReportTheCountTheyLeaveBehind()
        {
            Combatant fighter = Wired();
            for (int i = 0; i < 5; i++) fighter.Activate();

            fighter.EndTurn();
            fighter.EndBattle();

            int[] reported = fighter.Bus.Published<ExhaustionChangedEvent>().Select(report => report.Stacks).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 2, 0 }, reported,
                "a count that fades away has to fade away on screen too");
        }

        [TestMethod]
        public void ACountThatDidNotMoveIsSilent()
        {
            Combatant fighter = Wired();

            fighter.EndTurn();
            fighter.EndBattle();

            Assert.AreEqual(0, fighter.Bus.Published<ExhaustionChangedEvent>().Length,
                "an unexhausted fighter reported a change nobody made");
        }

        [TestMethod]
        public void ADetachedCombatantStopsReportingItsCount()
        {
            Combatant fighter = Wired();
            fighter.Activate();

            ExhaustionGrant.Detach(fighter.Owner);
            fighter.Bus.ClearPublished();
            fighter.Activate();

            Assert.AreEqual(0, fighter.Bus.Published<ExhaustionChangedEvent>().Length,
                "a fighter taken off the field went on feeding the readout");
        }

        [TestMethod]
        public void MitigationDemandsAStream_SoNoHitRollsOnAnAnonymousGenerator()
        {
            ParameterInfo[] parameters = typeof(Calculations)
                .GetMethod(nameof(Calculations.CalculateMitigation))!
                .GetParameters();

            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual(typeof(IRandomNumberGenerator), parameters[2].ParameterType);
            Assert.IsFalse(parameters[2].IsOptional,
                "an optional stream is a hit resolved on a generator nobody named — the battle stops being reproducible");
            Assert.IsFalse(parameters[2].HasDefaultValue);
        }

        [TestMethod]
        public void TheSuppressionRollBurnsTheStreamTheCallerHandedIn()
        {
            var target = new Mock<IFightable>();
            var parameters = new EntityParametersComponent();
            parameters.SetBaseValueForParameter(EntityParameter.SuppressChance, 1f);
            parameters.SetBaseValueForParameter(EntityParameter.Suppress, 0.5f);
            target.Setup(t => t.Parameters).Returns(parameters);
            var context = new DamageContext { Source = target.Object, Cause = DamageCause.Ability };
            context.Add(DamageType.Sacred, 100f);
            var stream = new CountingRandom();

            Calculations.CalculateMitigation(context, target.Object, stream);

            Assert.AreEqual(1, stream.Draws, "the roll came from somewhere other than the stream the fighter owns");
            Assert.AreEqual(50f, context.DamageComponents[DamageType.Sacred], 0.001f);
        }

        [TestMethod]
        public void UnregisteringTheTreeSourceReturnsTheParameterToItsBase()
        {
            // The mirror of the player's destruction step: the tree contributes through a registered
            // source, so taking the source off has to take the whole contribution with it — and leave
            // the allocation itself alone, because the character did not sell anything back.
            var fighter = new Fighter();
            var service = new PassiveTreeService(new TreeProviderStub(SyntheticTree()), ConditionCatalogs.Empty());
            service.SetTotalPoints(1);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);

            Assert.AreEqual(AllocationResult.Success, service.Take(Node));
            Assert.AreEqual(BaseStrength + NodeStrength, fighter[EntityParameter.Strength], 0f);

            fighter.Modifiers.UnregisterSource(service.ParameterSource);

            Assert.AreEqual(BaseStrength, fighter[EntityParameter.Strength], 0f, "the destroyed fighter kept part of the tree");
            Assert.IsTrue(service.IsTaken(Node), "unregistering a source must not refund nodes");
        }

        [TestMethod]
        public void TheTreeSourceIsIdempotent_ASecondRegistrationDoesNotDoubleTheNode()
        {
            var fighter = new Fighter();
            var service = new PassiveTreeService(new TreeProviderStub(SyntheticTree()), ConditionCatalogs.Empty());
            service.SetTotalPoints(1);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);

            fighter.Modifiers.RegisterSource(service.ParameterSource);
            fighter.Modifiers.RegisterSource(service.ParameterSource);
            service.Take(Node);

            Assert.AreEqual(BaseStrength + NodeStrength, fighter[EntityParameter.Strength], 0f,
                "the source counted twice — registration must happen once per fighter lifetime");
        }

        private static Combatant Wired()
        {
            Combatant fighter = new();
            ExhaustionGrant.Attach(fighter.Owner, Rules);
            return fighter;
        }

        /// <summary>A seed with one Small node hanging off it, carrying flat Strength.</summary>
        private static PassiveTreeDocument SyntheticTree()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Strength, ValueType = ModifierValueType.Flat, Value = NodeStrength
            });
            document.AddNode(node);
            document.Link(Seed, Node);
            return document;
        }

        /// <summary>The combatant as the grant sees it: a real modifier handler and a bus that can be
        /// asked what is still listening.</summary>
        private sealed class Combatant
        {
            private readonly ModifierHandlerComponent _handler = new();

            public RecordingBus Bus { get; } = new();

            public IFightable Owner { get; }

            public Combatant()
            {
                var owner = new Mock<IFightable>();
                owner.Setup(o => o.ModifierHandler).Returns(_handler);
                owner.Setup(o => o.CombatEvents).Returns(Bus);
                Owner = owner.Object;
            }

            public void Activate() =>
                Bus.Publish(new AbilityActivatedEvent(Mock.Of<IAbility>(), Owner, default, "cast"));

            public void EndTurn() => Bus.Publish(new TurnEndEvent());

            public void EndBattle() => Bus.Publish(new BattleEndEvent(BattleResults.PlayerWon));

            public float CostOf(float cost)
            {
                IAbilityActivationContext context = new AbilityActivationContext
                {
                    Ability = Mock.Of<IAbility>(),
                    Caster = Owner,
                    Field = Mock.Of<IBattleField>(),
                    Rnd = null!,
                    Cost = cost,
                    CostType = Costs.Mana
                };
                _handler.Apply(context);
                return context.Cost;
            }
        }

        /// <summary>Dispatches like the real bus and, unlike it, answers what is still subscribed —
        /// a lambda subscription is invisible from the outside otherwise — and what has been
        /// published on it, which is what the timeline records and the replay later shows.</summary>
        private sealed class RecordingBus : ICombatEventBus
        {
            private readonly List<(Type Event, object Handler)> _handlers = [];
            private readonly List<ICombatEvent> _published = [];

            public int HandlerCount => _handlers.Count;

            public Type[] SubscribedEvents => _handlers.Select(entry => entry.Event).ToArray();

            public T[] Published<T>() where T : ICombatEvent => _published.OfType<T>().ToArray();

            public void ClearPublished() => _published.Clear();

            public void Publish<T>(T evnt) where T : ICombatEvent
            {
                _published.Add(evnt);
                foreach ((Type type, object handler) in _handlers.ToArray())
                    if (type == typeof(T)) ((Action<T>)handler).Invoke(evnt);
            }

            public void Subscribe<T>(Action<T> handler) where T : ICombatEvent => _handlers.Add((typeof(T), handler));

            public void Unsubscribe<T>(Action<T> handler) where T : ICombatEvent => _handlers.Remove((typeof(T), handler));

            public void SubscribeAll(Action<ICombatEvent> handler) => _handlers.Add((typeof(ICombatEvent), handler));

            public void UnsubscribeAll(Action<ICombatEvent> handler) => _handlers.Remove((typeof(ICombatEvent), handler));

            public void Dispose() => _handlers.Clear();
        }

        /// <summary>Always rolls a success and counts how often it was asked.</summary>
        private sealed class CountingRandom : IRandomNumberGenerator
        {
            public int Draws { get; private set; }

            public float RandFloat()
            {
                Draws++;
                return 0f;
            }

            public float RandFloatRange(float min, float max) => min;

            public int RandIntRange(int min, int max) => min;

            public float RandFloatN(float mean, float deviation) => mean;

            public uint RandInt() => 0;

            public long RandWeighted(float[] weights) => 0;

            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }

        /// <summary>The modifier/parameter pair wired the way a fighter wires it.</summary>
        private sealed class Fighter
        {
            public ParameterModifiersComponent Modifiers { get; } = new();

            public EntityParametersComponent Parameters { get; } = new();

            public Fighter()
            {
                Parameters.Initialize(Modifiers.GetModifiers);
                Modifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
            }

            public float this[EntityParameter parameter] => Parameters.GetValueForParameter(parameter);
        }

        /// <summary>Stands in for the data pipeline: the document the catalog would have loaded.</summary>
        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree => tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
