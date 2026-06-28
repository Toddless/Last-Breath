namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Core.Results;
    using Godot;
    using Utilities;

    public class ItemUpgrader(RandomNumberGenerator rnd, ICraftingMastery mastery) : IItemUpgrader
    {
        private const float P0 = 0.95f;
        private const float P5 = 0.70f;
        private const float P6 = 0.35f;
        private const float P9 = 0.15f;
        private const float P12 = 0.01f;

        private static readonly float s_r = Mathf.Pow(P12 / P9, 1.0f / 3.0f);


        private readonly Dictionary<EquipmentCategory, List<IRequirement>> _upgradeRequirements = new()
        {
            [EquipmentCategory.Weapon] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Weapon_Rune"),
            ],
            [EquipmentCategory.Armor] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Blacksmith_Rune"),
            ],
            [EquipmentCategory.Jewellery] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Jeweler_Rune")
            ]
        };

        private readonly Dictionary<EquipmentCategory, List<IRequirement>> _recraftRequirements = new()
        {
            [EquipmentCategory.Weapon] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Weapon_Dust")
            ],
            [EquipmentCategory.Jewellery] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Jewellery_Dust")
            ],
            [EquipmentCategory.Armor] =
            [
                new Requirement(RequirementType.Resource, "Upgrade_Resource_Armor_Dust")
            ]
        };


        public List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory)
        {
            var requirements = _upgradeRequirements[itemCategory].ToList();
            var newReq = new List<IRequirement>();
            int amount = GetAmount(itemRarity);
            requirements.ForEach(req => newReq.Add(new Requirement(req.Type, req.Id, req.Amount + amount)));
            return newReq;
        }

        private int GetAmount(Rarity itemRarity) => itemRarity switch
        {
            Rarity.Common or Rarity.Uncommon => 1,
            Rarity.Rare => 2,
            Rarity.Epic => 3,
            Rarity.Legendary or Rarity.Unique => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(itemRarity), itemRarity, null)
        };

        public List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory)
        {
            var requirements = _recraftRequirements[itemCategory].ToList();
            var newRequirements = new List<IRequirement>();
            requirements.ForEach(req => newRequirements.Add(new Requirement(req.Type, req.Id, req.Amount + GetAmount(itemRarity))));

            return newRequirements;
        }

        public IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers)
        {
            (List<WeightedObject<IModifier>> weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(modifiers.Concat(item.ModifiersPool));

            item.RemoveAdditionalModifier(modifierToReroll);
            IModifierInstance? modifier = null;
            while (modifier == null)
            {
                var newMod = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);

                if (item.Modifiers.Any(x => x.GetHashCode() == newMod.GetHashCode())) continue;

                modifier = ModifiersCreator.CreateModifierInstance(newMod.EntityParameter, newMod.ModifierValueType, newMod.BaseValue, item);
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
