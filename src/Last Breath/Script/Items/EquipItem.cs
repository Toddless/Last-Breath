namespace LastBreath.Script.Items
{
    using Godot;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    [Tool]
    [GlobalClass]
    public partial class EquipItem : Item, IEquipItem
    {
        protected const float From = 0.8f;
        protected const float To = 1.2f;

        private float _currentUpdateMultiplier = 1f;
        private int _maxUpdateLevel = 12;
        private int _currentUpdateLevel = 0;
        private HashSet<IItemModifier> _baseModifiers = [];
        private HashSet<IItemModifier> _additionalModifiers = [];
        private List<IMaterialModifier> _modifiersPool = [];
        private Dictionary<string, int> _usedResources = [];
        private ISkill? _skill;

        [Export] public EquipmentType EquipmentPart { get; set; }
        [Export] public int Quantity { get; set; }
        [Export] public AttributeType AttributeType { get; set; } = AttributeType.None;

        // change to editor compatible class
        public int UpdateLevel => _currentUpdateLevel;
        public int MaxUpdateLevel => _maxUpdateLevel;
        public ISkill? Skill => _skill;
        public IReadOnlyList<IItemModifier> AdditionalModifiers => [.. _additionalModifiers];
        public IReadOnlyList<IItemModifier> BaseModifiers => [.. _baseModifiers];
        public IReadOnlyList<IMaterialModifier> ModifiersPool => _modifiersPool;
        public IReadOnlyDictionary<string, int> UsedResources => _usedResources;

        public EquipItem()
        {

        }

        public EquipItem(EquipmentType part,
            string id,
            Texture2D icon,
            Texture2D fullImage,
            Rarity rarity,
            string[] tags,
            HashSet<IItemModifier> baseModifiers,
            HashSet<IItemModifier> addModifiers,
            AttributeType attributeType = default, int quantity = 1, int maxStackSize = 1, ISkill? skill = default) : base(id, rarity, icon, fullImage, maxStackSize, tags)
        {
            EquipmentPart = part;
            AttributeType = attributeType;
            Quantity = quantity;
            _baseModifiers = baseModifiers;
            _additionalModifiers = addModifiers;
            _skill = skill;
        }

        public void SetBaseModifiers(IEnumerable<IItemModifier> modifiers) => SetModifiers(_baseModifiers, modifiers);
        public void SetAdditionalModifiers(IEnumerable<IItemModifier> modifiers) => SetModifiers(_additionalModifiers, modifiers);
        public void SetSkill(ISkill skill) => _skill = skill;
        public void RemoveAdditionalModifier(int hash) => _additionalModifiers.RemoveWhere(m => m.GetHashCode() == hash);
        public void AddAdditionalModifier(IItemModifier modifier) => SetModifier(_additionalModifiers, modifier);

        public bool Upgrade(int upgradeLevel = 1)
        {
            if (UpdateLevel >= 12) return false;
            // I should change it later
            var canUpgrade = _maxUpdateLevel - UpdateLevel;
            int appliedUpgrade = Mathf.Min(upgradeLevel, canUpgrade);
            float upgradeAmount = appliedUpgrade / 10f;
            _currentUpdateMultiplier += upgradeAmount;
            _currentUpdateLevel += appliedUpgrade;
            UpdateModifiersValue();

            return true;
        }

        public bool Downgrade(int downgradeLevel = 1)
        {
            // cant downgrade
            if (UpdateLevel == 0) return false;
            float downgradeAmount = Mathf.Min(downgradeLevel, _maxUpdateLevel) / 10f;
            _currentUpdateMultiplier = Mathf.Max(1f, _currentUpdateMultiplier - downgradeAmount);
            _currentUpdateLevel = Mathf.Max(0, _currentUpdateLevel - downgradeLevel);
            UpdateModifiersValue();
            return true;
        }

        public void ReplaceAdditionalModifier(int hash, IItemModifier newModifier)
        {
            _additionalModifiers.RemoveWhere(mod => mod.GetHashCode() == hash);
            _additionalModifiers.Add(newModifier);
        }

        public void OnUnequip()
        {

        }

        public void SaveModifiersPool(IEnumerable<IMaterialModifier> modifiers) => _modifiersPool.AddRange(modifiers);
        public void SaveUsedResources(Dictionary<string, int> resources)
        {
            foreach (var res in resources)
            {
                _usedResources.TryAdd(res.Key, res.Value);
            }
        }
        private void SetModifiers(HashSet<IItemModifier> itemModifiers, IEnumerable<IItemModifier> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var modifier in newModifiers)
                SetModifier(itemModifiers, modifier);
        }

        private void SetModifier(HashSet<IItemModifier> itemModifiers, IItemModifier newModifier)
        {
            newModifier.Value = newModifier.BaseValue * _currentUpdateMultiplier;
            itemModifiers.Add(newModifier);
        }

        private void UpdateModifiersValue()
        {
            foreach (var modifier in _baseModifiers.Concat(_additionalModifiers))
                modifier.Value = modifier.BaseValue * _currentUpdateMultiplier;
        }
    }
}
