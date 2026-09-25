namespace LastBreathTest.CraftingSystemTests
{
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>The roller's contracts: each slot family draws only from its own bucket, a bucket short
    /// on entries honestly leaves slots empty (no exception, no cross-family bleed), a stray None
    /// entry is skipped instead of ever occupying a slot, and no item is born wearing the same LINE
    /// twice — atoms by (parameter + value type), composites by their whole part set; a multi-part
    /// bundle still neither blocks nor is blocked by its parts' standalone atoms.</summary>
    [TestClass]
    public class AffixRollerTests
    {
        [TestMethod]
        public void Roll_RichPool_FillsExactlyTheRequestedSlotsPerFamily()
        {
            var pool = new List<IModifierDescriptor>
            {
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix),
                Entry(EntityParameter.Health, AffixKind.Prefix),
                Entry(EntityParameter.Armor, AffixKind.Prefix),
                Entry(EntityParameter.Strength, AffixKind.Suffix),
                Entry(EntityParameter.Dexterity, AffixKind.Suffix),
                Entry(EntityParameter.Accuracy, AffixKind.Suffix),
            };

            foreach ((int prefixes, int suffixes) in (ReadOnlySpan<(int, int)>)[(1, 0), (0, 1), (1, 1), (2, 1), (1, 2), (2, 2)])
            {
                var picked = AffixRoller.Roll(pool, prefixes, suffixes, new DefaultRandomNumberGenerator(seed: prefixes * 10 + suffixes));

                Assert.AreEqual(prefixes, picked.Count(descriptor => descriptor.Affix == AffixKind.Prefix));
                Assert.AreEqual(suffixes, picked.Count(descriptor => descriptor.Affix == AffixKind.Suffix));
            }
        }

        [TestMethod]
        public void Roll_SlotCountersHoldAcrossManySeeds()
        {
            var pool = new List<IModifierDescriptor>
            {
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix, weight: 100f),
                Entry(EntityParameter.Health, AffixKind.Prefix, weight: 1f),
                Entry(EntityParameter.Strength, AffixKind.Suffix, weight: 100f),
                Entry(EntityParameter.Dexterity, AffixKind.Suffix, weight: 1f),
            };

            for (int seed = 0; seed < 200; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 2, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count(descriptor => descriptor.Affix == AffixKind.Prefix), $"seed {seed}");
                Assert.AreEqual(2, picked.Count(descriptor => descriptor.Affix == AffixKind.Suffix), $"seed {seed}");
            }
        }

        [TestMethod]
        public void Roll_EmptyBucket_LeavesSlotsEmptyWithoutThrowing()
        {
            var pool = new List<IModifierDescriptor>
            {
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix),
                Entry(EntityParameter.Health, AffixKind.Prefix),
            };

            // Two suffix slots requested from a pool with zero suffixes: the item is born thinner, not broken.
            var picked = AffixRoller.Roll(pool, 2, 2, new DefaultRandomNumberGenerator(seed: 5));

            Assert.AreEqual(2, picked.Count);
            Assert.IsTrue(picked.All(descriptor => descriptor.Affix == AffixKind.Prefix));
        }

        [TestMethod]
        public void Roll_ShortBucket_YieldsWhatItHas()
        {
            var pool = new List<IModifierDescriptor>
            {
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix),
                Entry(EntityParameter.Strength, AffixKind.Suffix),
            };

            // Legendary asks 2+2 but each family only holds one entry.
            var picked = AffixRoller.Roll(pool, 2, 2, new DefaultRandomNumberGenerator(seed: 5));

            Assert.AreEqual(1, picked.Count(descriptor => descriptor.Affix == AffixKind.Prefix));
            Assert.AreEqual(1, picked.Count(descriptor => descriptor.Affix == AffixKind.Suffix));
        }

        [TestMethod]
        public void Roll_NoneEntry_IsSkippedAndNeverPicked()
        {
            var stray = Entry(EntityParameter.PhysicalDamage, AffixKind.None, weight: 100000f);
            var pool = new List<IModifierDescriptor>
            {
                stray,
                Entry(EntityParameter.Health, AffixKind.Prefix, weight: 1f),
                Entry(EntityParameter.Strength, AffixKind.Suffix, weight: 1f),
            };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 2, new DefaultRandomNumberGenerator(seed));
                Assert.IsFalse(picked.Contains(stray), $"seed {seed}: a None entry occupied a slot.");
            }
        }

        [TestMethod]
        public void Roll_TwoEntriesOfTheSameLine_NeverBothOccupySlots()
        {
            // The same (parameter, type) from two sources — family pool and the base's own pool speak about
            // Damage with different bounds. They are one line for the player: only one may land.
            var pool = new List<IModifierDescriptor>
            {
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix, weight: 100f),
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, new ValueRange(50f, 90f), ModifierScope.Global)
                    { Weight = 100f, Affix = AffixKind.Prefix },
                Entry(EntityParameter.Health, AffixKind.Prefix, weight: 1f),
            };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 0, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count, $"seed {seed}");
                Assert.AreEqual(1, picked.Count(descriptor => descriptor is ParameterDescriptor { Parameter: EntityParameter.PhysicalDamage }), $"seed {seed}: the same line landed twice.");
            }
        }

        [TestMethod]
        public void Roll_MultiPartComposite_DoesNotBlockItsPartsAtom()
        {
            // A multi-part bundle renders as ONE line whose identity is the whole set: its Damage part
            // neither blocks nor is blocked by the standalone Damage entry — both may sit on one item.
            var composite = Composite(AffixKind.Prefix, Part(EntityParameter.PhysicalDamage), Part(EntityParameter.Health));
            var pool = new List<IModifierDescriptor> { composite, Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix, weight: 100f) };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 0, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count, $"seed {seed}: the composite and the atom must both be pickable.");
                Assert.IsTrue(picked.Contains(composite), $"seed {seed}");
            }
        }

        [TestMethod]
        public void Roll_SameShapeComposites_NeverBothLand()
        {
            // Two entries with the SAME part set but different bounds (tiers from two sources) are one
            // line for the player — like same-key atoms, only one may occupy a slot.
            var familyTier = Composite(AffixKind.Prefix, Part(EntityParameter.PhysicalDamage, 10f), Part(EntityParameter.Health, 10f));
            var richTier = Composite(AffixKind.Prefix, Part(EntityParameter.PhysicalDamage, 50f), Part(EntityParameter.Health, 50f));
            var pool = new List<IModifierDescriptor> { familyTier, richTier, Entry(EntityParameter.Armor, AffixKind.Prefix, weight: 1f) };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 0, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count, $"seed {seed}");
                Assert.AreEqual(1, picked.Count(descriptor => descriptor is CompositeDescriptor), $"seed {seed}: the same composite line landed twice.");
            }
        }

        [TestMethod]
        public void Roll_SinglePartComposite_SharesTheAtomsIdentity()
        {
            // A one-part composite reads exactly like the atom, so the two compete for one identity —
            // an item is never born with "+50 Mana" twice through the bundle back door.
            var composite = Composite(AffixKind.Prefix, Part(EntityParameter.PhysicalDamage));
            var pool = new List<IModifierDescriptor>
            {
                composite,
                Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix, weight: 100f),
                Entry(EntityParameter.Health, AffixKind.Prefix, weight: 1f),
            };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 0, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count, $"seed {seed}");
                int damageLines = picked.Count(descriptor => descriptor == composite || descriptor is ParameterDescriptor { Parameter: EntityParameter.PhysicalDamage });
                Assert.AreEqual(1, damageLines, $"seed {seed}: the atom and the one-part composite are the same line.");
            }
        }

        private static ParameterDescriptor Entry(EntityParameter parameter, AffixKind affix, float weight = 10f) =>
            new(parameter, ModifierValueType.Flat, 10f, ModifierScope.Global) { Weight = weight, Affix = affix };

        private static ParameterDescriptor Part(EntityParameter parameter, float value = 10f) =>
            new(parameter, ModifierValueType.Flat, value, ModifierScope.Global);

        private static CompositeDescriptor Composite(AffixKind affix, params IModifierDescriptor[] parts) =>
            new([.. parts]) { Weight = 100f, Affix = affix };
    }
}
