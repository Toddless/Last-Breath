namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Save;

    [TestClass]
    public class EquipItemSaveConverterTests
    {
        private readonly EquipItemSaveConverter _converter = new(() => null);

        [TestMethod]
        public void PlainItemRoundTripsRolledState()
        {
            var item = new EquipItem(EquipmentPiece.Body, "Iron_Chest", ["armor"]);
            item.SetImplicits([Modifier(EntityParameter.Armor, ModifierValueType.Flat, 50f)]);
            item.SetModifiers([
                Modifier(EntityParameter.Health, ModifierValueType.Flat, 120f),
                Modifier(EntityParameter.Evade, ModifierValueType.Increase, 0.15f)
            ]);
            item.SaveModifiersPool([Modifier(EntityParameter.Accuracy, ModifierValueType.Flat, 30f)]);
            item.SaveUsedResources(new Dictionary<string, int> { ["Crafting_Resource_Diamond"] = 3 });
            item.Upgrade(3); // multiplier 1.3: restored Values must match, not just BaseValues
            item.Rarity = Rarity.Epic;

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.AreEqual("Iron_Chest", restored.Id);
            Assert.AreEqual(EquipmentPiece.Body, restored.EquipmentPiece);
            Assert.AreEqual(Rarity.Epic, restored.Rarity);
            Assert.AreEqual(3, restored.UpdateLevel);
            Assert.AreEqual(3, restored.UsedResources["Crafting_Resource_Diamond"]);
            Assert.AreEqual(1, restored.ModifiersPool.Count);
            Assert.AreEqual(1, restored.Implicits.Count);
            Assert.AreEqual(50f * 1.3f, restored.Implicits[0].Value, 0.001f);
            Assert.AreEqual(2, restored.Modifiers.Count);
            var health = restored.Modifiers.First(m => m.EntityParameter == EntityParameter.Health);
            Assert.AreEqual(120f * 1.3f, health.Value, 0.001f);
            Assert.AreEqual(120f, health.BaseValue, 0.001f);
        }

        [TestMethod]
        public void WeaponRoundTripKeepsDamageMath()
        {
            var weapon = new WeaponItem(WeaponType.Sword, Handedness.OneHanded, 100f, 0.3f, 1.6f, "Iron_Sword", []);
            var local = Modifier(EntityParameter.Damage, ModifierValueType.Increase, 0.2f);
            local.Scope = ModifierScope.Local;
            weapon.SetModifiers([local]);
            weapon.Upgrade(2);

            var restored = (WeaponItem)_converter.FromData(_converter.ToData(weapon));

            Assert.AreEqual(WeaponType.Sword, restored.WeaponType);
            Assert.AreEqual(Handedness.OneHanded, restored.Handedness);
            Assert.AreEqual(weapon.CriticalChance, restored.CriticalChance, 0.001f);
            Assert.AreEqual(weapon.CriticalDamage, restored.CriticalDamage, 0.001f);
            Assert.AreEqual(weapon.Damage, restored.Damage, 0.001f);
        }

        [TestMethod]
        public void SealedMythicItemStaysSealed()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            item.SetModifiers([Modifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f)]);
            item.Upgrade(item.MaxUpdateLevel);
            Assert.IsTrue(item.TryAscend());

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.IsTrue(restored.IsSealed);
            Assert.AreEqual(Rarity.Mythic, restored.Rarity);
            Assert.AreEqual(item.MaxUpdateLevel, restored.UpdateLevel);
            Assert.AreEqual(1, restored.Modifiers.Count); // seal restored AFTER content, not before
        }

        [TestMethod]
        public void ContextLinesRoundTripScaledByUpgrade()
        {
            var item = new EquipItem(EquipmentPiece.Weapon, "Bloodthirsty", []);
            item.SetContextImplicits([new ContextModifierEntry(ContextParameter.BleedDuration, ModifierValueType.Flat, 1f)]);
            item.SetContextModifiers([new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.15f)]);
            item.Upgrade(12); // multiplier 2.2: whole-number knobs floor the scaled value

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.AreEqual(1, restored.ContextImplicits.Count);
            var duration = restored.ContextImplicits[0];
            Assert.AreEqual(ContextParameter.BleedDuration, duration.Parameter);
            Assert.AreEqual(1f, duration.BaseValue, 0.001f);
            Assert.AreEqual(2, duration.WholeValue); // 1 * 2.2 floored
            Assert.AreEqual(1, restored.ContextModifiers.Count);
            Assert.AreEqual(0.15f * 2.2f, restored.ContextModifiers[0].Value, 0.001f);
        }

        [TestMethod]
        public void GrantsRoundTripWithTheirKind()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            item.AddGrant(new ModifierGrant("grant_str", [Modifier(EntityParameter.Strength, ModifierValueType.Flat, 5f)]));
            item.AddGrant(new PassiveSkillGrant("grant_skill", "Skill_Regeneration", new Dictionary<string, float> { ["percent"] = 0.05f }, () => null));

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.AreEqual(2, restored.Grants.Count);
            var modifierGrant = (ModifierGrant)restored.Grants.First(g => g.Id == "grant_str");
            Assert.AreEqual(1, modifierGrant.Modifiers.Count);
            Assert.AreEqual(5f, modifierGrant.Modifiers[0].BaseValue, 0.001f);
            var skillGrant = (PassiveSkillGrant)restored.Grants.First(g => g.Id == "grant_skill");
            Assert.AreEqual("Skill_Regeneration", skillGrant.SkillId);
            Assert.AreEqual(0.05f, skillGrant.Properties["percent"], 0.001f);
        }

        private static SimpleModifier Modifier(EntityParameter parameter, ModifierValueType type, float value) =>
            new(parameter, type, value, "test");
    }
}
