namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Core.Results;

    /// <summary>Ascension per the design reference: gated by crafting mastery, then — strictly BEFORE
    /// the seal — the extra sharpening roll, the flat "+15% to everything", the mythic gift, and only
    /// then the Mythic transition. All tuning (gate, roll bounds, bonus, gift chance) lives in the
    /// CraftingMastery catalog.</summary>
    public class ItemAscender(IRandomNumberGenerator rnd, IItemDataProvider itemDataProvider, IModifierMaterializer materializer, ICraftingMastery mastery) : IItemAscender
    {
        // A mark is bound to its category, so the gift pool follows the item's category 1:1.
        private static readonly Dictionary<EquipmentCategory, string> s_mythicPoolByCategory = new()
        {
            [EquipmentCategory.Weapon] = "Mythic_Weapon",
            [EquipmentCategory.Jewellery] = "Mythic_Jewellery",
            [EquipmentCategory.Armor] = "Mythic_Armor",
        };

        public bool CanAscend(IEquipItem item) =>
            mastery.IsAscensionUnlocked
            && item is IAscendable { IsAscendable: true } && item is { IsSealed: false, Rarity: Rarity.Legendary };

        public List<IRequirement> GetAscendResourceCost(EquipmentCategory itemCategory) =>
            itemDataProvider.GetAscendCost(itemCategory).ToList();

        public AscensionResult TryAscendItem(IEquipItem item)
        {
            if (!CanAscend(item) || item is not IAscendable ascendable) return new AscensionResult(false, []);

            // 1. "+15% to everything": line channels recompute through the item's AscensionMultiplier;
            // grant payloads (no recomputable base) scale once.
            item.AscensionMultiplier = 1f + mastery.AscensionStatBonus;
            item.ScaleGrantValues(1f + mastery.AscensionStatBonus);

            // 2. The gift must land BEFORE the seal: every Add*/Upgrade on a sealed item is a no-op.
            // Invariant: a stat gift appends rolled lines; a levels gift raises the cap and re-sharpens
            // to it (UpdateLevel == MaxUpdateLevel again) — IsAscendable stays true either way, so
            // TryAscend() cannot fail after CanAscend() passed above.
            var giftedIds = TryRollGift(item);

            // 3. The transition itself (Mythic + seal) is deliberately LAST.
            return new AscensionResult(ascendable.TryAscend(), giftedIds);
        }

        /// <summary>Rolls one weighted entry of the item category's mythic pool and mints fresh instances
        /// through the materializer — a composite entry lands as its atomic parts (same path as the recraft
        /// roll). Chance = data base × (1 + mastery mythic channel). The gift's slot family rolls 50/50
        /// prefix/suffix; a pool short on the chosen kind falls back to whatever it has.
        /// The rarity slot cap is deliberately NOT checked — the gift is the only legal fifth line.</summary>
        private IReadOnlyList<string> TryRollGift(IEquipItem item)
        {
            if (rnd.RandFloat() > mastery.GetMythicGiftChance()) return [];

            var pool = itemDataProvider.GetEquipItemModifierPool(s_mythicPoolByCategory[item.EquipmentPiece.ConvertEquipmentPartToCategory()]);
            if (pool.Count == 0) return [];

            var kind = rnd.RandFloat() < 0.5f ? AffixKind.Prefix : AffixKind.Suffix;
            var candidates = pool.Where(descriptor => descriptor.Affix == kind).ToList();
            if (candidates.Count == 0) candidates = pool.ToList();

            (var weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(candidates);
            var picked = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);

            // The "+Min..Max sharpening levels" entry (present in every mark's pool) is an operation,
            // not a line: raise the cap from the current (full) level and run the STANDARD upgrade
            // path, so every line scales exactly like a manual sharpen. No InstanceIds to report.
            if (picked is UpgradeLevelsDescriptor extraLevels)
            {
                int rolled = rnd.RandIntRange(extraLevels.Min, extraLevels.Max);
                item.MaxUpdateLevel = item.UpdateLevel + rolled;
                item.Upgrade(rolled);
                return [];
            }

            var sink = new CollectingSink();
            materializer.Materialize(picked, sink, item.InstanceId);
            foreach (var entity in sink.Entities) item.AddAdditionalModifier(entity);
            foreach (var context in sink.Contexts) item.AddAdditionalContextModifier(context);

            return sink.Entities.Select(entity => entity.InstanceId)
                .Concat(sink.Contexts.Select(entry => entry.InstanceId))
                .ToList();
        }
    }
}
