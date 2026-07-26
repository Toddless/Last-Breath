namespace LastBreathTest.CraftingSystemTests
{
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>The roller's contracts: each slot family draws only from its own bucket, a bucket short
    /// on entries honestly leaves slots empty (no exception, no cross-family bleed), a stray None
    /// entry is skipped instead of ever occupying a slot, and no item is born wearing the same line
    /// (parameter + value type) twice — composites excepted.</summary>
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
        public void Roll_CompositeEntry_IsExemptFromLineDedup()
        {
            // A composite renders as ONE line, so its Damage part neither blocks nor is blocked by the
            // standalone Damage entry — both may sit on the same item.
            var composite = new CompositeDescriptor(
                [new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 10f, ModifierScope.Global)])
                { Weight = 100f, Affix = AffixKind.Prefix };
            var pool = new List<IModifierDescriptor> { composite, Entry(EntityParameter.PhysicalDamage, AffixKind.Prefix, weight: 100f) };

            for (int seed = 0; seed < 50; seed++)
            {
                var picked = AffixRoller.Roll(pool, 2, 0, new DefaultRandomNumberGenerator(seed));

                Assert.AreEqual(2, picked.Count, $"seed {seed}: the composite and the atom must both be pickable.");
                Assert.IsTrue(picked.Contains(composite), $"seed {seed}");
            }
        }

        private static ParameterDescriptor Entry(EntityParameter parameter, AffixKind affix, float weight = 10f) =>
            new(parameter, ModifierValueType.Flat, 10f, ModifierScope.Global) { Weight = weight, Affix = affix };
    }
}
