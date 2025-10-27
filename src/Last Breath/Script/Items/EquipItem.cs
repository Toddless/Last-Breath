namespace LastBreath.Script.Items
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    [Tool]
    [GlobalClass]
    public partial class EquipItem : Resource, IEquipItem, IItem
    {
        private float _currentUpdateMultiplier = 1f;
        private int _maxUpdateLevel = 12;
        private int _currentUpdateLevel = 0;
        private HashSet<IModifierInstance> _baseModifiers = [];
        private HashSet<IModifierInstance> _additionalModifiers = [];
        private List<IMaterialModifier> _modifiersPool = [];
        private Dictionary<string, int> _usedResources = [];
        private ICharacter? _owner;

        [Export] public EquipmentType EquipmentPart { get; set; }
        [Export] public string Id { get; set; } = string.Empty;
        [Export] public int Quantity { get; set; }
        [Export] public int MaxStackSize { get; set; }
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Rarity Rarity { get; set; }
        [Export] public string[] Tags { get; set; } = [];
        [Export] public AttributeType AttributeType { get; set; } = AttributeType.None;
        public Texture2D? FullImage { get; set; }
        public ISkill? Skill { get; private set; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Description => Lokalizator.Lokalize(Id + "_Description");
        public string DisplayName => Lokalizator.Lokalize(Id);
        public int UpdateLevel => _currentUpdateLevel;
        public int MaxUpdateLevel => _maxUpdateLevel;
        public IReadOnlyList<IModifierInstance> AdditionalModifiers => [.. _additionalModifiers];
        public IReadOnlyList<IModifierInstance> BaseModifiers => [.. _baseModifiers];
        public IReadOnlyList<IMaterialModifier> ModifiersPool => _modifiersPool;
        public IReadOnlyDictionary<string, int> UsedResources => _usedResources;

        public bool IsAscendable => Rarity == Rarity.Legendary && UpdateLevel >= 12;

        public EquipItem()
        {
            
        }

        public EquipItem(EquipmentType part,
            string id,
            Texture2D icon,
            Rarity rarity,
            string[] tags,
            HashSet<IModifierInstance> baseModifiers,
            HashSet<IModifierInstance> addModifiers,
            AttributeType attributeType = default, int quantity = 1, int maxStackSize = 1, ISkill? skill = default)
        {
            EquipmentPart = part;
            Id = id;
            Icon = icon;
            Rarity = rarity;
            Tags = tags;
            AttributeType = attributeType;
            Quantity = quantity;
            MaxStackSize = maxStackSize;
            _baseModifiers = baseModifiers;
            _additionalModifiers = addModifiers;
            Skill = skill;
        }

        public T Copy<T>()
        {
            var duplicate = (IEquipItem)DuplicateDeep();
            return (T)duplicate;
        }

        public void SetBaseModifiers(IEnumerable<IModifierInstance> modifiers) => SetModifiers(_baseModifiers, modifiers);
        public void SetAdditionalModifiers(IEnumerable<IModifierInstance> modifiers) => SetModifiers(_additionalModifiers, modifiers);
        public void SetSkill(ISkill skill) => Skill = skill;
        public void RemoveAdditionalModifier(int hash) => _additionalModifiers.RemoveWhere(m => m.GetHashCode() == hash);
        public void AddAdditionalModifier(IModifierInstance modifier) => SetModifier(_additionalModifiers, modifier);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

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

        public void ReplaceAdditionalModifier(int hash, IModifierInstance newModifier)
        {
            _additionalModifiers.RemoveWhere(mod => mod.GetHashCode() == hash);
            _additionalModifiers.Add(newModifier);
        }

        public void OnUnequip()
        {
            foreach (var mod in _baseModifiers.Concat(_additionalModifiers))
                _owner?.Modifiers.RemovePermanentModifier(mod);
            _owner = null;
        }

        public void OnEquip(ICharacter owner)
        {
            _owner = owner;
            var modifiers = _baseModifiers.Concat(_additionalModifiers);
            foreach (var mod in _baseModifiers.Concat(_additionalModifiers))
                _owner.Modifiers.AddPermanentModifier(mod);
            Skill?.Attach(_owner);
        }
        public void SaveModifiersPool(IEnumerable<IMaterialModifier> modifiers) => _modifiersPool.AddRange(modifiers);
        public void SaveUsedResources(Dictionary<string, int> resources)
        {
            foreach (var res in resources)
                _usedResources.TryAdd(res.Key, res.Value);
        }

        public bool TryAscend()
        {
            return false;
        }

        private void SetModifiers(HashSet<IModifierInstance> itemModifiers, IEnumerable<IModifierInstance> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var modifier in newModifiers)
                SetModifier(itemModifiers, modifier);
        }

        private void SetModifier(HashSet<IModifierInstance> itemModifiers, IModifierInstance newModifier)
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
