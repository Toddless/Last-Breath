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

    public class ItemUpgrader(RandomNumberGenerator rnd, ICraftingMastery mastery, IItemDataProvider itemDataProvider) : IItemUpgrader
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

        public IModifierInstance? TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers)
        {
            if (item.Modifiers.All(modifier => modifier.GetHashCode() != modifierToReroll)) return null;

            (List<WeightedObject<IModifier>> weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(modifiers.Concat(item.ModifiersPool));

            item.RemoveAdditionalModifier(modifierToReroll);
            IModifierInstance? modifier = null;
            while (modifier == null)
            {
                var newMod = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);

                if (item.Modifiers.Any(x => x.GetHashCode() == newMod.GetHashCode())) continue;

                modifier = ModifiersCreator.CreateModifierInstance(newMod.EntityParameter, newMod.ModifierValueType, newMod.BaseValue, item.InstanceId);
                item.AddAdditionalModifier(modifier);
            }

            return modifier;
        }

        public ItemUpgradeResult TryUpgradeItem(IEquipItem item)
        {
            if (item.UpdateLevel == item.MaxUpdateLevel) return ItemUpgradeResult.ReachedMaxLevel;

            float chances = GetChance(item.UpdateLevel);
            bool upgradeSucceed = rnd.Randf() <= chances;

            if (upgradeSucceed) item.Upgrade();

            return upgradeSucceed ? ItemUpgradeResult.Success : ItemUpgradeResult.Failure;
        }

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
