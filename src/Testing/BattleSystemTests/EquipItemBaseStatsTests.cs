namespace LastBreathTest.BattleSystemTests
{
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Save;

    /// <summary>
    /// The typed base channel (implicits rework, owner decision 2026-07-26): a piece's plain stats
    /// live as base rolls, not implicit lines. Locals amplify the base the way weapon damage always
    /// worked — (base × sharpening/ascension + flat) × (1 + inc) × (1 + multi) — and the owner
    /// receives exactly one flat per base stat. Implicits are reserved for special authored lines.
    /// </summary>
    [TestClass]
    public class EquipItemBaseStatsTests
    {
        [TestMethod]
        public void ResolvedContribution_FoldsLocalsAroundTheBase_Once()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f)]);
            item.AddAdditionalModifier(new SimpleModifier(EntityParameter.Evade, ModifierValueType.Flat, 100f, "test") { Scope = ModifierScope.Local });
            item.AddAdditionalModifier(new SimpleModifier(EntityParameter.Evade, ModifierValueType.Increase, 0.5f, "test") { Scope = ModifierScope.Local });

            var resolved = item.GetResolvedModifiers(EntityParameter.Evade).ToList();

            // ONE synthetic flat: (500 + 100) × 1.5 — the local lines must not also arrive raw.
            Assert.AreEqual(1, resolved.Count);
            Assert.AreEqual(ModifierValueType.Flat, resolved[0].ModifierValueType);
            Assert.AreEqual(900f, resolved[0].Value, 0.001f);
        }

        [TestMethod]
        public void GlobalLines_StayGlobal_NextToTheBase()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f)]);
            item.AddAdditionalModifier(new SimpleModifier(EntityParameter.Evade, ModifierValueType.Increase, 0.3f, "test"));

            var resolved = item.GetResolvedModifiers(EntityParameter.Evade).ToList();

            // The global increase multiplies the OWNER's total, so it must pass through untouched.
            Assert.AreEqual(2, resolved.Count);
            Assert.IsTrue(resolved.Any(m => m.ModifierValueType == ModifierValueType.Increase && System.Math.Abs(m.Value - 0.3f) < 0.001f));
            Assert.IsTrue(resolved.Any(m => m.ModifierValueType == ModifierValueType.Flat && System.Math.Abs(m.Value - 500f) < 0.001f));
        }

        [TestMethod]
        public void Sharpening_ScalesTheBase_LikeEveryLine()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f)]);
            item.Upgrade(4); // ×1.2

            (float baseValue, float localBonus) = item.GetBaseStatBreakdown(EntityParameter.Evade);
            Assert.AreEqual(600f, baseValue, 0.001f);
            Assert.AreEqual(0f, localBonus, 0.001f);
        }

        [TestMethod]
        public void BreakdownSplitsBaseFromLocalContribution()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f)]);
            item.AddAdditionalModifier(new SimpleModifier(EntityParameter.Evade, ModifierValueType.Increase, 0.5f, "test") { Scope = ModifierScope.Local });

            (float baseValue, float localBonus) = item.GetBaseStatBreakdown(EntityParameter.Evade);
            Assert.AreEqual(500f, baseValue, 0.001f);
            Assert.AreEqual(250f, localBonus, 0.001f); // the local % now has a base to amplify
        }

        [TestMethod]
        public void SaveRoundTrip_KeepsTheUnscaledBase()
        {
            var converter = new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null));
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f), new(EntityParameter.Health, 108f)]);
            item.Upgrade(2);

            var restored = converter.FromData(converter.ToData(item));

            Assert.AreEqual(500f, restored.BaseStats[EntityParameter.Evade], 0.001f);
            Assert.AreEqual(108f, restored.BaseStats[EntityParameter.Health], 0.001f);
            // Effective value re-derives from the restored level, not from a stored scaled number.
            Assert.AreEqual(500f * 1.1f, restored.GetBaseStatBreakdown(EntityParameter.Evade).Base, 0.001f);
        }

        [TestMethod]
        public void LegacySaveWithoutBaseStats_KeepsLocalImplicitFold()
        {
            // A pre-rework item: base stats live as LOCAL implicit lines. It must keep resolving
            // through the old fold — same numbers as before the rework, no migration required.
            var converter = new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null));
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetImplicits([
                new SimpleModifier(EntityParameter.Evade, ModifierValueType.Flat, 500f, "legacy") { Scope = ModifierScope.Local },
                new SimpleModifier(EntityParameter.Evade, ModifierValueType.Increase, 0.4f, "legacy") { Scope = ModifierScope.Local },
            ]);

            var restored = converter.FromData(converter.ToData(item));

            Assert.AreEqual(0, restored.BaseStats.Count);
            var resolved = restored.GetResolvedModifiers(EntityParameter.Evade).ToList();
            Assert.AreEqual(1, resolved.Count);
            Assert.AreEqual(700f, resolved[0].Value, 0.001f); // 500 × 1.4 — the legacy local fold
        }

        [TestMethod]
        public void SharpeningAndAscension_ScaleEveryWeaponBase()
        {
            // The owner's rule: the scales raise EVERY numeric value — damage and the crit pair alike.
            var weapon = new WeaponItem(WeaponType.Axe, Handedness.OneHanded, 100f, 0.05f, 1.5f, "Axe", []);
            weapon.Upgrade(2); // ×1.1
            weapon.AscensionMultiplier = 1.15f;

            Assert.AreEqual(100f * 1.1f * 1.15f, weapon.Damage, 0.001f);
            Assert.AreEqual(0.05f * 1.1f * 1.15f, weapon.GetStatBreakdown(EntityParameter.CriticalChance).Base, 0.0001f);
            Assert.AreEqual(1.5f * 1.1f * 1.15f, weapon.GetStatBreakdown(EntityParameter.CriticalDamage).Base, 0.001f);
        }

        [TestMethod]
        public void WeaponCritPair_FoldsLocalBucketAroundTheBase_LikeDamage()
        {
            // The Simple Axe case (owner report 2026-07-26): a LOCAL "+40% increased crit chance"
            // used to multiply an empty bucket and die. It must amplify the weapon's own base.
            var weapon = new WeaponItem(WeaponType.Axe, Handedness.OneHanded, 100f, 0.05f, 1.5f, "Axe", []);
            weapon.AddAdditionalModifier(new SimpleModifier(EntityParameter.CriticalChance, ModifierValueType.Increase, 0.4f, "test") { Scope = ModifierScope.Local });

            (float baseValue, float localBonus) = weapon.GetStatBreakdown(EntityParameter.CriticalChance);
            Assert.AreEqual(0.05f, baseValue, 0.0001f);
            Assert.AreEqual(0.05f * 0.4f, localBonus, 0.0001f); // 5% × (1 + 0.4) = 7%

            // The bucket is consumed by the fold — it must NOT also resolve into a synthetic flat.
            Assert.AreEqual(0, weapon.GetResolvedModifiers(EntityParameter.CriticalChance).Count());
        }

        [TestMethod]
        public void WeaponCritPair_GlobalLinesStayOutOfTheFold()
        {
            var weapon = new WeaponItem(WeaponType.Axe, Handedness.OneHanded, 100f, 0.05f, 1.5f, "Axe", []);
            weapon.AddAdditionalModifier(new SimpleModifier(EntityParameter.CriticalChance, ModifierValueType.Multiplicative, 0.126f, "test"));

            // A global "more crit" multiplies the OWNER's total through parameter math, not the item stat.
            Assert.AreEqual(0f, weapon.GetStatBreakdown(EntityParameter.CriticalChance).LocalBonus, 0.0001f);
            Assert.AreEqual(1, weapon.GetResolvedModifiers(EntityParameter.CriticalChance).Count());
        }

        [TestMethod]
        public void AffectedParameters_IncludeBaseStats()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Helm", []);
            item.SetBaseStats([new(EntityParameter.Evade, 500f)]);
            CollectionAssert.Contains(item.AffectedParameters.ToList(), EntityParameter.Evade);
        }
    }
}
