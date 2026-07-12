namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Core.Results;
    using Godot;

    public class ItemUpgrader(RandomNumberGenerator rnd, ICraftingMastery mastery, IItemDataProvider itemDataProvider, ICraftingAdditiveProvider additives, IModifierMaterializer materializer) : IItemUpgrader
    {
        private const float P0 = 0.95f;
        private const float P5 = 0.70f;
        private const float P6 = 0.35f;
        private const float P9 = 0.15f;
        private const float P12 = 0.01f;

        private static readonly float s_r = Mathf.Pow(P12 / P9, 1.0f / 3.0f);

        public List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory) =>
            ScaleByRarity(itemDataProvider.GetUpgradeCost(itemCategory), itemRarity);

        private static int GetAmount(Rarity itemRarity) => itemRarity switch
        {
            Rarity.Common or Rarity.Uncommon => 1,
            Rarity.Rare => 2,
            Rarity.Epic => 3,
            Rarity.Legendary or Rarity.Unique or Rarity.Mythic => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(itemRarity), itemRarity, null)
        };

        public List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory) =>
            ScaleByRarity(itemDataProvider.GetRecraftCost(itemCategory), itemRarity);

        private static List<IRequirement> ScaleByRarity(IReadOnlyList<IRequirement> requirements, Rarity itemRarity)
        {
            int amount = GetAmount(itemRarity);
            return requirements.Select(IRequirement (req) => new Requirement(req.Type, req.Id, req.Amount + amount)).ToList();
        }

        public string? TryRecraftModifier(IEquipItem item, string modifierInstanceId, IReadOnlyCollection<string>? additiveResourceIds = null)
        {
            bool onItem = item.Modifiers.Any(modifier => modifier.InstanceId == modifierInstanceId)
                          || item.ContextModifiers.Any(entry => entry.InstanceId == modifierInstanceId);
            if (!onItem) return null;

            // Variant B allows duplicate lines, so the whole (already quality-scaled) pool joins the roll with no
            // value exclusion. Composites flatten to atomic parts so a reroll is 1-for-1. Additives join for THIS
            // operation only; the item's own pool never changes.
            var candidates = DescriptorOperations.Flatten(item.ModifiersPool.Concat(AdditivePools(additiveResourceIds))).ToList();
            if (candidates.Count == 0) return null; // nothing to roll — refuse instead of looping

            (var weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(candidates);
            item.RemoveAdditionalModifier(modifierInstanceId);

            var picked = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);
            var sink = new CollectingSink();
            materializer.Materialize(picked, sink, item.InstanceId); // pool descriptors are already quality-scaled
            foreach (var entity in sink.Entities) item.AddAdditionalModifier(entity);
            foreach (var context in sink.Contexts) item.AddAdditionalContextModifier(context);

            return sink.Entities.Select(entity => entity.InstanceId)
                .Concat(sink.Contexts.Select(entry => entry.InstanceId))
                .FirstOrDefault();
        }

        public ItemUpgradeResult TryUpgradeItem(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null)
        {
            if (item.UpdateLevel == item.MaxUpdateLevel) return ItemUpgradeResult.ReachedMaxLevel;

            var effects = AdditiveEffects(additiveResourceIds);
            float chances = Mathf.Clamp(GetChance(item.UpdateLevel) + effects.Sum(e => e.UpgradeChanceBonus), 0f, 1f);
            bool upgradeSucceed = rnd.Randf() <= chances;

            if (upgradeSucceed)
            {
                item.Upgrade();
                TryGrantExtraLevel(item, effects);
            }

            return upgradeSucceed ? ItemUpgradeResult.Success : ItemUpgradeResult.Failure;
        }

        /// <summary>Additives may gift a second level on success (never past the cap).</summary>
        private void TryGrantExtraLevel(IEquipItem item, List<CraftingAdditiveEffects> effects)
        {
            float extraChance = Mathf.Clamp(effects.Sum(e => e.ExtraUpgradeLevelChance), 0f, 1f);
            if (extraChance <= 0f || item.UpdateLevel >= item.MaxUpdateLevel) return;
            if (rnd.Randf() <= extraChance) item.Upgrade();
        }

        private List<CraftingAdditiveEffects> AdditiveEffects(IReadOnlyCollection<string>? resourceIds) =>
            resourceIds?.Select(additives.GetEffects).Where(effects => effects != null).Cast<CraftingAdditiveEffects>().ToList() ?? [];

        private IEnumerable<IModifierDescriptor> AdditivePools(IReadOnlyCollection<string>? resourceIds) =>
            AdditiveEffects(resourceIds)
                .Where(effects => !string.IsNullOrEmpty(effects.RecraftPoolId))
                .SelectMany(effects => itemDataProvider.GetEquipItemModifierPool(effects.RecraftPoolId!).Select(DescriptorOperations.FromModifier));

        private float GetChance(int level)
        {
            return true switch
            {
                _ when level <= 5 => Mathf.Lerp(P0, P5, level / 3.0f),
                _ when level <= 9 => Mathf.Lerp(P6, P9, (level - 5) / 3.0f),
                _ => P9 * Mathf.Pow(s_r, level - 9),
            };
        }
    }
}
