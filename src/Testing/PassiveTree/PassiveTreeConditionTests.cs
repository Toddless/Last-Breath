namespace LastBreathTest.PassiveTree
{
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Session;

    /// <summary>
    /// A node line that only applies while something is true of its carrier. Both channels of the tree
    /// answer the same catalog and both have to let the line go the moment it stops holding — one by
    /// skipping a modifier the fighter pulls, the other by leaving it out of a knob's total — and both
    /// have to drop a line whose condition nobody wrote rather than hand it over unconditionally.
    /// </summary>
    [TestClass]
    public class PassiveTreeConditionTests
    {
        private const string Seed = "start";
        private const string Node = "small_1";
        private const float MaxHealth = 100f;
        private const float BaseStrength = 10f;
        private const float Grant = 6f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void AConditionalLine_CountsOnlyWhileItsConditionHolds()
        {
            ConditionOwner fighter = Wired(out IPassiveTreeService service, Conditional(ConditionCatalogs.WhileWounded));

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance, "the line counted on a fighter its condition says nothing about");

            Wound(fighter);
            Assert.AreEqual(BaseStrength + Grant, Strength(fighter), Tolerance, "the condition came true and the parameter was never resolved again");

            Heal(fighter);
            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance, "the condition let go and the bonus stayed on the character");

            Assert.IsTrue(service.IsTaken(Node), "reading a condition must not touch the allocation");
        }

        [TestMethod]
        public void AConditionalLine_CountsFromTheMomentTheFighterIsKnown()
        {
            // The source is registered before the fighter is handed over — that is the order a player is
            // built in. Until the hand-over a predicate has nobody to read, so the value resolved in
            // between is the one without the line; the arrival has to be announced or it stays that way.
            IPassiveTreeService service = Service(Conditional(ConditionCatalogs.WhileWounded), ConditionCatalogs.Wounded());
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            Wound(fighter);

            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);
            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance, "a predicate answered before it had a fighter to read");

            service.ContextSource.Attach(fighter);

            Assert.AreEqual(BaseStrength + Grant, Strength(fighter), Tolerance,
                "the line was armed on a wounded fighter and nothing asked for the parameter again");
        }

        [TestMethod]
        public void ALineNamingAConditionNobodyWrote_IsDroppedInsteadOfCountingAlways()
        {
            ConditionOwner fighter = Wired(out _, Conditional(ConditionCatalogs.Unwritten));

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance);

            Wound(fighter);

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance,
                "a line whose condition is in no catalog was handed over anyway — the refusal is the catalog's and it costs the line");
        }

        [TestMethod]
        public void ALineWithoutACondition_NeedsNoCatalogBehindIt()
        {
            ConditionOwner fighter = Wired(out _, Line(condition: string.Empty), ConditionCatalogs.Empty());

            Assert.AreEqual(BaseStrength + Grant, Strength(fighter), Tolerance,
                "an unconditional line was made to answer a catalog that holds nothing");
        }

        [TestMethod]
        public void ARespec_TakesTheConditionalLineAndItsWatchOffTheFighter()
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            Heal(fighter);
            int alone = fighter.ListenerCount;

            IPassiveTreeService service = Service(Conditional(ConditionCatalogs.WhileWounded), ConditionCatalogs.Wounded());
            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            service.ContextSource.Attach(fighter);
            Wound(fighter);

            Assert.AreEqual(BaseStrength + Grant, Strength(fighter), Tolerance, "the fixture never got the bonus on");
            Assert.IsTrue(fighter.ListenerCount > alone, "the predicate never subscribed to the fighter it watches");

            service.Respec();

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance, "the refunded line kept counting while its condition held");
            Assert.AreEqual(alone, fighter.ListenerCount, "the refunded line is still watching the fighter");

            // The allocation goes on changing after the tree let go; nothing of it may find its way back.
            ((ISessionResettable)service).ResetSession();

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance);
            Assert.AreEqual(alone, fighter.ListenerCount);
        }

        [TestMethod]
        public void MovingToASecondFighter_TakesTheConditionalLineWithIt()
        {
            // World and arena build their own player objects out of one tree service.
            IPassiveTreeService service = Service(Conditional(ConditionCatalogs.WhileWounded), ConditionCatalogs.Wounded());
            ConditionOwner first = Carrier(service);
            ConditionOwner second = Carrier(service);
            int alone = first.ListenerCount;

            service.ContextSource.Attach(first);
            service.ContextSource.Attach(second);
            Wound(first);
            Wound(second);

            Assert.AreEqual(BaseStrength, Strength(first), Tolerance, "the replaced fighter kept the tree's conditional line");
            Assert.AreEqual(alone, first.ListenerCount, "the replaced fighter is still being watched");
            Assert.AreEqual(BaseStrength + Grant, Strength(second), Tolerance);
        }

        [TestMethod]
        public void TakingTheContributionOffAFighter_TakesTheWatchWithIt()
        {
            // The destruction step of a player that has no successor. The predicate outlives the fighter
            // — the tree is a singleton and the fighter is a scene node — so what is left listening here
            // is what keeps a dead object alive and answering.
            IPassiveTreeService service = Service(Conditional(ConditionCatalogs.WhileWounded), ConditionCatalogs.Wounded());
            ConditionOwner fighter = Carrier(service);
            int alone = fighter.ListenerCount;

            service.ContextSource.Attach(fighter);
            Wound(fighter);
            Assert.AreEqual(BaseStrength + Grant, Strength(fighter), Tolerance, "the fixture never got the bonus on");

            service.ContextSource.Detach(fighter);

            Assert.AreEqual(BaseStrength, Strength(fighter), Tolerance, "the fighter the tree let go kept counting its conditional line");
            Assert.AreEqual(alone, fighter.ListenerCount, "the tree is still watching a fighter it let go");
        }

        [TestMethod]
        public void AConditionalContextLine_LeavesTheKnobsTotalWhileItsConditionDoesNot()
        {
            ConditionOwner fighter = Carrier(out IPassiveTreeService service, Context(ConditionCatalogs.WhileWounded));
            service.ContextSource.Attach(fighter);

            Assert.AreEqual(0f, service.ContextSource.ValueOf(ContextParameter.HealingEfficiency), Tolerance);
            Assert.AreEqual(100f, Healed(fighter), Tolerance, "the line counted before its condition did");

            Wound(fighter);

            Assert.AreEqual(150f, Healed(fighter), Tolerance,
                "the total is read when a pipeline asks for it, so the line had to be in it the moment its condition was");

            Heal(fighter);

            Assert.AreEqual(100f, Healed(fighter), Tolerance, "the line stayed in the total after its condition let go");
        }

        [TestMethod]
        public void AContextLineNamingAConditionNobodyWrote_IsDroppedWithTheKnobItFed()
        {
            ConditionOwner fighter = Carrier(out IPassiveTreeService service, Context(ConditionCatalogs.Unwritten));
            service.ContextSource.Attach(fighter);
            Wound(fighter);

            Assert.AreEqual(0, service.ContextSource.Knobs.Count, "a knob was bought for a line the catalog refused");
            Assert.AreEqual(100f, Healed(fighter), Tolerance);
        }

        [TestMethod]
        public void ASwitchHeldUpByACondition_IsOffUntilTheConditionHolds()
        {
            // The one knob whose binding reads no number: nothing about the switch reaches the pipeline
            // as an amount, so a lazy total gates it only if the switch itself is read when the hit is.
            ConditionOwner fighter = Carrier(out IPassiveTreeService service, new ContextModifierLine
            {
                Parameter = ContextParameter.AttacksIgnoreResistances,
                ValueType = ModifierValueType.Flag,
                Condition = ConditionCatalogs.WhileWounded
            });
            service.ContextSource.Attach(fighter);

            Assert.IsFalse(Ignores(fighter), "the switch was on while the condition holding it up was not");

            Wound(fighter);
            Assert.IsTrue(Ignores(fighter));

            Heal(fighter);
            Assert.IsFalse(Ignores(fighter), "the switch stayed on after its condition let go");
        }

        private static float Strength(ConditionOwner fighter) => fighter.Parameters.GetValueForParameter(EntityParameter.Strength);

        private static void Wound(ConditionOwner fighter) => fighter.CurrentHealth = MaxHealth * (ConditionCatalogs.WoundedShare - 0.1f);

        private static void Heal(ConditionOwner fighter) => fighter.CurrentHealth = MaxHealth;

        private static float Healed(IFightable fighter)
        {
            var context = new HealContext(fighter, fighter) { Amount = 100f };
            fighter.ModifierHandler.Apply(context);

            return context.Amount;
        }

        private static bool Ignores(IFightable fighter)
        {
            var context = new DamageContext { Source = fighter, Cause = DamageCause.Attack };
            fighter.ModifierHandler.Apply(context);

            return context.IgnoreResistances;
        }

        private static ModifierLine Conditional(string condition) => Line(condition);

        private static ModifierLine Line(string condition) => new()
        {
            Parameter = EntityParameter.Strength,
            ValueType = ModifierValueType.Flat,
            Value = Grant,
            Condition = condition
        };

        private static ContextModifierLine Context(string condition) => new()
        {
            Parameter = ContextParameter.HealingEfficiency,
            ValueType = ModifierValueType.Increase,
            Value = 0.5f,
            Condition = condition
        };

        /// <summary>A fighter carrying the tree's parametric channel, with the line already answerable.</summary>
        private static ConditionOwner Wired(out IPassiveTreeService service, ModifierLine line, ConditionProvider? catalog = null)
        {
            service = Service(line, catalog ?? ConditionCatalogs.Wounded());
            ConditionOwner fighter = Carrier(service);
            service.ContextSource.Attach(fighter);

            return fighter;
        }

        private static ConditionOwner Carrier(out IPassiveTreeService service, ContextModifierLine line)
        {
            service = Service(line, ConditionCatalogs.Wounded());
            return Carrier(service);
        }

        private static ConditionOwner Carrier(IPassiveTreeService service)
        {
            var fighter = new ConditionOwner();
            fighter.SetMaximum(EntityParameter.Health, MaxHealth);
            Heal(fighter);
            fighter.Parameters.SetBaseValueForParameter(EntityParameter.Strength, BaseStrength);
            fighter.ParameterModifiers.RegisterSource(service.ParameterSource);

            return fighter;
        }

        private static IPassiveTreeService Service(ModifierLine line, ConditionProvider catalog) =>
            Service(Tree(node => node.Modifiers.Add(line)), catalog);

        private static IPassiveTreeService Service(ContextModifierLine line, ConditionProvider catalog) =>
            Service(Tree(node => node.ContextModifiers.Add(line)), catalog);

        private static IPassiveTreeService Service(PassiveTreeDocument document, ConditionProvider catalog)
        {
            var service = new PassiveTreeService(new TreeProviderStub(document), catalog);
            service.SetTotalPoints(1);
            Assert.AreEqual(AllocationResult.Success, service.Take(Node), "the fixture never bought its node");

            return service;
        }

        /// <summary>A seed with one bought node hanging off it — the smallest tree a line can live on.</summary>
        private static PassiveTreeDocument Tree(Action<PassiveNode> fill)
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });

            var node = new PassiveNode { Id = Node, Kind = PassiveNodeKind.Small, Stance = Stance.Strength };
            fill(node);
            document.AddNode(node);
            document.Link(Seed, Node);

            return document;
        }

        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
