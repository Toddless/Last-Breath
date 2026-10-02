namespace LastBreathTest.CraftingSystem
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Crafting.Source;
    using Moq;

    /// <summary>The reroll never hands the item a line it already wears: identity is what the line is ABOUT,
    /// never the values ("+35% damage" and "+33% damage" are the same line) — atoms by (parameter, value
    /// type), composites by their whole part set. A multi-part bundle neither blocks nor is blocked by its
    /// parts' standalone atoms (only a full set match is a duplicate); the rerolled line/group itself is
    /// lifted and may come back as the same stat or composite with fresh values.</summary>
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
        public void Reroll_PutsTheFreshLineBackIntoTheRerolledSlot()
        {
            // Middle line rerolled: the replacement must land in slot 1, not at the bottom of the list —
            // the row the player clicked stays where they clicked it.
            var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence), Line(EntityParameter.Evade));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Dexterity, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers[1].InstanceId);

            Assert.IsNotNull(rolled);
            Assert.AreEqual(rolled, item.Modifiers[1].InstanceId, "The fresh line took the slot of the one it replaced.");
            CollectionAssert.AreEqual(
                new[] { EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Evade },
                item.Modifiers.Select(modifier => modifier.EntityParameter).ToArray());
        }

        [TestMethod]
        public void Reroll_CompositeGroup_TakesTheSlotOfItsFirstPart()
        {
            // The whole group leaves and one atom replaces it — that atom inherits the group's slot.
            var item = ItemWith(
                Line(EntityParameter.Strength),
                Line(EntityParameter.Intelligence, groupId: "composite"),
                Line(EntityParameter.Evade, groupId: "composite"),
                Line(EntityParameter.Accuracy));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Descriptor(EntityParameter.Dexterity, weight: 10f)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers[1].InstanceId);

            Assert.IsNotNull(rolled);
            CollectionAssert.AreEqual(
                new[] { EntityParameter.Strength, EntityParameter.Dexterity, EntityParameter.Accuracy },
                item.Modifiers.Select(modifier => modifier.EntityParameter).ToArray());
        }

        [TestMethod]
        public void Reroll_CompositeCandidate_LandsAsOneGroupedLine()
        {
            // Bug 85: a composite pool entry must be pickable by the reroll and land as a whole group —
            // several stamped lines sharing a fresh GroupId, sitting in the rerolled slot.
            var item = ItemWith(Line(EntityParameter.Intelligence), Line(EntityParameter.Accuracy));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Strength), Part(EntityParameter.Evade))));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.First().InstanceId);

            Assert.IsNotNull(rolled);
            CollectionAssert.AreEqual(
                new[] { EntityParameter.Strength, EntityParameter.Evade, EntityParameter.Accuracy },
                item.Modifiers.Select(modifier => modifier.EntityParameter).ToArray(),
                "The group replaces the rerolled line in place.");
            var group = item.Modifiers.Take(2).Cast<SimpleModifier>().ToList();
            Assert.IsNotNull(group[0].GroupId, "Composite parts must land grouped — one line for the player.");
            Assert.AreEqual(group[0].GroupId, group[1].GroupId);
            Assert.IsTrue(group.All(part => part.Affix == AffixKind.Suffix), "Parts inherit the root affix.");
            Assert.AreEqual(rolled, group[0].InstanceId);
            Assert.AreEqual(1, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_CompositeWithPartialOverlap_StaysLegal()
        {
            // The item already wears Strength as a standalone line; a composite carrying a Strength part
            // stays legal — a bundle's identity is the WHOLE set, partial overlap is not a duplicate.
            var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Strength), Part(EntityParameter.Evade))));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNotNull(rolled, "A composite bundle must not be locked out by its parts' keys.");
            Assert.AreEqual(3, item.Modifiers.Count);
        }

        [TestMethod]
        public void Reroll_SameShapeCompositeAlreadyWorn_IsRefused()
        {
            // The item wears a {Strength, Evade} group; a candidate with the SAME part set (whatever its
            // bounds) is the same line for the player — refuse without touching the item.
            var item = ItemWith(
                Line(EntityParameter.Strength, groupId: "composite"),
                Line(EntityParameter.Evade, groupId: "composite"),
                Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Strength), Part(EntityParameter.Evade))));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNull(rolled, "The item already wears this composite line.");
            Assert.AreEqual(3, item.Modifiers.Count);
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_TargetGroup_MayComeBackAsTheSameComposite()
        {
            // Rerolling the group itself lifts its identity: a pool holding nothing but the same-shape
            // composite still serves — the player is buying fresh values for the same line.
            var item = ItemWith(Line(EntityParameter.Strength, groupId: "composite"), Line(EntityParameter.Evade, groupId: "composite"));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Strength), Part(EntityParameter.Evade))));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.First().InstanceId);

            Assert.IsNotNull(rolled);
            Assert.AreEqual(2, item.Modifiers.Count);
            Assert.AreEqual(1, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_SinglePartComposite_IsBlockedByTheAtom()
        {
            // A one-part composite reads exactly like the atom — the worn "+X Strength" line blocks it.
            var item = ItemWith(Line(EntityParameter.Strength), Line(EntityParameter.Intelligence));
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Strength))));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Last().InstanceId);

            Assert.IsNull(rolled);
            Assert.AreEqual(2, item.Modifiers.Count);
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_CompositeWithAGrantPart_IsNotPickable()
        {
            // A grant hiding among composite parts would materialize outside the line channels —
            // the whole bundle fails the line-for-line contract and never enters the candidates.
            var item = ItemWith(Line(EntityParameter.Strength));
            var grantPart = new GrantDescriptor(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float>());
            var upgrader = CreateUpgrader(seed: 3, PoolOf(Composite(10f, Part(EntityParameter.Evade), grantPart)));

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Single().InstanceId);

            Assert.IsNull(rolled);
            Assert.AreEqual(1, item.Modifiers.Count, "A refusal must not remove the target line.");
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_NonLineCandidates_AreNotPickable()
        {
            // A reroll swaps a line for a line. A grant entry or the sharpening-levels operation would take
            // the old line away and put no line back, so neither may enter the candidate list.
            var item = ItemWith(Line(EntityParameter.Strength));
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Band")).Returns(
            [
                new GrantDescriptor(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float>()) { Weight = 100f, Affix = AffixKind.Suffix },
                new UpgradeLevelsDescriptor(1, 69) { Weight = 100f, Affix = AffixKind.Suffix },
            ]);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            var upgrader = CreateUpgrader(seed: 3, provider.Object);

            string? rolled = upgrader.TryRecraftModifier(item, item.Modifiers.Single().InstanceId);

            Assert.IsNull(rolled, "Nothing rerollable in the pool — refuse instead of eating the line.");
            Assert.AreEqual(1, item.Modifiers.Count);
            Assert.AreEqual(0, item.RecraftCount);
        }

        [TestMethod]
        public void Reroll_CandidatesWithoutWeight_RefuseInsteadOfThrowing()
        {
            // Weight 0 means "never rolls" — a candidate list carrying no weight at all must refuse
            // like an empty one, not blow up the picker.
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

        /// <summary>A part carries no affix and no weight — both live on the composite root (parse contract).</summary>
        private static ParameterDescriptor Part(EntityParameter parameter) =>
            new(parameter, ModifierValueType.Flat, new ValueRange(10f, 10f), ModifierScope.Global);

        private static CompositeDescriptor Composite(float weight, params IModifierDescriptor[] parts) =>
            new([.. parts]) { Weight = weight, Affix = AffixKind.Suffix };

        /// <summary>Provider whose item pool holds the given entries and whose other pool sources are empty.</summary>
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
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd));
        }
    }
}
