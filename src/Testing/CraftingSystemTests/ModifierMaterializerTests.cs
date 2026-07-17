namespace LastBreathTest.CraftingSystemTests
{
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Moq;

    /// <summary>The materializer is the ONLY point where a value range becomes a number. Contracts under
    /// guard: ranged values roll inside their bounds on the injected (seedable) RNG; fixed values stay
    /// bit-identical to the pre-range behavior and consume zero rolls; composite parts share one GroupId
    /// and inherit the root affix; roll provenance survives every copy path.</summary>
    [TestClass]
    public class ModifierMaterializerTests
    {
        [TestMethod]
        public void Materialize_RangedValue_RollsWithinBounds()
        {
            var range = new ValueRange(10f, 20f);
            var descriptor = new ParameterDescriptor(EntityParameter.Damage, ModifierValueType.Flat, range, ModifierScope.Global);
            var sink = new CollectingSink();

            new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 123)).Materialize(descriptor, sink, "test");

            var line = (SimpleModifier)sink.Entities.Single();
            Assert.IsTrue(line.BaseValue is >= 10f and <= 20f, $"rolled {line.BaseValue} outside [10..20]");
            Assert.AreEqual(line.BaseValue, line.Value); // materialized instance starts unscaled
            Assert.AreEqual(range, line.RolledRange); // the source spread is kept for the Alt tooltip
        }

        [TestMethod]
        public void Materialize_FixedValue_BitIdenticalAndConsumesNoRng()
        {
            var descriptor = new ParameterDescriptor(EntityParameter.Health, ModifierValueType.Flat, 50f, ModifierScope.Global);
            var sink = new CollectingSink();
            var rnd = new Mock<IRandomNumberGenerator>(MockBehavior.Strict); // ANY call would throw

            new ModifierMaterializer(rnd.Object).Materialize(descriptor, sink, "test");

            var line = (SimpleModifier)sink.Entities.Single();
            Assert.AreEqual(50f, line.BaseValue); // exact, not approximate: legacy content must not drift
            Assert.IsNull(line.RolledRange); // fixed lines carry no range — nothing was rolled
            Assert.IsNull(line.GroupId);
            Assert.AreEqual(AffixKind.None, line.Affix);
        }

        [TestMethod]
        public void Materialize_Composite_SharesGroupIdAndInheritsRootAffix()
        {
            var composite = new CompositeDescriptor(
            [
                new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Flat, new ValueRange(1f, 5f), ModifierScope.Global),
                new ParameterDescriptor(EntityParameter.Evade, ModifierValueType.Flat, 3f, ModifierScope.Global),
                new ContextDescriptor(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.15f),
            ]) { Affix = AffixKind.Prefix };
            var sink = new CollectingSink();

            new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 7)).Materialize(composite, sink, "test");

            Assert.AreEqual(2, sink.Entities.Count);
            Assert.AreEqual(1, sink.Contexts.Count);
            var first = (SimpleModifier)sink.Entities[0];
            var second = (SimpleModifier)sink.Entities[1];
            var context = sink.Contexts[0];
            Assert.IsNotNull(first.GroupId);
            Assert.AreEqual(first.GroupId, second.GroupId);
            Assert.AreEqual(first.GroupId, context.GroupId);
            Assert.AreEqual(AffixKind.Prefix, first.Affix);
            Assert.AreEqual(AffixKind.Prefix, second.Affix);
            Assert.AreEqual(AffixKind.Prefix, context.Affix);
        }

        [TestMethod]
        public void Materialize_TwoComposites_GetDistinctGroupIds()
        {
            var materializer = new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 7));
            var sink = new CollectingSink();
            for (int roll = 0; roll < 2; roll++)
                materializer.Materialize(new CompositeDescriptor(
                    [new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Flat, 1f, ModifierScope.Global)]), sink, "test");

            Assert.AreNotEqual(
                ((SimpleModifier)sink.Entities[0]).GroupId,
                ((SimpleModifier)sink.Entities[1]).GroupId);
        }

        [TestMethod]
        public void Scale_MultiplicativeDelta_ScalesLinearly()
        {
            // Multi data stores the bonus delta (0.15 = +15%), so quality x2 doubles the delta — the old
            // factor-aware branch (1.15 -> 1.3) is gone together with factor-form data.
            var descriptor = new ParameterDescriptor(EntityParameter.Health, ModifierValueType.Multiplicative, 0.15f, ModifierScope.Global);

            var scaled = (ParameterDescriptor)DescriptorOperations.Scale(descriptor, 2f);

            Assert.AreEqual(0.3f, scaled.Value.Min, 0.0001f);
            Assert.AreEqual(0.3f, scaled.Value.Max, 0.0001f);
        }

        [TestMethod]
        public void Scale_Range_ScalesBothBounds()
        {
            var descriptor = new ParameterDescriptor(EntityParameter.Damage, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global);

            var scaled = (ParameterDescriptor)DescriptorOperations.Scale(descriptor, 1.5f);

            Assert.AreEqual(15f, scaled.Value.Min, 0.0001f);
            Assert.AreEqual(30f, scaled.Value.Max, 0.0001f);
        }

        [TestMethod]
        public void Flatten_KeepsRootAffixOnAtoms()
        {
            var composite = new CompositeDescriptor(
            [
                new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Flat, 1f, ModifierScope.Global),
                new ContextDescriptor(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.1f),
            ]) { Affix = AffixKind.Suffix };

            var atoms = DescriptorOperations.Flatten([composite]).ToList();

            Assert.AreEqual(2, atoms.Count);
            Assert.AreEqual(AffixKind.Suffix, ((ParameterDescriptor)atoms[0]).Affix);
            Assert.AreEqual(AffixKind.Suffix, ((ContextDescriptor)atoms[1]).Affix);
        }

        [TestMethod]
        public void EquipItemCopy_KeepsRollProvenanceStamps()
        {
            var descriptor = new CompositeDescriptor(
            [
                new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global),
            ]) { Affix = AffixKind.Prefix };
            var sink = new CollectingSink();
            new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 5)).Materialize(descriptor, sink, "test");
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.AddAdditionalModifier(sink.Entities.Single());
            var original = (SimpleModifier)item.Modifiers.Single();

            var copied = (SimpleModifier)item.Copy<EquipItem>().Modifiers.Single();

            Assert.AreEqual(original.Affix, copied.Affix);
            Assert.AreEqual(original.GroupId, copied.GroupId);
            Assert.AreEqual(original.RolledRange, copied.RolledRange);
        }

        [TestMethod]
        public void ContextEntryCopy_KeepsRollProvenanceStamps()
        {
            var entry = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.2f)
            {
                Affix = AffixKind.Suffix,
                GroupId = "group-1",
                RolledRange = new ValueRange(0.1f, 0.3f),
            };

            var copy = entry.Copy();

            Assert.AreEqual(AffixKind.Suffix, copy.Affix);
            Assert.AreEqual("group-1", copy.GroupId);
            Assert.AreEqual(new ValueRange(0.1f, 0.3f), copy.RolledRange);
        }
    }
}
