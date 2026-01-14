namespace Crafting.Source
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Results;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public class ItemUpgrader : IItemUpgrader
    {
        private const float BaseDoubleRoll = 0.05f;
        private const float BaseLuckyRollUpgrade = 0.01f;
        private const float BaseLuckyRollDowngrade = 0.01f;
        private const int BaseDoubleUpgradeAmount = 2;

        private float _criticalDobubleRoll = BaseDoubleRoll;
        private float _criticalLuckyRollUpgrade = BaseLuckyRollUpgrade;
        private float _criticalLuckyRollDowngrade = BaseLuckyRollDowngrade;
        private int _doubleUpgradeAmount = BaseDoubleUpgradeAmount;
        private readonly Dictionary<EquipmentCategory, List<IRequirement>> _upgradeRequiremens = new()
        {
            [EquipmentCategory.Weapon] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Weapon_Rune"),
            ],
            [EquipmentCategory.Armor] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Blacksmith_Rune"),
            ],
            [EquipmentCategory.Jewellery] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Jeweler_Rune")
            ]
        };

        private readonly Dictionary<EquipmentCategory, List<IRequirement>> _recraftRequirements = new()
        {
            [EquipmentCategory.Weapon] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Weapon_Dust")
            ],
            [EquipmentCategory.Jewellery] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Jewellery_Dust")
            ],
            [EquipmentCategory.Armor] =
            [
                new ResourceRequirement(RequirementType.ResourceAmount, "Upgrade_Resource_Armor_Dust")
            ]
        };

        private readonly RandomNumberGenerator _rnd;

        private ItemUpgradeMode _currentUpgradeMode = ItemUpgradeMode.None;

        public ItemUpgrader(RandomNumberGenerator rnd)
        {
            _rnd = rnd;
        }

        public List<IResourceRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory, ItemUpgradeMode mode)
        {
            _currentUpgradeMode = mode;
            var requirements = _upgradeRequiremens[itemCategory].Cast<IResourceRequirement>().ToList();
            var newRequ = new List<IResourceRequirement>();
            int amount = GetAmount(itemRarity, mode);
            requirements.ForEach(req => newRequ.Add(new ResourceRequirement(req.Type, req.ResourceId, req.Amount + amount)));
            return newRequ;
        }

        public List<IResourceRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory)
        {
            var requirements = _recraftRequirements[itemCategory].Cast<IResourceRequirement>().ToList();
            var newRequrements = new List<IResourceRequirement>();
            requirements.ForEach(req => newRequrements.Add(new ResourceRequirement(req.Type, req.ResourceId, req.Amount + (int)itemRarity)));

            return newRequrements;
        }

        public IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, IEntity? player = default)
        {
            var (WeightedObjects, TotalWeight) = WeightedRandomPicker.CalculateWeights(modifiers.Concat(item.ModifiersPool));

            item.RemoveAdditionalModifier(modifierToReroll);
            IModifierInstance? modifier = null;
            while (modifier == null)
            {
                var newMod = WeightedRandomPicker.PickRandom(WeightedObjects, TotalWeight, _rnd);

                if (newMod != null && !item.AdditionalModifiers.Any(x => x.GetHashCode() == newMod.GetHashCode()))
                {
                    modifier = ModifiersCreator.CreateModifierInstance(newMod.EntityParameter, newMod.ModifierType, ApplyPlayerMultiplier(newMod.BaseValue, player), item);
                    item.AddAdditionalModifier(modifier);
                }
            }
            return modifier;
        }

        public ItemUpgradeResult TryUpgradeItem(IEquipItem item)
        {
            if (item.UpdateLevel == item.MaxUpdateLevel) return ItemUpgradeResult.ReachedMaxLevel;

            if (_currentUpgradeMode == ItemUpgradeMode.None) return ItemUpgradeResult.Failure;

            var chances = UpgradeChances.GetChance(item.UpdateLevel);
            bool upgradeSucced = _rnd.Randf() <= chances;

            if (upgradeSucced)
            {
                switch (true)
                {
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Lucky && CheckRollIsCritical(_criticalLuckyRollUpgrade):
                        item.Upgrade(item.MaxUpdateLevel);
                        return ItemUpgradeResult.CriticalSuccess;
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Double:
                        item.Upgrade(_doubleUpgradeAmount);
                        return ItemUpgradeResult.Success;
                    default:
                        item.Upgrade();
                        return ItemUpgradeResult.Success;
                }
            }
            else
            {
                switch (true)
                {
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Double && CheckRollIsCritical(_criticalDobubleRoll):
                        item.Downgrade();
                        return ItemUpgradeResult.Failure;
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Lucky && CheckRollIsCritical(_criticalLuckyRollDowngrade):
                        item.Downgrade(item.MaxUpdateLevel);
                        return ItemUpgradeResult.CriticalFailure;
                    default:
                        return ItemUpgradeResult.Failure;
                }
            }
        }

        private bool CheckRollIsCritical(float criticalChance) => _rnd.Randf() <= criticalChance;
        private int GetAmount(Rarity itemRarity, ItemUpgradeMode mode) => (int)itemRarity + (mode == ItemUpgradeMode.None ? 0 : (int)mode + 1);
        private float ApplyPlayerMultiplier(float baseValue, IEntity? player = default) => baseValue * _rnd.RandfRange(0.95f, 1.2f);

        //public void SetCriticalDoubleRoll(float doubleRoll) => _criticalDobubleRoll = doubleRoll;
        //public void SetCriticalLuckyRollUpgrade(float luckyRoll) => _criticalLuckyRollUpgrade = luckyRoll;
        //public void SetCriticalLuckyRollDowngrde(float luckyRoll) => _criticalLuckyRollDowngrade = luckyRoll;
        //public void SetDoubleUpgradeAmount(int amount) => _doubleUpgradeAmount = amount;
        //public List<IRequirement> GetUpgradeResourceRequirements(string itemId) => [];
        //public List<IRequirement> GetRecraftResourceRequirements(string id) => [];
    }
}
