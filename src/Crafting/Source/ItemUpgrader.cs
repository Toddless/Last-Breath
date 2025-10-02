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
    using Crafting.TestResources;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemUpgrader
    {
        private const string UpgradeData = "uid://br3am4hnn5iqg";
        private const string RecraftData = "uid://cg33ax8kmy3gx";
        private const float BaseDoubleRoll = 0.05f;
        private const float BaseLuckyRollUpgrade = 0.01f;
        private const float BaseLuckyRollDowngrade = 0.01f;
        private const int BaseDoubleUpgradeAmount = 2;

        private float _criticalDobubleRoll = BaseDoubleRoll;
        private float _criticalLuckyRollUpgrade = BaseLuckyRollUpgrade;
        private float _criticalLuckyRollDowngrade = BaseLuckyRollDowngrade;
        private int _doubleUpgradeAmount = BaseDoubleUpgradeAmount;

        private readonly RandomNumberGenerator _rnd;
        private Dictionary<string, List<IResourceRequirement>> _upgradeRequirements = [];
        private Dictionary<string, List<IResourceRequirement>> _recraftRequirements = [];

        private ItemUpgradeMode _currentUpgradeMode = ItemUpgradeMode.None;

        public ItemUpgrader()
        {
            _rnd = new RandomNumberGenerator();
            _rnd.Randomize();
            LoadData();
        }

        public List<IResourceRequirement> SetCost(string itemId, ItemUpgradeMode mode)
        {
            if (!_upgradeRequirements.TryGetValue(itemId, out var reqs))
            {
                Tracker.TrackNotFound($"Requirements: {itemId}", this);
                return [];
            }
            _currentUpgradeMode = mode;
            switch (mode)
            {
                case ItemUpgradeMode.Normal:
                    return reqs;
                case ItemUpgradeMode.Double:
                    var doubl = new List<IResourceRequirement>();
                    foreach (var req in reqs) doubl.Add(new ResourceRequirement() { ResourceId = req.ResourceId, Amount = req.Amount * 2 });
                    return doubl;
                case ItemUpgradeMode.Lucky:
                    var lucky = new List<IResourceRequirement>();
                    foreach (var req in reqs) lucky.Add(new ResourceRequirement() { ResourceId = req.ResourceId, Amount = req.Amount * 5 });
                    return lucky;
                default:
                    return [];
            }
        }

        // just flat percent or should i calculate it here??
        public void SetCriticalDoubleRoll(float doubleRoll) => _criticalDobubleRoll = doubleRoll;
        public void SetCriticalLuckyRollUpgrade(float luckyRoll) => _criticalLuckyRollUpgrade = luckyRoll;
        public void SetCriticalLuckyRollDowngrde(float luckyRoll) => _criticalLuckyRollDowngrade = luckyRoll;
        public void SetDoubleUpgradeAmount(int amount) => _doubleUpgradeAmount = amount;
        public List<IResourceRequirement> GetUpgradeResourceRequirements(string itemId) => _upgradeRequirements.GetValueOrDefault(itemId) ?? [];
        public List<IResourceRequirement> GetRecraftResourceRequirements(string id) => _recraftRequirements.GetValueOrDefault(id) ?? [];


        public IItemModifier TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, ICharacter? player = default)
        {
            var (WeightedObjects, TotalWeight) = WeightedRandomPicker.CalculateWeights(modifiers);

            item.RemoveAdditionalModifier(modifierToReroll);
            IItemModifier? modifier = null;
            while (modifier == null)
            {
                var newMod = WeightedRandomPicker.PickRandom(WeightedObjects, TotalWeight, _rnd);

                if (newMod != null && !item.AdditionalModifiers.Any(x => x.GetHashCode() == newMod.GetHashCode()))
                {
                    modifier = ModifiersCreator.CreateModifier(newMod.Parameter, newMod.ModifierType, ApplyPlayerMultiplier(newMod.BaseValue, player), item);
                    item.AddAdditionalModifier(modifier);
                }
            }

            return modifier;
        }


        public IResult<ItemUpgradeResult> TryUpgradeItem(IEquipItem item)
        {
            if (item.UpdateLevel == item.MaxUpdateLevel) return Result<ItemUpgradeResult>.Failure(ItemUpgradeResult.ReachedMaxLevel);

            if (_currentUpgradeMode == ItemUpgradeMode.None) return Result<ItemUpgradeResult>.Failure(ItemUpgradeResult.UpgradeModeNotSet);

            using var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            var chances = UpgradeChances.GetChance(item.UpdateLevel);
            bool upgradeSucced = rnd.Randf() <= chances;

            if (upgradeSucced)
            {
                switch (true)
                {
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Lucky && CheckRollIsCritical(_criticalLuckyRollUpgrade, rnd):
                        item.Upgrade(item.MaxUpdateLevel);
                        return Result<ItemUpgradeResult>.Success(ItemUpgradeResult.CriticalSuccess);
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Double:
                        item.Upgrade(_doubleUpgradeAmount);
                        return Result<ItemUpgradeResult>.Success();
                    default:
                        item.Upgrade();
                        return Result<ItemUpgradeResult>.Success();
                }
            }
            else
            {
                switch (true)
                {
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Double && CheckRollIsCritical(_criticalDobubleRoll, rnd):
                        item.Downgrade();
                        return Result<ItemUpgradeResult>.Failure(ItemUpgradeResult.Failure);
                    case var _ when _currentUpgradeMode == ItemUpgradeMode.Lucky && CheckRollIsCritical(_criticalLuckyRollDowngrade, rnd):
                        item.Downgrade(item.MaxUpdateLevel);
                        return Result<ItemUpgradeResult>.Failure(ItemUpgradeResult.CriticalFailure);
                    default:
                        return Result<ItemUpgradeResult>.Failure(ItemUpgradeResult.Failure);
                }
            }
        }

        private bool CheckRollIsCritical(float criticalChance, RandomNumberGenerator rnd) => rnd.Randf() <= criticalChance;


        private float ApplyPlayerMultiplier(float baseValue, ICharacter? player = default)
        {
            if (player != null)
            {

            }

            return baseValue * _rnd.RandfRange(0.95f, 1.2f);
        }

        private void LoadData()
        {
            var data = ResourceLoader.Load<UpgradeRequirement>(UpgradeData);

            foreach (var req in data.UpgradeRequrements)
                _upgradeRequirements.Add(req.Key, [.. req.Requirements]);

            var recraft = ResourceLoader.Load<UpgradeRequirement>(RecraftData);
            foreach (var req in recraft.UpgradeRequrements)
                _recraftRequirements.Add(req.Key, [.. req.Requirements]);
        }
    }
}
