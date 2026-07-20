namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Crafting.Source;
    using Moq;

    /// <summary>The reroll never hands the item a line it already wears: identity is (parameter, value type),
    /// the value plays no part ("+35% damage" and "+33% damage" are the same line). Exempt by design: the
    /// rerolled line itself (it may come back as the same stat with a fresh value) and composite groups
    /// (they render as a single line, so their parts neither block nor are blocked).</summary>
    [TestClass]
    public class RecraftDuplicateTests
    {
        [TestMethod]
        public void Reroll_LineTheItemAlreadyWears_IsNeverPicked()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence));
                // Strength outweighs Dexterity 100:1 — without the guard it would win almost every roll.
                var upgrader = CreateUpgrader(seed, PoolOf(
                    Descriptor(EntityParameter.Strength, weight: 100f),
                    Descriptor(EntityParameter.Dexterity, weight: 1f)));

                string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

                Assert.IsNotNull(rolled, $"seed {seed}: Dexterity was available.");
                Assert.AreEqual(1, item.Modifiers.Count(modifier => modifier.EntityParameter == EntityParameter.Strength), $"seed {seed}: the item wears Strength twice.");
                Assert.AreEqual(EntityParameter.Dexterity, item.Modifiers.Single(modifier => modifier.InstanceId == rolled).EntityParameter, $"seed {seed}");
            }
        }

        [TestMethod]
        public void Reroll_SameStatWithAFreshValue_StaysLegal()
        {
            // The rerolled line is being replaced, so its own key must not lock itself out: a pool holding
            // nothing but Strength still rerolls — the player is buying a new value.
            var item = ItemWith(Line(EntityParameter.Strength));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 10f, min: 1f, max: 100f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Single().InstanceId);

            Assert.IsNotNull(rolled);
            Assert.AreEqual(1, item.Modifiers.Count);
            Assert.AreEqual(EntityParameter.Strength, item.Modifiers.Single().EntityParameter);
            Assert.AreEqual(1, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_PoolFullyOccupiedByOtherLines_RefusesWithoutTouchingTheItem()
        {
            // The only candidate is already worn by ANOTHER line: the refusal must happen before anything
            // leaves the item — the target line stays put and the growing price does not move.
            var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence));
            var target = item.Modifiers.Last();
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, target.InstanceId);

            Assert.IsNull(rolled);
            Assert.AreEqual(2, item.Modifiers.Count, "A refusal must not remove the target line.");
            Assert.IsTrue(item.Modifiers.Any(modifier => modifier.InstanceId == target.InstanceId));
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_CompositeParts_DoNotBlockACandidate()
        {
            // Strength sits on the item as part of a composite line — one line for the player, so the
            // standalone Strength candidate stays legal.
            var item = ItemWith(
                Line(EntityParameter.Strength, groupId: "composite"),
                Line(EntityParameter.Dexterity, groupId: "composite"),
                Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNotNull(rolled);
            Assert.AreEqual(EntityParameter.Strength, item.Modifiers.Single(modifier => modifier.InstanceId == rolled).EntityParameter);
            Assert.AreEqual(3, item.Modifiers.Count, "The composite parts must survive untouched.");
        }

        [TestMethod]
        public void Reroll_CompositeGroup_IsNotBlockedByItsOwnParts()
        {
            // Rerolling the group replaces ALL its parts with one atom: the keys leaving the item must not
            // block the replacement, so a pool of nothing but Strength still serves it.
            var item = ItemWith(Line(EntityParameter.Strength, groupId: "composite"), Line(EntityParameter.Dexterity, groupId: "composite"));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.First().InstanceId);

            Assert.IsNotNull(rolled);
            Assert.AreEqual(1, item.Modifiers.Count, "The whole group leaves, one fresh line replaces it.");
            Assert.AreEqual(EntityParameter.Strength, item.Modifiers.Single().EntityParameter);
        }

        [TestMethod]
        public void Reroll_CandidatesWithoutWeight_RefuseInsteadOfThrowing()
        {
            // Weight 0 means "never rolls" — composite parts inherit it by parse contract, so a candidate
            // list of nothing but flattened atoms must refuse like an empty one, not blow up the picker.
            var item = ItemWith(Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Strength, weight: 0f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Single().InstanceId);

            Assert.IsNull(rolled);
            Assert.AreEqual(1, item.Modifiers.Count, "A refusal must not remove the target line.");
            Assert.AreEqual(0, item.RecraftCount);
        }

        private static EquipItem ItemWith(params SimpleModifier[] lines)
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            foreach (var line in lines) item.AddAdditionalModifier(line);
            return item;
        }

        private static SimpleModifier Line(EntityParameter parameter, string? groupId = null) =>
            new(parameter, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix, GroupId = groupId };

        private static ParameterDescriptor Descriptor(EntityParameter parameter, float weight, float min = 10f, float max = 10f) =>
            new(parameter, ModifierValueType.Flat, new ValueRange(min, max), ModifierScope.Global) { Weight = weight, Affix = AffixKind.Suffix };

        /// <summary>Provider whose item pool holds the given entries and whose other pool sources are empty.</summary>
        private static IItemDataProvider PoolOf(params ParameterDescriptor[] entries)
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
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd));
        }
    }
}
