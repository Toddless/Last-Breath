namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Modifiers.Conditions;
    using Crafting.Source;
    using LootGeneration.Internal;
    using Moq;

    /// <summary>A pool entry may name the condition its line only counts under, and that condition is part
    /// of what makes two entries the same line: "+X armor" and "+X armor while wounded" are priced apart,
    /// so a roll may hand out both and a reroll may trade one for the other. What the catalog cannot answer
    /// for costs the line — never the line without its gate.</summary>
    [TestClass]
    public class ConditionalLineTests
    {
        [TestMethod]
        public void APoolEntryNamingACondition_CarriesTheIdOntoItsDescriptor()
        {
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "parameter": "Armor", "modifierType": "increase", "value": 0.1, "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}" }
                ] } ] }
                """);

            Assert.AreEqual(TestConditions.Wounded, pool.Single().Condition);
        }

        [TestMethod]
        public void AConditionOnACompositePart_DropsTheWholeEntry()
        {
            // One bundle is one line to the player: a part gated apart from its siblings is half a line.
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "weight": 10, "affix": "Prefix", "parts": [
                        { "parameter": "Armor", "modifierType": "increase", "value": 0.1, "condition": "{{TestConditions.Wounded}}" },
                        { "parameter": "Evade", "modifierType": "increase", "value": 0.1 }
                    ] }
                ] } ] }
                """);

            Assert.AreEqual(0, pool.Count);
        }

        [TestMethod]
        public void AConditionOnAnEntryThatIsNotALine_DropsIt()
        {
            // A grant answers for itself and an operation is applied once — neither has anything a
            // predicate could hold up, so taking the condition and ignoring it would hand out the
            // ungated version of exactly what the data wrote a gate for.
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}", "grant": { "kind": "Passive", "id": "Passive_Skill_Regeneration" } },
                    { "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}", "extraUpgradeLevels": { "min": 1, "max": 5 } }
                ] } ] }
                """);

            Assert.AreEqual(0, pool.Count);
        }

        [TestMethod]
        public void AConditionOnAPipelineLine_IsRefusedInsteadOfIgnored()
        {
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "parameter": "HealingEfficiency", "modifierType": "increase", "value": 0.1, "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}" }
                ] } ] }
                """);

            Assert.AreEqual(0, pool.Count, "a knob that cannot carry the predicate must not take the line unconditionally");
        }

        [TestMethod]
        public void AConditionOnALineInsideAGrant_DropsThatLine()
        {
            // The lines of a grant are minted with it and land on the wearer through it — there is no entry
            // of their own for a predicate to travel in, so taking the id and ignoring it would hand out
            // "while wounded" for good and in silence.
            var grant = ParseGrant($$"""
                { "kind": "Modifier", "id": "Grant_Of_Stone", "modifiers": [
                    { "parameter": "Armor", "modifierType": "flat", "value": 50, "condition": "{{TestConditions.Wounded}}" },
                    { "parameter": "Health", "modifierType": "flat", "value": 100 }
                ] }
                """);

            Assert.AreEqual(1, grant.Modifiers.Count, "a gated line landed inside a grant that cannot gate it");
            Assert.AreEqual(EntityParameter.Health, grant.Modifiers.Single().EntityParameter);
        }

        [TestMethod]
        public void AConditionInsideAGrantsBundle_DropsTheWholeBundle()
        {
            var grant = ParseGrant($$"""
                { "kind": "Modifier", "id": "Grant_Of_Stone", "modifiers": [
                    { "weight": 10, "parts": [
                        { "parameter": "Armor", "modifierType": "flat", "value": 50, "condition": "{{TestConditions.Wounded}}" },
                        { "parameter": "Evade", "modifierType": "flat", "value": 50 }
                    ] }
                ] }
                """);

            Assert.AreEqual(0, grant.Modifiers.Count);
        }

        [TestMethod]
        public void AnUngatedGrantLine_StillParses()
        {
            var grant = ParseGrant("""
                { "kind": "Modifier", "id": "Grant_Of_Stone", "modifiers": [
                    { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                ] }
                """);

            Assert.AreEqual(1, grant.Modifiers.Count, "only the condition is refused here — a plain grant line is untouched");
        }

        [TestMethod]
        public void ANestedBundle_KeysByTheGateTheMintApplies()
        {
            // One bundle is one line and one line has one gate: the mint stamps the outermost root over
            // everything below it. Identity has to fold the same way, or the entry would not be recognized
            // in the very lines it produced.
            var inner = new CompositeDescriptor([Part(EntityParameter.Evade), Part(EntityParameter.Strength)])
            {
                Weight = 10f, Condition = TestConditions.AtFullHealth,
            };
            var entry = new CompositeDescriptor([Part(EntityParameter.Armor), inner])
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };
            var item = ItemWearing(entry);

            Assert.IsTrue(item.Modifiers.All(line => ((SimpleModifier)line).ConditionId == TestConditions.Wounded));
            Assert.IsTrue(LineIdentity.TryFrom(entry, out var identity));
            Assert.IsTrue(item.OccupiedLineIdentities([]).Contains(identity),
                "the bundle the item wears is not recognized as the entry it was rolled from");
        }

        [TestMethod]
        public void TheSameStatUnderDifferentConditions_AreDifferentLines()
        {
            Assert.IsTrue(LineIdentity.TryFrom(Descriptor(EntityParameter.Armor), out var plain));
            Assert.IsTrue(LineIdentity.TryFrom(Descriptor(EntityParameter.Armor, TestConditions.Wounded), out var wounded));
            Assert.IsTrue(LineIdentity.TryFrom(Descriptor(EntityParameter.Armor, TestConditions.AtFullHealth), out var healthy));

            Assert.AreNotEqual(plain, wounded);
            Assert.AreNotEqual(wounded, healthy);
        }

        [TestMethod]
        public void TheSameConditionWrittenInAnotherCase_IsTheSameLine()
        {
            // The catalog matches ids ignoring case, so identity must not split a condition into two.
            Assert.IsTrue(LineIdentity.TryFrom(Descriptor(EntityParameter.Armor, TestConditions.Wounded), out var written));
            Assert.IsTrue(LineIdentity.TryFrom(Descriptor(EntityParameter.Armor, TestConditions.Wounded.ToUpperInvariant()), out var shouted));

            Assert.AreEqual(written, shouted);
        }

        [TestMethod]
        public void ARoll_FillsTwoSlotsWithOneStatUnderTwoConditions()
        {
            var pool = new IModifierDescriptor[]
            {
                Descriptor(EntityParameter.Armor, weight: 10f),
                Descriptor(EntityParameter.Armor, TestConditions.Wounded, weight: 10f),
            };

            var rolled = AffixRoller.Roll(pool, prefixes: 2, suffixes: 0, new DefaultRandomNumberGenerator(seed: 7));

            Assert.AreEqual(2, rolled.Count, "the gated line lost its slot to the plain one — they are not the same line");
            CollectionAssert.AreEquivalent(
                new[] { null, TestConditions.Wounded },
                rolled.Select(descriptor => descriptor.Condition).ToArray());
        }

        [TestMethod]
        public void ARoll_StillRefusesTheSameStatUnderTheSameCondition()
        {
            var pool = new IModifierDescriptor[]
            {
                Descriptor(EntityParameter.Armor, TestConditions.Wounded, weight: 10f),
                Descriptor(EntityParameter.Armor, TestConditions.Wounded, weight: 10f),
            };

            var rolled = AffixRoller.Roll(pool, prefixes: 2, suffixes: 0, new DefaultRandomNumberGenerator(seed: 7));

            Assert.AreEqual(1, rolled.Count);
        }

        [TestMethod]
        public void AReroll_IsNotBlockedByTheSameStatWornUnderACondition()
        {
            // The item wears "+X Strength while wounded"; the pool offers the plain "+X Strength". They are
            // two lines, so the reroll must serve instead of refusing for a slot that is not taken.
            var item = ItemWith(Line(EntityParameter.Strength, TestConditions.Wounded), Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNotNull(rolled, "the gated line the item wears occupied the slot of the plain one");
            var replacement = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rolled);
            Assert.AreEqual(EntityParameter.Strength, replacement.EntityParameter);
            Assert.IsNull(replacement.ConditionId);
        }

        [TestMethod]
        public void AReroll_IsStillBlockedByTheSameStatUnderTheSameCondition()
        {
            var item = ItemWith(Line(EntityParameter.Strength, TestConditions.Wounded), Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, TestConditions.Wounded, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNull(rolled, "the item already wears this line");
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void MaterializingAConditionalEntry_BuildsThePredicateAndKeepsTheId()
        {
            var sink = Materialize(Descriptor(EntityParameter.Armor, TestConditions.Wounded), TestConditions.Holding());

            var line = (SimpleModifier)sink.Entities.Single();
            Assert.AreEqual(TestConditions.Wounded, line.ConditionId);
            Assert.IsNotNull(line.Condition, "the line was minted without the predicate that answers for it");
            Assert.IsFalse(line.IsActive, "an unattached predicate is met by nobody");
        }

        [TestMethod]
        public void MaterializingAnEntryNamingAnUnknownCondition_MintsNoLineAtAll()
        {
            var sink = Materialize(Descriptor(EntityParameter.Armor, "While_The_Moon_Is_Full"), TestConditions.Holding());

            Assert.AreEqual(0, sink.Entities.Count, "an unknown condition must cost the line, not turn it into an unconditional one");
        }

        [TestMethod]
        public void MaterializingAConditionalEntryWithoutACatalog_MintsNoLineAtAll()
        {
            var sink = Materialize(Descriptor(EntityParameter.Armor, TestConditions.Wounded), catalog: null);

            Assert.AreEqual(0, sink.Entities.Count);
        }

        [TestMethod]
        public void MaterializingAPlainEntryWithoutACatalog_IsUntouched()
        {
            var sink = Materialize(Descriptor(EntityParameter.Armor), catalog: null);

            var line = (SimpleModifier)sink.Entities.Single();
            Assert.IsNull(line.ConditionId);
            Assert.IsTrue(line.IsActive, "a line that names no condition counts always");
        }

        [TestMethod]
        public void ACompositeCondition_ReachesEveryPartAsAPredicateOfItsOwn()
        {
            var composite = new CompositeDescriptor([Part(EntityParameter.Armor), Part(EntityParameter.Evade)])
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };

            var parts = Materialize(composite, TestConditions.Holding()).Entities.Cast<SimpleModifier>().ToList();

            Assert.AreEqual(2, parts.Count);
            Assert.IsTrue(parts.All(part => part.ConditionId == TestConditions.Wounded));
            Assert.AreNotSame(parts[0].Condition, parts[1].Condition, "the parts share one predicate: releasing one would disarm the other");
        }

        [TestMethod]
        public void AConditionalPipelineEntry_MintsNothing()
        {
            var descriptor = new ContextDescriptor(ContextParameter.HealingEfficiency, ModifierValueType.Increase, new ValueRange(0.1f, 0.1f))
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };

            var sink = Materialize(descriptor, TestConditions.Holding());

            Assert.AreEqual(0, sink.Contexts.Count, "a pipeline line cannot carry its predicate, so it must not be minted without one");
        }

        [TestMethod]
        public void AKeyBuiltPastTheFactories_StillNormalizesItsCondition()
        {
            // The positional constructor and `with` are public too: a key seated through either has to
            // compare like one the factories built, or how an id was typed would split one line into two.
            var built = ModifierKey.From(EntityParameter.Armor, ModifierValueType.Flat, TestConditions.Wounded);
            var raw = new ModifierKey(ModifierChannel.Entity, (int)EntityParameter.Armor, ModifierValueType.Flat, TestConditions.Wounded.ToUpperInvariant());

            Assert.AreEqual(built, raw);
            Assert.AreEqual(built, raw with { Condition = $" {TestConditions.Wounded.ToUpperInvariant()} " });
        }

        [TestMethod]
        public void AConditionalCompositeHidingAGrant_DoesNotParse()
        {
            // The gate belongs to the WHOLE bundle: a grant answers for itself, so a root condition it
            // cannot honour would reach the item as a behaviour handed out for good next to a gated stat.
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}", "parts": [
                        { "parameter": "Armor", "modifierType": "increase", "value": 0.1 },
                        { "grant": { "kind": "Passive", "id": "Passive_Skill_Regeneration" } }
                    ] }
                ] } ] }
                """);

            Assert.AreEqual(0, pool.Count, "a bundle whose gate cannot reach all of it must not be rollable at all");
        }

        [TestMethod]
        public void AConditionalCompositeHidingAPipelineKnob_DoesNotParse()
        {
            // The knob cannot carry the predicate, so letting the entry through would put the parametric
            // half of an authored line on the item and drop the rest with an error.
            var pool = ParsePool($$"""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "weight": 10, "affix": "Prefix", "condition": "{{TestConditions.Wounded}}", "parts": [
                        { "parameter": "Armor", "modifierType": "increase", "value": 0.1 },
                        { "parameter": "HealingEfficiency", "modifierType": "increase", "value": 0.1 }
                    ] }
                ] } ] }
                """);

            Assert.AreEqual(0, pool.Count);
        }

        [TestMethod]
        public void AnUngatedCompositeHidingAGrant_StillParses()
        {
            var pool = ParsePool("""
                { "pools": [ { "id": "Ring", "modifiersPool": [
                    { "weight": 10, "affix": "Prefix", "parts": [
                        { "parameter": "Armor", "modifierType": "increase", "value": 0.1 },
                        { "grant": { "kind": "Passive", "id": "Passive_Skill_Regeneration" } }
                    ] }
                ] } ] }
                """);

            Assert.AreEqual(1, pool.Count, "only the condition is refused here — an ungated bundle is untouched");
        }

        [TestMethod]
        public void MaterializingAConditionalCompositeHidingAGrant_MintsNothingAtAll()
        {
            var composite = new CompositeDescriptor([Part(EntityParameter.Armor), Grant()])
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };

            var sink = MaterializeWithGrants(composite);

            Assert.AreEqual(0, sink.Grants.Count, "a behaviour the data wrote a gate for was handed out for good");
            Assert.AreEqual(0, sink.Entities.Count, "the rest of the bundle landed as half of an authored line");
        }

        [TestMethod]
        public void MaterializingAnUngatedCompositeHidingAGrant_IsUntouched()
        {
            var composite = new CompositeDescriptor([Part(EntityParameter.Armor), Grant()]) { Weight = 10f, Affix = AffixKind.Prefix };

            var sink = MaterializeWithGrants(composite);

            Assert.AreEqual(1, sink.Grants.Count);
            Assert.AreEqual(1, sink.Entities.Count);
        }

        [TestMethod]
        public void MaterializingAConditionalCompositeHidingAPipelineKnob_MintsNoHalfLine()
        {
            var composite = new CompositeDescriptor([Part(EntityParameter.Armor), Knob()])
            {
                Weight = 10f, Affix = AffixKind.Prefix, Condition = TestConditions.Wounded,
            };

            var sink = Materialize(composite, TestConditions.Holding());

            Assert.AreEqual(0, sink.Entities.Count, "the parametric half landed while the knob half was refused");
            Assert.AreEqual(0, sink.Contexts.Count);
        }

        [TestMethod]
        public void AConditionalComposite_IsNotTheSameLineAsThePlainOne()
        {
            Assert.IsTrue(LineIdentity.TryFrom(Composite(condition: null), out var plain));
            Assert.IsTrue(LineIdentity.TryFrom(Composite(TestConditions.Wounded), out var gated));

            Assert.AreNotEqual(plain, gated, "a bundle and its gated twin read as one line to every roll");
        }

        [TestMethod]
        public void AWornConditionalComposite_OccupiesTheIdentityOfItsOwnPoolEntry()
        {
            // The item's side of the invariant is folded from the worn parts, each stamped with the root's
            // condition; the pool's side is folded from the entry. They have to meet.
            var entry = Composite(TestConditions.Wounded);
            var item = ItemWearing(entry);

            Assert.IsTrue(LineIdentity.TryFrom(entry, out var identity));
            Assert.IsTrue(item.OccupiedLineIdentities([]).Contains(identity),
                "the bundle the item wears is not recognized as the entry it was rolled from");
        }

        [TestMethod]
        public void ARerollNextToAWornConditionalComposite_RefusesTheSameBundleUnderTheSameCondition()
        {
            var item = ItemWearing(Composite(TestConditions.Wounded));
            item.AddAdditionalModifier(Line(EntityParameter.Strength));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(TestConditions.Wounded)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNull(rolled, "the item would wear the same gated bundle twice");
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void ARerollNextToAWornPlainComposite_StillServesItsGatedTwin()
        {
            var item = ItemWearing(Composite(condition: null));
            item.AddAdditionalModifier(Line(EntityParameter.Strength));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(TestConditions.Wounded)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNotNull(rolled, "the plain bundle the item wears occupied the slot of the gated one");
            Assert.AreEqual(TestConditions.Wounded, ((SimpleModifier)item.Modifiers.Single(line => line.InstanceId == rolled)).ConditionId);
        }

        [TestMethod]
        public void ARerollThatWouldMintNothing_KeepsTheLineAndCostsNothing()
        {
            // The only candidate names a predicate this host cannot build, so it mints no line at all.
            // Taking the old row away for it would be a free destruction of what the player paid for.
            var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Dexterity, "While_The_Moon_Is_Full", weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNull(rolled);
            Assert.AreEqual(2, item.Modifiers.Count, "an impossible reroll destroyed the line it could not replace");
            Assert.AreEqual(0, item.RecraftCount, "a reroll that put no line back still raised the price of the next one");
        }

        private static List<IModifierDescriptor> ParsePool(string json) =>
            new DataParser(new ItemGameDataFactory()).ParseEquipItemModifierPools(json)["Ring"];

        /// <summary>The single grant of a template — the OTHER parse path an <c>ItemModifier</c> travels,
        /// the one that builds a grant's own lines instead of rollable descriptors.</summary>
        private static GrantBlueprint ParseGrant(string grant) =>
            new DataParser(new ItemGameDataFactory()).ParseEquipItems($$"""
                { "items": [ {
                    "id": "Band", "equipmentPart": "Ring", "rarity": "Rare",
                    "updateLevel": 0, "maxUpdateLevel": 12,
                    "grants": [ {{grant}} ]
                } ] }
                """).Single().Grants.Single();

        private static CollectingSink Materialize(IModifierDescriptor descriptor, IConditionProvider? catalog) =>
            Materialize(descriptor, catalog, grantFactory: null);

        /// <summary>A host that CAN mint grants — without one the grant bucket stays empty for a reason
        /// that has nothing to do with the condition under test.</summary>
        private static CollectingSink MaterializeWithGrants(IModifierDescriptor descriptor)
        {
            var factory = new Mock<IGrantFactory>();
            factory.Setup(mock => mock.Create(It.IsAny<GrantKind>(), It.IsAny<string>(), It.IsAny<List<IModifier>>(), It.IsAny<IReadOnlyDictionary<string, float>>()))
                .Returns(Mock.Of<IItemGrant>());
            return Materialize(descriptor, TestConditions.Holding(), factory.Object);
        }

        private static CollectingSink Materialize(IModifierDescriptor descriptor, IConditionProvider? catalog, IGrantFactory? grantFactory)
        {
            var sink = new CollectingSink();
            new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 11), grantFactory, catalog).Materialize(descriptor, sink, "test");
            return sink;
        }

        private static ParameterDescriptor Descriptor(EntityParameter parameter, string? condition = null, float weight = 10f) =>
            new(parameter, ModifierValueType.Flat, new ValueRange(10f, 10f), ModifierScope.Global)
            {
                Weight = weight, Affix = AffixKind.Prefix, Condition = condition,
            };

        private static ParameterDescriptor Part(EntityParameter parameter) =>
            new(parameter, ModifierValueType.Flat, new ValueRange(10f, 10f), ModifierScope.Global);

        /// <summary>A two-stat bundle — one player-facing line whose gate, if any, sits on the root.</summary>
        private static CompositeDescriptor Composite(string? condition, float weight = 10f) =>
            new([Part(EntityParameter.Armor), Part(EntityParameter.Evade)])
            {
                Weight = weight, Affix = AffixKind.Prefix, Condition = condition,
            };

        private static GrantDescriptor Grant() =>
            new(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float>());

        private static ContextDescriptor Knob() =>
            new(ContextParameter.HealingEfficiency, ModifierValueType.Increase, new ValueRange(0.1f, 0.1f));

        private static EquipItem ItemWith(params SimpleModifier[] lines)
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            foreach (var line in lines) item.AddAdditionalModifier(line);
            return item;
        }

        /// <summary>An item wearing the entry the way a roll leaves it — every part minted and grouped.</summary>
        private static EquipItem ItemWearing(IModifierDescriptor entry)
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            foreach (var line in Materialize(entry, TestConditions.Holding()).Entities) item.AddAdditionalModifier(line);
            return item;
        }

        /// <summary>A worn line, in the slot family the pool entries above are written for — the reroll
        /// keeps the family, so a candidate of another one would be filtered out before identity is asked.</summary>
        private static SimpleModifier Line(EntityParameter parameter, string? condition = null) =>
            new(parameter, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix, ConditionId = condition };

        private static IItemDataProvider PoolOf(params IModifierDescriptor[] entries)
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Band")).Returns(entries);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            return provider.Object;
        }

        private static ItemUpgrader CreateUpgrader(int seed, IItemDataProvider provider)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            return new ItemUpgrader(rnd, Mock.Of<ICraftingMastery>(), provider,
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd, grantFactory: null, TestConditions.Holding()));
        }
    }
}
