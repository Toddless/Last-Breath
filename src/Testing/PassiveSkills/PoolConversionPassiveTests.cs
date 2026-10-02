namespace LastBreathTest.PassiveSkills
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Entity.Components.Decorator;
    using Core.Enums;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;

    /// <summary>
    /// The keystones that trade one pool for another: the whole of a parameter is added to a second one and
    /// the first is emptied. Three of them share a class and differ only in the pair they name, so what is
    /// pinned here is the conversion itself — what it measures, when it re-measures, what it leaves behind,
    /// and what two of them do to one another.
    /// <para>The measure is the load-bearing part: a conversion counts the pool as it stands UNCONVERTED,
    /// which is what keeps it from closing on itself (its own drain would leave it nothing to convert) and
    /// what makes a mutual pair settle instead of feeding each other without end.</para>
    /// </summary>
    [TestClass]
    public class PoolConversionPassiveTests
    {
        private const string IronWillId = "Passive_Skill_Iron_Will";
        private const string WindOfFreedomId = "Passive_Skill_Wind_Of_Freedom";

        private const string Seed = "start_dex";
        private const string IronWillNode = "keystone_iron_will";

        /// <summary>A conversion of a CAPPED pool, which none of the shipped three is — the class takes any
        /// pair, so the cap is shown on a pair invented here rather than by re-tuning a keystone.</summary>
        private const string FixtureConversionId = "Passive_Skill_Fixture_Block_Conversion";

        /// <summary>Block chance is bounded at 0.9 by the parameters component.</summary>
        private const float BlockChanceCap = 0.9f;

        private const float EvadeBase = 500f;
        private const float ArmorBase = 100f;
        private const float ManaBase = 1000f;
        private const float HealthBase = 2000f;
        private const float CriticalDamageBase = 2f;
        private const float ColdDamageBase = 100f;

        private const float AgnosticCostScale = 0.25f;
        private const float Precision = 0.01f;

        // ---- what the conversion is ------------------------------------------------------------------

        [TestMethod]
        public void TheReceivingPoolGainsTheWholeOfTheOneGivenUp()
        {
            var carrier = new Carrier();

            carrier.Wear(IronWill());

            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision,
                "the conversion measured a pool it had already emptied — it counted its own lines");
        }

        [TestMethod]
        public void TheConvertedPoolIsEmptied()
        {
            var carrier = new Carrier();

            carrier.Wear(IronWill());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision,
                "the pool was given away and kept at the same time");
        }

        /// <summary>The pool is emptied CATEGORICALLY, not by a multiplier of its own: gear that multiplies
        /// evasion (Creator's Trace boots, the mythic armor-and-evasion affix) shares the one multiplicative
        /// bucket, so an emptying written as −1 would only halve a pool standing at +50% — given away and
        /// half alive at the same time. The gift is measured WITH that multiplier: 500 at +50% is 750.</summary>
        [TestMethod]
        public void TheConvertedPoolIsEmptiedUnderAForeignMultiplier()
        {
            var carrier = new Carrier();
            new SimpleModifier(EntityParameter.Evade, ModifierValueType.Multiplicative, 0.5f, "boots").ApplyTo(carrier.Player);

            carrier.Wear(IronWill());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision,
                "a multiplied pool survived being converted");
            Assert.AreEqual(ArmorBase + (EvadeBase * 1.5f), carrier.Value(EntityParameter.Armor), Precision,
                "the gift was not measured at what the pool was actually worth");
        }

        /// <summary>The same fact where it costs most: a multiplied pool on BOTH sides of a mutual pair
        /// still leaves two empty pools rather than two half-alive ones.</summary>
        [TestMethod]
        public void TheMutualPairEmptiesBothPoolsUnderForeignMultipliers()
        {
            var carrier = new Carrier();
            new SimpleModifier(EntityParameter.Evade, ModifierValueType.Multiplicative, 0.5f, "boots").ApplyTo(carrier.Player);
            new SimpleModifier(EntityParameter.Armor, ModifierValueType.Multiplicative, 0.5f, "plate").ApplyTo(carrier.Player);

            carrier.Wear(IronWill());
            carrier.Wear(WindOfFreedom());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Armor), Precision);
        }

        /// <summary>The verdict is taken past the decorators too: a decorator adding to the pool sits above
        /// the whole formula, so an emptying written anywhere inside the numbers would let it hand the pool
        /// back after the conversion had already given it away.</summary>
        [TestMethod]
        public void ADecoratorCannotHandBackAPoolThatWasGivenAway()
        {
            var carrier = new Carrier();
            carrier.Parameters.AddModuleDecorator(
                new EntityParameterDecorator("fixture_evade_boost", 300f, OperationType.Add, EntityParameter.Evade, Priority.Strong));

            carrier.Wear(IronWill());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision,
                "a decorator put the converted pool back on top of the formula");
        }

        /// <summary>A pool with a ceiling is taken over at its ceiling: the measure is the parameter as its
        /// readers see it, bounds and all, so a conversion cannot smuggle a capped stat past its cap by
        /// pouring it somewhere uncapped.</summary>
        [TestMethod]
        public void TheMeasureRespectsTheCapOfThePoolItTakesOver()
        {
            var carrier = new Carrier();
            carrier.Parameters.SetBaseValueForParameter(EntityParameter.BlockChance, 2f);

            carrier.Wear(new PoolConversionPassiveSkill(FixtureConversionId, EntityParameter.BlockChance, EntityParameter.Armor));

            Assert.AreEqual(ArmorBase + BlockChanceCap, carrier.Value(EntityParameter.Armor), Precision,
                "the conversion handed over more of the pool than the pool is ever allowed to be worth");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.BlockChance), Precision);
        }

        [TestMethod]
        public void TakingTheKeystoneOffLeavesBothPoolsAsTheyWere()
        {
            var carrier = new Carrier();
            ISkill keystone = IronWill();

            carrier.Wear(keystone);
            carrier.TakeOff(keystone);

            Assert.AreEqual(EvadeBase, carrier.Value(EntityParameter.Evade), Precision, "the emptied pool never came back");
            Assert.AreEqual(ArmorBase, carrier.Value(EntityParameter.Armor), Precision, "the gift outlived the keystone");
        }

        /// <summary>The gift is re-measured for as long as the keystone is worn: gear that carried the pool
        /// carries the gift with it, in and out.</summary>
        [TestMethod]
        public void TheGiftFollowsThePoolForAsLongAsTheKeystoneIsWorn()
        {
            var carrier = new Carrier();
            carrier.Wear(IronWill());
            var ring = new SimpleModifier(EntityParameter.Evade, ModifierValueType.Flat, 200f, "ring");

            ring.ApplyTo(carrier.Player);
            Assert.AreEqual(ArmorBase + EvadeBase + 200f, carrier.Value(EntityParameter.Armor), Precision,
                "evasion put on after the keystone was never converted");

            ring.RemoveFrom(carrier.Player);
            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision,
                "the gift kept a pool the character no longer has");
        }

        /// <summary>The gift lands flat, so the receiving pool's own increases scale it like any other line
        /// — armor bought with evasion answers to armor's increases. Deliberate.</summary>
        [TestMethod]
        public void TheGiftAnswersToTheReceivingPoolsIncreases()
        {
            var carrier = new Carrier();
            new SimpleModifier(EntityParameter.Armor, ModifierValueType.Increase, 0.5f, "plate").ApplyTo(carrier.Player);

            carrier.Wear(IronWill());

            Assert.AreEqual((ArmorBase + EvadeBase) * 1.5f, carrier.Value(EntityParameter.Armor), Precision,
                "the converted pool stopped answering to the increases of the pool it joined");
        }

        /// <summary>Everything else reading the emptied pool reads a zero — a pool that was converted is
        /// gone. Critical damage counted per point of evasion (Dance of Death) is worth nothing beside a
        /// conversion of evasion.</summary>
        [TestMethod]
        public void AnythingCountedPerPointOfTheEmptiedPoolCountsNothing()
        {
            var carrier = new Carrier();
            PerPoint(EntityParameter.CriticalDamage, EntityParameter.Evade, 0.02f / 100f).ApplyTo(carrier.Player);
            Assert.AreEqual(CriticalDamageBase * 1.1f, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "the fixture no longer carries a line measured off the pool");

            carrier.Wear(IronWill());

            Assert.AreEqual(CriticalDamageBase, carrier.Value(EntityParameter.CriticalDamage), Precision,
                "a line paid per point of an emptied pool kept being paid");
        }

        // ---- two conversions on one fighter ----------------------------------------------------------

        /// <summary>Both keystones at once is a legal allocation and a losing one: each pours its own pool
        /// into the other's and empties what it gave, so the gift always lands on a parameter the other
        /// keystone multiplies away. A full swap is not reachable — the drain is a multiplier and cannot
        /// spare another conversion's flat gift.</summary>
        [TestMethod]
        public void TwoConversionsPointingAtOneAnotherEmptyBothPools()
        {
            var carrier = new Carrier();

            carrier.Wear(IronWill());
            carrier.Wear(WindOfFreedom());

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Armor), Precision);
        }

        /// <summary>A conversion takes over the pool's OWN sources and nothing another conversion poured
        /// into it, so conversions do not chain: evasion made into armor does not travel on into health
        /// behind a second conversion of armor. Two conversions pointing at one another would otherwise
        /// each grow on what the other handed over, round after round, and the answer would be however
        /// deep the chain of announcements happened to go.</summary>
        [TestMethod]
        public void AConversionTakesOverAPoolWithoutWhatAnotherConversionPouredIntoIt()
        {
            var carrier = new Carrier();

            carrier.Wear(IronWill());
            carrier.Wear(new PoolConversionPassiveSkill(FixtureConversionId, EntityParameter.Armor, EntityParameter.Health));

            Assert.AreEqual(HealthBase + ArmorBase, carrier.Value(EntityParameter.Health), Precision,
                "the second conversion carried the first pool through as well — conversions chained");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Armor), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
        }

        /// <summary>The same rule read straight off the measure, for the case the parameters cannot show:
        /// with both pools of a mutual pair emptied categorically, an escalating gift is invisible in what
        /// anyone reads, so the guard has to ask the measure itself.</summary>
        [TestMethod]
        public void TheUnconvertedMeasureIgnoresWhatAnotherConversionGave()
        {
            var carrier = new Carrier();

            carrier.Wear(WindOfFreedom());

            Assert.AreEqual(EvadeBase, carrier.Parameters.GetUnconvertedValueForParameter(EntityParameter.Evade), Precision,
                "a conversion of evasion would have counted the armor another conversion poured into it");
        }

        /// <summary>Neither conversion counts the other's gift, so neither can grow on it: the pair has one
        /// settled answer and the order the keystones were taken in cannot change it.</summary>
        [TestMethod]
        public void TheMutualPairSettlesTheSameWhicheverKeystoneIsTakenFirst()
        {
            var first = new Carrier();
            first.Wear(IronWill());
            first.Wear(WindOfFreedom());

            var second = new Carrier();
            second.Wear(WindOfFreedom());
            second.Wear(IronWill());

            Assert.AreEqual(first.Value(EntityParameter.Evade), second.Value(EntityParameter.Evade), Precision,
                "the pair answers to the order it was taken in");
            Assert.AreEqual(first.Value(EntityParameter.Armor), second.Value(EntityParameter.Armor), Precision,
                "the pair answers to the order it was taken in");
        }

        /// <summary>Taking one of the pair back hands the other a whole pool again: nothing of the first
        /// conversion is left in the second's measure.</summary>
        [TestMethod]
        public void TakingOneOfThePairOffLeavesTheOtherConvertingAlone()
        {
            var carrier = new Carrier();
            ISkill windOfFreedom = WindOfFreedom();
            carrier.Wear(IronWill());
            carrier.Wear(windOfFreedom);

            carrier.TakeOff(windOfFreedom);

            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision,
                "the surviving conversion did not go back to converting a whole pool");
        }

        /// <summary>A gift that feeds back into its own pool through a third line settles one level deep
        /// instead of running the loop on the stack — the same latch a scaled line holds. Mana counted per
        /// point of health, converted into health: the runaway fixed point is 6000 and the settled answer is
        /// short of it on purpose. The figure pinned here is the settle depth, not a balance number.</summary>
        [TestMethod]
        public void AGiftThatFeedsItsOwnPoolSettlesInsteadOfRunningTheLoop()
        {
            var carrier = new Carrier();
            PerPoint(EntityParameter.Mana, EntityParameter.Health, 0.5f, ModifierValueType.Flat).ApplyTo(carrier.Player);

            carrier.Wear(Agnostic());

            Assert.AreEqual(5500f, carrier.Value(EntityParameter.Health), Precision,
                "the conversion either went round the loop to its fixed point or stopped short of settling");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Mana), Precision);
        }

        // ---- the agnostic --------------------------------------------------------------------------

        [TestMethod]
        public void TheAgnosticPoursManaIntoHealth()
        {
            var carrier = new Carrier();

            carrier.Wear(Agnostic());

            Assert.AreEqual(HealthBase + ManaBase, carrier.Value(EntityParameter.Health), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Mana), Precision);
        }

        [TestMethod]
        public void TheAgnosticPaysWithHealthAtASurcharge()
        {
            var carrier = new Carrier();

            carrier.Wear(Agnostic());
            IAbilityActivationContext cast = carrier.Cast(cost: 100f);

            Assert.AreEqual(Costs.Health, cast.CostType, "a caster with no mana was still billed for mana");
            Assert.AreEqual(100f * (1 + AgnosticCostScale), cast.Cost, Precision);
        }

        [TestMethod]
        public void TakingTheAgnosticOffGivesTheManaBackAndStopsTheSurcharge()
        {
            var carrier = new Carrier();
            ISkill agnostic = Agnostic();
            carrier.Wear(agnostic);

            carrier.TakeOff(agnostic);
            IAbilityActivationContext cast = carrier.Cast(cost: 100f);

            Assert.AreEqual(ManaBase, carrier.Value(EntityParameter.Mana), Precision);
            Assert.AreEqual(HealthBase, carrier.Value(EntityParameter.Health), Precision);
            Assert.AreEqual(Costs.Mana, cast.CostType, "the health price outlived the keystone");
            Assert.AreEqual(100f, cast.Cost, Precision);
        }

        /// <summary>The same fact as Dance of Death, on the other keystone: cold damage counted per fifteen
        /// mana (Cold Reason) is worth nothing once the mana pool has become health.</summary>
        [TestMethod]
        public void TheAgnosticLeavesLinesCountedPerManaAtNothing()
        {
            var carrier = new Carrier();
            PerPoint(EntityParameter.ColdDamage, EntityParameter.Mana, 0.02f / 15f).ApplyTo(carrier.Player);
            float withMana = carrier.Value(EntityParameter.ColdDamage);

            carrier.Wear(Agnostic());

            Assert.IsTrue(withMana > ColdDamageBase, "the fixture no longer carries a line measured off mana");
            Assert.AreEqual(ColdDamageBase, carrier.Value(EntityParameter.ColdDamage), Precision,
                "a line paid per point of an emptied pool kept being paid");
        }

        // ---- the road the keystone actually arrives by -----------------------------------------------

        [TestMethod]
        public void ANodeGrantsTheConversionAndTheGiftLands()
        {
            var carrier = new Carrier();
            IPassiveTreeService tree = NewTree();
            CreateService(carrier, tree);

            Assert.AreEqual(AllocationResult.Success, tree.Take(IronWillNode));

            Assert.IsNotNull(carrier.Skills.GetSkill(IronWillId), "the node named a conversion the character never received");
            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision);
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
        }

        /// <summary>Nothing of the grant is saved — the allocation is, and a load reconciles by the same
        /// road as a click. The conversion must run again on the restored character and must not run twice.</summary>
        [TestMethod]
        public void AReloadedAllocationConvertsOnceAndNotTwice()
        {
            IPassiveTreeService sourceTree = NewTree();
            CreateService(new Carrier(), sourceTree);
            sourceTree.Take(IronWillNode);
            SaveFile file = ManagerFor(sourceTree).Capture(new SaveMetadata());

            var carrier = new Carrier();
            IPassiveTreeService targetTree = NewTree();
            PassiveGrantService service = CreateService(carrier, targetTree);
            ManagerFor(targetTree).Restore(file);
            service.Reconcile();

            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision,
                "the restored allocation converted the pool a second time");
            Assert.AreEqual(0f, carrier.Value(EntityParameter.Evade), Precision);
        }

        /// <summary>Two sources naming one conversion are two registrations of one id, and only one of them
        /// is ever worn — an item and a node cannot convert the same pool twice over.</summary>
        [TestMethod]
        public void TwoRegistrationsOfOneConversionConvertOnce()
        {
            var carrier = new Carrier();

            carrier.Wear(IronWill());
            carrier.Wear(IronWill());

            Assert.AreEqual(ArmorBase + EvadeBase, carrier.Value(EntityParameter.Armor), Precision);
        }

        // ---- fixtures --------------------------------------------------------------------------------

        private static ISkill IronWill() => Build(IronWillId);

        private static ISkill WindOfFreedom() => Build(WindOfFreedomId);

        private static ISkill Agnostic() =>
            Build(AgnosticPassiveSkill.PassiveId, new Dictionary<string, float> { ["costScale"] = AgnosticCostScale });

        private static ISkill Build(string id, Dictionary<string, float>? properties = null)
        {
            ISkill? skill = new PassiveSkillProvider().CreateSkill(id, new RecordProperties(id, properties ?? []));

            Assert.IsNotNull(skill, $"the registry does not build '{id}'");
            return skill;
        }

        /// <summary>A line worth <paramref name="value"/> for every unit of a carrier — the shape a keystone
        /// counted per point of another parameter takes.</summary>
        private static IModifierInstance PerPoint(
            EntityParameter parameter,
            EntityParameter perParameter,
            float value,
            ModifierValueType valueType = ModifierValueType.Increase) =>
            new ScaledByParameterModifier(valueType, parameter, perParameter, value, condition: null, source: "fixture");

        private static PassiveTreeDocument Document()
        {
            var document = new PassiveTreeDocument();
            document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Dexterity });
            document.AddNode(new PassiveNode
            {
                Id = IronWillNode, Kind = PassiveNodeKind.Keystone, Stance = Stance.Dexterity, PassiveId = IronWillId
            });
            document.Link(Seed, IronWillNode);

            return document;
        }

        private static PassiveTreeService NewTree()
        {
            PassiveTreeDocument document = Document();
            var service = new PassiveTreeService(new TreeStub(document), ConditionCatalogs.Empty());
            service.SetTotalPoints(document.Nodes.Count);
            return service;
        }

        private static ISaveManager ManagerFor(IPassiveTreeService tree)
        {
            var manager = new SaveManager(new LoadScope());
            manager.Register(new PassiveTreeSaveParticipant(tree));
            return manager;
        }

        private static PassiveGrantService CreateService(Carrier carrier, IPassiveTreeService tree)
        {
            var accessor = new PlayerAccessor();
            accessor.Set(carrier.Player);
            return new PassiveGrantService(accessor, new PassiveSkillProvider(), tree);
        }

        /// <summary>A player with the parts a conversion reaches: the roster it registers on, the modifier
        /// list its two halves land in, the parameters they resolve through and the cast pipeline the
        /// agnostic's price rides.</summary>
        private sealed class Carrier
        {
            public Carrier()
            {
                var mock = new Mock<IPlayer>();
                Player = mock.Object;
                Skills = new PassiveSkillsComponent(Player);
                Parameters.Initialize(Modifiers.GetModifiers);
                Modifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
                Parameters.SetBaseValueForParameter(EntityParameter.Evade, EvadeBase);
                Parameters.SetBaseValueForParameter(EntityParameter.Armor, ArmorBase);
                Parameters.SetBaseValueForParameter(EntityParameter.Mana, ManaBase);
                Parameters.SetBaseValueForParameter(EntityParameter.Health, HealthBase);
                Parameters.SetBaseValueForParameter(EntityParameter.CriticalDamage, CriticalDamageBase);
                Parameters.SetBaseValueForParameter(EntityParameter.ColdDamage, ColdDamageBase);

                mock.SetupGet(player => player.PassiveSkills).Returns(Skills);
                mock.SetupGet(player => player.ParameterModifiers).Returns(Modifiers);
                mock.SetupGet(player => player.Parameters).Returns(Parameters);
                mock.SetupGet(player => player.ModifierHandler).Returns(Handler);
                mock.SetupGet(player => player.CombatEvents).Returns(new CombatEventBus());
            }

            public IPlayer Player { get; }

            public IPassiveSkillsComponent Skills { get; }

            public IParameterModifiersComponent Modifiers { get; } = new ParameterModifiersComponent();

            public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();

            public IModifierHandlerComponent Handler { get; } = new ModifierHandlerComponent();

            public float Value(EntityParameter parameter) => Parameters.GetValueForParameter(parameter);

            public void Wear(ISkill skill) => Skills.AddSkill(skill);

            public void TakeOff(ISkill skill) => Skills.RemoveSkill(skill);

            /// <summary>One activation run through the fighter's own cast mutators — what a passive that
            /// changes the price of a cast is actually read by.</summary>
            public IAbilityActivationContext Cast(float cost)
            {
                var context = new Mock<IAbilityActivationContext>();
                context.SetupProperty(activation => activation.Cost, cost);
                context.SetupProperty(activation => activation.CostType, Costs.Mana);
                Handler.Apply(context.Object);

                return context.Object;
            }
        }

        /// <summary>Stands in for the data pipeline: the document is already parsed.</summary>
        private sealed class TreeStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
