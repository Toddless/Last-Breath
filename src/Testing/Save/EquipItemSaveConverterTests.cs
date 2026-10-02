namespace LastBreathTest.Save
{
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Save;

    [TestClass]
    public class EquipItemSaveConverterTests
    {
        private readonly EquipItemSaveConverter _converter = new(new GrantFactory(() => null, () => null, () => null));

        [TestMethod]
        public void PlainItemRoundTripsRolledState()
        {
            var item = new EquipItem(EquipmentPiece.Body, "Iron_Chest", ["armor"]);
            item.SetImplicits([Modifier(EntityParameter.Armor, ModifierValueType.Flat, 50f)]);
            item.SetModifiers([
                Modifier(EntityParameter.Health, ModifierValueType.Flat, 120f),
                Modifier(EntityParameter.Evade, ModifierValueType.Increase, 0.15f)
            ]);
            item.SaveUsedResources(
                new Dictionary<string, int> { ["Crafting_Resource_Diamond"] = 3 },
                new Dictionary<string, int> { ["Crafting_Resource_Essence_Health"] = 1 });
            item.PowerMultiplier = 1.75f; // the item's caliber must survive the trip — the live reroll pool rescales by it
            item.RecraftCount = 4; // the growing recraft price must survive the trip
            item.Upgrade(3); // multiplier 1.15 on the base channel: restored Values must match, not just BaseValues
            item.Rarity = Rarity.Epic;

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.AreEqual("Iron_Chest", restored.Id);
            Assert.AreEqual(EquipmentPiece.Body, restored.EquipmentPiece);
            Assert.AreEqual(Rarity.Epic, restored.Rarity);
            Assert.AreEqual(3, restored.UpdateLevel);
            // The split must survive the round trip, not just the merged view.
            Assert.AreEqual(3, restored.UsedRequiredResources["Crafting_Resource_Diamond"]);
            Assert.AreEqual(1, restored.UsedOptionalResources["Crafting_Resource_Essence_Health"]);
            Assert.IsFalse(restored.UsedRequiredResources.ContainsKey("Crafting_Resource_Essence_Health"));
            Assert.AreEqual(3, restored.UsedResources["Crafting_Resource_Diamond"]);
            Assert.AreEqual(1, restored.UsedResources["Crafting_Resource_Essence_Health"]);
            Assert.AreEqual(1.75f, restored.PowerMultiplier, 0.001f);
            Assert.AreEqual(4, restored.RecraftCount);
            Assert.AreEqual(1, restored.Implicits.Count);
            Assert.AreEqual(50f * 1.15f, restored.Implicits[0].Value, 0.001f);
            Assert.AreEqual(2, restored.Modifiers.Count);
            var health = restored.Modifiers.First(m => m.EntityParameter == EntityParameter.Health);
            Assert.AreEqual(120f, health.Value, 0.001f); // rolled: sharpening does not reach it
            Assert.AreEqual(120f, health.BaseValue, 0.001f);
        }

        [TestMethod]
        public void WeaponRoundTripKeepsDamageMath()
        {
            var weapon = new WeaponItem(WeaponType.Sword, Handedness.OneHanded, 100f, 0.3f, 1.6f, "Iron_Sword", []);
            var local = Modifier(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0.2f);
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
        public void AscendedItemRoundTripsTheAscensionMultiplier()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            item.SetModifiers([Modifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f)]);
            item.Upgrade(item.MaxUpdateLevel); // multiplier 1.6 (12 levels × +5%)
            item.AscensionMultiplier = 1.15f;  // the ascension +15%, set before the seal like the ascender does
            Assert.IsTrue(item.TryAscend());

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.IsTrue(restored.IsSealed);
            Assert.AreEqual(1.15f, restored.AscensionMultiplier, 0.0001f);
            // The rolled channel survives the trip: ascension reaches it, sharpening does not.
            Assert.AreEqual(10f * 1.15f, restored.Modifiers.Single().Value, 0.001f);
        }

        [TestMethod]
        public void ContextLinesRoundTrip_FlatScaledByUpgrade_PercentUntouched()
        {
            var item = new EquipItem(EquipmentPiece.Weapon, "Bloodthirsty", []);
            item.SetContextImplicits([new ContextModifierEntry(ContextParameter.BleedDuration, ModifierValueType.Flat, 1f)]);
            item.SetContextModifiers([new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.15f)]);
            item.Upgrade(12); // multiplier 1.6 (+5%/level): whole-number knobs floor the scaled value

            var restored = _converter.FromData(_converter.ToData(item));

            Assert.AreEqual(1, restored.ContextImplicits.Count);
            var duration = restored.ContextImplicits[0];
            Assert.AreEqual(ContextParameter.BleedDuration, duration.Parameter);
            Assert.AreEqual(1f, duration.BaseValue, 0.001f);
            Assert.AreEqual(1, duration.WholeValue); // 1 * 1.6 floored
            Assert.AreEqual(1, restored.ContextModifiers.Count);
            // Only FLAT rides the sharpening scale: a percent line would otherwise stack a multiplier on a
            // multiplier, so it round-trips at exactly the value data wrote.
            Assert.AreEqual(0.15f, restored.ContextModifiers[0].Value, 0.001f);
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

        [TestMethod]
        public void RollProvenanceStampsRoundTrip()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Stamped", []);
            var stamped = Modifier(EntityParameter.Health, ModifierValueType.Flat, 15f);
            stamped.Affix = AffixKind.Prefix;
            stamped.GroupId = "group-1";
            stamped.RolledRange = new ValueRange(10f, 20f);
            item.SetModifiers([stamped, Modifier(EntityParameter.Armor, ModifierValueType.Flat, 5f)]);
            item.SetContextModifiers([
                new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.15f)
                {
                    Affix = AffixKind.Suffix,
                    GroupId = "group-2",
                    RolledRange = new ValueRange(0.1f, 0.2f),
                }
            ]);

            var restored = _converter.FromData(_converter.ToData(item));

            var line = (SimpleModifier)restored.Modifiers.First(m => m.EntityParameter == EntityParameter.Health);
            Assert.AreEqual(AffixKind.Prefix, line.Affix);
            Assert.AreEqual("group-1", line.GroupId);
            Assert.IsNotNull(line.RolledRange);
            Assert.AreEqual(10f, line.RolledRange.Value.Min, 0.001f);
            Assert.AreEqual(20f, line.RolledRange.Value.Max, 0.001f);
            // The unstamped line stays unstamped: pre-range content must not grow phantom provenance.
            var plain = (SimpleModifier)restored.Modifiers.First(m => m.EntityParameter == EntityParameter.Armor);
            Assert.AreEqual(AffixKind.None, plain.Affix);
            Assert.IsNull(plain.GroupId);
            Assert.IsNull(plain.RolledRange);
            var context = restored.ContextModifiers.Single();
            Assert.AreEqual(AffixKind.Suffix, context.Affix);
            Assert.AreEqual("group-2", context.GroupId);
            Assert.IsNotNull(context.RolledRange);
            Assert.AreEqual(0.1f, context.RolledRange.Value.Min, 0.001f);
            Assert.AreEqual(0.2f, context.RolledRange.Value.Max, 0.001f);
        }

        [TestMethod]
        public void LegacySaveWithoutPowerMultiplier_RestoresWithNeutralOne()
        {
            // Pre-live-pool saves carry no powerMultiplier (and may still carry a modifiersPool blob —
            // the deserializer must shrug it off): such items restore at the neutral caliber of 1.
            const string legacyJson = """
            {
                "kind": "equip",
                "id": "Legacy_Ring",
                "piece": "Ring",
                "tags": [],
                "rarity": "Rare",
                "modifiersPool": [{ "kind": "parameter", "parameter": "Accuracy", "min": 20.0, "max": 40.0, "weight": 100.0 }]
            }
            """;

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Data.SaveData.EquipItemSaveData>(legacyJson);

            Assert.IsNotNull(data);
            var restored = _converter.FromData(data);
            Assert.AreEqual("Legacy_Ring", restored.Id);
            Assert.AreEqual(1f, restored.PowerMultiplier, 0.001f);
            Assert.AreEqual(0, restored.RecraftCount); // pre-counter saves recraft at the base price
            Assert.AreEqual(1f, restored.AscensionMultiplier, 0.001f); // pre-ascension-rework saves read as neutral
        }

        [TestMethod]
        public void RestoreNeverRolls_TwoRestoresOfOneSnapshotAreIdentical()
        {
            var item = new EquipItem(EquipmentPiece.Body, "Snapshot", []);
            var ranged = Modifier(EntityParameter.Health, ModifierValueType.Flat, 17.3f);
            ranged.RolledRange = new ValueRange(10f, 20f);
            item.SetModifiers([ranged]);
            item.Upgrade(4);
            var data = _converter.ToData(item);

            var first = _converter.FromData(data);
            var second = _converter.FromData(data);

            // Stored results replay verbatim: a range in the DTO is provenance, never a reroll invitation.
            Assert.AreEqual(first.Modifiers.Single().BaseValue, second.Modifiers.Single().BaseValue, 0.0001f);
            Assert.AreEqual(first.Modifiers.Single().Value, second.Modifiers.Single().Value, 0.0001f);
            Assert.AreEqual(17.3f, first.Modifiers.Single().BaseValue, 0.0001f);
            Assert.AreEqual(first.UpdateLevel, second.UpdateLevel);
        }

        private static SimpleModifier Modifier(EntityParameter parameter, ModifierValueType type, float value) =>
            new(parameter, type, value, "test");
    }
}
