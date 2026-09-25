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

        public IReadOnlyList<IModifierDescriptor> GetGiftPool(EquipmentCategory itemCategory) =>
            itemDataProvider.GetEquipItemModifierPool(s_mythicPoolByCategory[itemCategory]);

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
        /// roll). Chance = data base × (1 + mastery mythic channel). The whole pool competes on weight alone:
        /// the gift occupies the item's OWN mythic slot, which stands outside the rarity's prefix/suffix
        /// count, so there is no slot family to draw for and no cap to check.</summary>
        private IReadOnlyList<string> TryRollGift(IEquipItem item)
        {
            if (rnd.RandFloat() > mastery.GetMythicGiftChance()) return [];

            var pool = itemDataProvider.GetEquipItemModifierPool(s_mythicPoolByCategory[item.EquipmentPiece.ConvertEquipmentPartToCategory()]);
            if (pool.Count == 0) return [];

            (var weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(pool);
            var picked = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);

            // The "+Min..Max sharpening levels" entry (present in every mark's pool) is an operation,
            // not a line: raise the cap from the current (full) level and run the STANDARD upgrade
            // path, so the base channel scales exactly like a manual sharpen. No InstanceIds to report.
            if (picked is UpgradeLevelsDescriptor extraLevels)
            {
                int rolled = rnd.RandIntRange(extraLevels.Min, extraLevels.Max);
                item.MaxUpdateLevel = item.UpdateLevel + rolled;
                item.Upgrade(rolled);
                return [];
            }

            // Mythic pool entries are marked Mythic in data; stamping it here too keeps the invariant even if
            // an entry forgets the marker — everything born on this path belongs to the item's mythic slot.
            // It rides the normal Affix channel, so the save round-trips it for free.
            var sink = new CollectingSink();
            materializer.Materialize(picked, sink, item.InstanceId);
            foreach (var entity in sink.Entities)
            {
                if (entity is SimpleModifier simple) simple.Affix = AffixKind.Mythic;
                item.AddAdditionalModifier(entity);
            }

            foreach (var context in sink.Contexts)
            {
                context.Affix = AffixKind.Mythic;
                item.AddAdditionalContextModifier(context);
            }

            // A gift can also be a grant (behaviour no line can express). It carries no affix stamp:
            // grants are not lines and render in the item's effect section, not among the rolls.
            foreach (var grant in sink.Grants) item.AddGrant(grant);

            return sink.Entities.Select(entity => entity.InstanceId)
                .Concat(sink.Contexts.Select(entry => entry.InstanceId))
                .ToList();
        }
    }
}
