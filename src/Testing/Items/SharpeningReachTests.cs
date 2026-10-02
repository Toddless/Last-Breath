namespace LastBreathTest.Items
{
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;

    /// <summary>
    /// How far each scale reaches. Sharpening raises the item's OWN base channel — the typed base stats,
    /// the weapon triple and the flat implicits — and stops there; the ascension +15% raises that channel
    /// and the rolled one alike. A rolled affix was already scaled once by the item's caliber when it was
    /// rolled, which is why sharpening no longer feeds it a second time.
    /// Numbers are the shipped Weapon_Simple_Sword / Body_Stoneheart on mid rolls, PowerMultiplier 1.
    /// </summary>
    [TestClass]
    public class SharpeningReachTests
    {
        private const float FullSharpening = 1.6f;   // +12 levels × 5%
        private const float Ascension = 1.15f;

        [TestMethod]
        public void Sword_FullySharpened_GrowsTheWeaponTripleAndLeavesTheAffixes()
        {
            var sword = SimpleSword();
            sword.Upgrade(12);

            // (220 × 1.60) + 150 — the base grew, the rolled local flat did not.
            Assert.AreEqual(502f, sword.Damage, 0.01f);
            Assert.AreEqual(225f, AddedFire(sword), 0.01f);
            Assert.AreEqual(0.1248f, Effective(sword.GetStatBreakdown(EntityParameter.CriticalChance)), 0.0001f);
            Assert.AreEqual(2.985f, CriticalDamage(sword), 0.001f);
        }

        [TestMethod]
        public void Sword_SharpenedAndAscended_GrowsBothChannelsByDifferentScales()
        {
            var sword = SimpleSword();
            sword.Upgrade(12);
            sword.AscensionMultiplier = Ascension;

            // (220 × 1.84) + (150 × 1.15): the base rides both scales, the affix only the ascension.
            Assert.AreEqual(577.3f, sword.Damage, 0.01f);
            Assert.AreEqual(258.75f, AddedFire(sword), 0.01f);
            Assert.AreEqual(0.14352f, Effective(sword.GetStatBreakdown(EntityParameter.CriticalChance)), 0.0001f);
            Assert.AreEqual(3.43275f, CriticalDamage(sword), 0.001f);
        }

        [TestMethod]
        public void BodyArmor_FullySharpened_GrowsTheBaseStatsAndLeavesTheAffixes()
        {
            var body = Stoneheart();
            body.Upgrade(12);

            Assert.AreEqual(624f, Effective(body.GetBaseStatBreakdown(EntityParameter.Health)), 0.01f);
            Assert.AreEqual(0.15f, Resolved(body, EntityParameter.FireResistance), 0.0001f);
        }

        [TestMethod]
        public void BodyArmor_SharpenedAndAscended_GrowsBothChannelsByDifferentScales()
        {
            var body = Stoneheart();
            body.Upgrade(12);
            body.AscensionMultiplier = Ascension;

            Assert.AreEqual(717.6f, Effective(body.GetBaseStatBreakdown(EntityParameter.Health)), 0.01f);
            Assert.AreEqual(0.1725f, Resolved(body, EntityParameter.FireResistance), 0.0001f);
        }

        [TestMethod]
        public void ImplicitOnlyItem_StillSharpens()
        {
            // Unique/Mythic blueprints carry authored implicits and no base stats: sharpening has to
            // reach them, or the whole operation would do nothing on those pieces.
            var item = new EquipItem(EquipmentPiece.Gloves, "Gloves_Flawlessness", []);
            item.SetImplicits([Line(EntityParameter.Accuracy, ModifierValueType.Flat, 100f)]);
            item.SetContextImplicits([new ContextModifierEntry(ContextParameter.BleedDuration, ModifierValueType.Flat, 2f)]);

            item.Upgrade(12);

            Assert.AreEqual(100f * FullSharpening, item.Implicits[0].Value, 0.01f);
            Assert.AreEqual(2f * FullSharpening, item.ContextImplicits[0].Value, 0.01f);
        }

        [TestMethod]
        public void PercentLines_MoveWithNeitherScale()
        {
            // A percent line multiplies a value the scales have already raised: scaling it too would
            // stack a multiplier on a multiplier, in either channel.
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.SetImplicits([Line(EntityParameter.Armor, ModifierValueType.Increase, 0.4f)]);
            item.SetModifiers([Line(EntityParameter.Health, ModifierValueType.Multiplicative, 0.25f)]);

            item.Upgrade(12);
            item.AscensionMultiplier = Ascension;

            Assert.AreEqual(0.4f, item.Implicits[0].Value, 0.0001f);
            Assert.AreEqual(0.25f, item.Modifiers[0].Value, 0.0001f);
        }

        [TestMethod]
        public void FlagLines_StayAtOne_InBothChannels()
        {
            var item = new EquipItem(EquipmentPiece.Weapon, "Cutter", []);
            item.SetContextImplicits([new ContextModifierEntry(ContextParameter.AttacksIgnoreResistances, ModifierValueType.Flag, 1f)]);
            item.SetContextModifiers([new ContextModifierEntry(ContextParameter.AttacksIgnoreResistances, ModifierValueType.Flag, 1f)]);

            item.Upgrade(12);
            item.AscensionMultiplier = Ascension;

            Assert.AreEqual(1f, item.ContextImplicits[0].Value, 0.0001f);
            Assert.AreEqual(1f, item.ContextModifiers[0].Value, 0.0001f);
        }

        [TestMethod]
        public void RecraftedLines_LandInTheRolledChannel()
        {
            // The reroll path: a line inserted onto an already sharpened item must not inherit the
            // sharpening, and must still answer to a later ascension.
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.Upgrade(12);

            var line = Line(EntityParameter.Health, ModifierValueType.Flat, 200f);
            var entry = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Flat, 10f);
            item.InsertAdditionalModifier(0, line);
            item.InsertAdditionalContextModifier(0, entry);

            Assert.AreEqual(200f, line.Value, 0.01f);
            Assert.AreEqual(10f, entry.Value, 0.01f);

            item.AscensionMultiplier = Ascension;

            Assert.AreEqual(200f * Ascension, line.Value, 0.01f);
            Assert.AreEqual(10f * Ascension, entry.Value, 0.01f);
        }

        [TestMethod]
        public void CompositeParts_FollowTheChannelTheyWereAddedTo()
        {
            // A composite is expanded into plain parts on add — each part belongs to the channel that
            // received the composite, not to the composite's own bookkeeping.
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.SetImplicits([Composite(Line(EntityParameter.Armor, ModifierValueType.Flat, 100f), Line(EntityParameter.Evade, ModifierValueType.Flat, 60f))]);
            item.SetModifiers([Composite(Line(EntityParameter.Health, ModifierValueType.Flat, 100f), Line(EntityParameter.Mana, ModifierValueType.Flat, 60f))]);

            item.Upgrade(12);

            Assert.AreEqual(100f * FullSharpening, item.Implicits[0].Value, 0.01f);
            Assert.AreEqual(60f * FullSharpening, item.Implicits[1].Value, 0.01f);
            Assert.AreEqual(100f, item.Modifiers[0].Value, 0.01f);
            Assert.AreEqual(60f, item.Modifiers[1].Value, 0.01f);
        }

        [TestMethod]
        public void ConditionalRolledLine_KeepsItsChannel()
        {
            // A predicate decides whether the line counts, not which scale reaches its value.
            var line = Line(EntityParameter.Health, ModifierValueType.Flat, 200f);
            line.ConditionId = "Condition_LowHealth";
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.SetModifiers([line]);

            item.Upgrade(12);
            item.AscensionMultiplier = Ascension;

            Assert.AreEqual(200f * Ascension, item.Modifiers[0].Value, 0.01f);
        }

        [TestMethod]
        public void SharpeningAndUnsharpening_LeaveTheRolledChannelExactlyWhereItWas()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.SetBaseStats([new(EntityParameter.Armor, 500f)]);
            item.SetModifiers([Line(EntityParameter.Health, ModifierValueType.Flat, 200f)]);

            item.Upgrade(12);
            Assert.AreEqual(200f, item.Modifiers[0].Value, 0.01f);
            Assert.AreEqual(500f * FullSharpening, item.GetBaseStatBreakdown(EntityParameter.Armor).Base, 0.01f);

            item.Downgrade(12);
            Assert.AreEqual(200f, item.Modifiers[0].Value, 0.01f);
            Assert.AreEqual(500f, item.GetBaseStatBreakdown(EntityParameter.Armor).Base, 0.01f);
        }

        [TestMethod]
        public void SealedItem_MovesForNeitherScale()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            item.SetImplicits([Line(EntityParameter.Armor, ModifierValueType.Flat, 100f)]);
            item.SetModifiers([Line(EntityParameter.Health, ModifierValueType.Flat, 200f)]);
            item.Upgrade(item.MaxUpdateLevel);
            Assert.IsTrue(item.TryAscend());

            Assert.IsFalse(item.Upgrade());
            item.AscensionMultiplier = 2f;

            Assert.AreEqual(1f, item.AscensionMultiplier, 0.0001f);
            Assert.AreEqual(100f * FullSharpening, item.Implicits[0].Value, 0.01f);
            Assert.AreEqual(200f, item.Modifiers[0].Value, 0.01f);
        }

        /// <summary>Weapon_Simple_Sword on mid rolls: base 220 / 0.06 / 1.6, prefixes +150 physical and
        /// +225 fire (both local), suffixes +30% local crit chance and +0.425 global crit damage.</summary>
        private static WeaponItem SimpleSword()
        {
            var sword = new WeaponItem(WeaponType.Sword, Handedness.OneHanded, 220f, 0.06f, 1.6f, "Weapon_Simple_Sword", []);
            sword.SetModifiers([
                Local(Line(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 150f)),
                Local(Line(EntityParameter.FireDamage, ModifierValueType.Flat, 225f)),
                Local(Line(EntityParameter.CriticalChance, ModifierValueType.Increase, 0.3f)),
                Line(EntityParameter.CriticalDamage, ModifierValueType.Flat, 0.425f),
            ]);
            return sword;
        }

        /// <summary>Body_Stoneheart on mid rolls: base Health 265 / Armor 525, prefix +200 local health,
        /// suffix +15% global fire resistance.</summary>
        private static EquipItem Stoneheart()
        {
            var body = new EquipItem(EquipmentPiece.Body, "Body_Stoneheart", []);
            body.SetBaseStats([new(EntityParameter.Health, 265f), new(EntityParameter.Armor, 525f)]);
            body.SetModifiers([
                Local(Line(EntityParameter.Health, ModifierValueType.Flat, 200f)),
                Line(EntityParameter.FireResistance, ModifierValueType.Flat, 0.15f),
            ]);
            return body;
        }

        private static SimpleModifier Line(EntityParameter parameter, ModifierValueType type, float value) =>
            new(parameter, type, value, "test");

        private static SimpleModifier Local(SimpleModifier line)
        {
            line.Scope = ModifierScope.Local;
            return line;
        }

        private static CompositeModifier Composite(params IModifierInstance[] parts) => new(1f, parts, "test");

        private static float Effective((float Base, float LocalBonus) breakdown) => breakdown.Base + breakdown.LocalBonus;

        /// <summary>The single flat a parameter without its own base hands the owner (added elemental
        /// damage, a resistance suffix): the local bucket folded, or the global line as it stands.</summary>
        private static float Resolved(IEquipItem item, EntityParameter parameter) =>
            item.GetResolvedModifiers(parameter).Sum(modifier => modifier.Value);

        private static float AddedFire(IEquipItem item) => Resolved(item, EntityParameter.FireDamage);

        /// <summary>The weapon's own scaled crit damage plus the global line stacked on it — the number
        /// the wearer ends up with.</summary>
        private static float CriticalDamage(WeaponItem sword) =>
            Effective(sword.GetStatBreakdown(EntityParameter.CriticalDamage)) + Resolved(sword, EntityParameter.CriticalDamage);
    }
}
