namespace LastBreath.Source.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class EquipItem :  Resource, IEquipItem, IAscendable
    {
        private float _currentUpdateMultiplier = 1f;
        private HashSet<IModifier> _baseModifiers = [];
        private HashSet<IModifier> _additionalModifiers = [];
        private List<IModifier> _modifiersPool = [];
        private Dictionary<string, int> _usedResources = [];
        private IEntity? _owner;

        [Export] public EquipmentType EquipmentPart { get; set; }
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int Quantity { get; set; } = 1;
        [Export] public int MaxStackSize { get; set; } = 1;

        [Export] public Texture2D? Icon { get; set; }
        // {
        //     get
        //     {
        //         if (field != null) return field;
        //         field = ResourceLoader.Load<Texture2D>($"res://Source/Items/{Id.Replace("_", "")}.png");
        //         return field;
        //     }
        //     private set;
        // }

        [Export] public Rarity Rarity { get; set; } = Rarity.Uncommon;
        [Export] public string[] Tags { get; set; } = [];
        [Export] public AttributeType AttributeType { get; set; } = AttributeType.None;
        [Export] public int UpdateLevel { get; set; } = 0;
        [Export] public int MaxUpdateLevel { get; set; } = 12;
        [Export] public string ItemEffect { get; private set; } = string.Empty;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

        public IReadOnlyList<IModifier> AdditionalModifiers => [.. _additionalModifiers];
        public IReadOnlyList<IModifier> BaseModifiers => [.. _baseModifiers];
        public IReadOnlyList<IModifier> ModifiersPool => _modifiersPool;
        public IReadOnlyDictionary<string, int> UsedResources => _usedResources;
        public bool IsAscendable => Rarity == Rarity.Legendary && UpdateLevel >= 12;


        public EquipItem()
        {
        }

        public EquipItem(EquipmentType part, string id, string[] tags)
        {
            EquipmentPart = part;
            Id = id;
            Tags = tags;
        }

        public T Copy<T>()
        {
            var copy = (IEquipItem)DuplicateDeep();
            copy.SetBaseModifiers(_baseModifiers.ToHashSet());
            copy.SetAdditionalModifiers(_additionalModifiers.ToHashSet());
            copy.SaveUsedResources(_usedResources.ToDictionary());
            copy.SaveModifiersPool(_modifiersPool.ToList());
            return (T)copy;
        }

        public override int GetHashCode() => HashCode.Combine(Id, InstanceId, Rarity);

        public override bool Equals(object? obj)
        {
            if (obj is not IEquipItem other) return false;
            return Id == other.Id;
        }

        public void SetBaseModifiers(IEnumerable<IModifier> modifiers) => SetModifiers(_baseModifiers, modifiers);

        public void SetAdditionalModifiers(IEnumerable<IModifier> modifiers) =>
            SetModifiers(_additionalModifiers, modifiers);

        public void SetItemEffect(string effectId) => ItemEffect = effectId;

        public void RemoveAdditionalModifier(int hash) =>
            _additionalModifiers.RemoveWhere(m => m.GetHashCode() == hash);

        public void AddAdditionalModifier(IModifier modifier)
        {
            modifier.Value = modifier.BaseValue * _currentUpdateMultiplier;
            _additionalModifiers.Add(modifier);
        }

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool Upgrade(int upgradeLevel = 1)
        {
            if (UpdateLevel >= 12) return false;
            // I should change it later
            int canUpgrade = MaxUpdateLevel - UpdateLevel;
            int appliedUpgrade = Mathf.Min(upgradeLevel, canUpgrade);
            float upgradeAmount = appliedUpgrade / 10f;
            _currentUpdateMultiplier += upgradeAmount;
            UpdateLevel += appliedUpgrade;
            UpdateModifiersValue();

            return true;
        }

        public bool Downgrade(int downgradeLevel = 1)
        {
            // cant downgrade
            if (UpdateLevel == 0) return false;
            float downgradeAmount = Mathf.Min(downgradeLevel, MaxUpdateLevel) / 10f;
            _currentUpdateMultiplier = Mathf.Max(1f, _currentUpdateMultiplier - downgradeAmount);
            UpdateLevel = Mathf.Max(0, UpdateLevel - downgradeLevel);
            UpdateModifiersValue();
            return true;
        }

        public void ReplaceAdditionalModifier(int hash, IModifier newModifier)
        {
            _additionalModifiers.RemoveWhere(mod => mod.GetHashCode() == hash);
            _additionalModifiers.Add(newModifier);
        }

        public void OnUnequip()
        {
            //foreach (var mod in _baseModifiers.Concat(_additionalModifiers))
            //    _owner?.Modifiers.RemovePermanentModifier(mod);
            //_owner = null;
        }

        public void OnEquip(IEntity owner)
        {
            //_owner = owner;
            //var modifiers = _baseModifiers.Concat(_additionalModifiers);
            //foreach (var mod in _baseModifiers.Concat(_additionalModifiers))
            //    _owner.Modifiers.AddPermanentModifier(mod);
            //Skill?.Attach(_owner);
        }

        public void SaveModifiersPool(IEnumerable<IModifier> modifiers) => _modifiersPool.AddRange(modifiers);

        public void SaveUsedResources(Dictionary<string, int> resources)
        {
            foreach (var res in resources)
                _usedResources.TryAdd(res.Key, res.Value);
        }

        public bool TryAscend()
        {
            return false;
        }

        private void SetModifiers(HashSet<IModifier> itemModifiers, IEnumerable<IModifier> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var modifier in newModifiers)
            {
                modifier.Value = modifier.BaseValue * _currentUpdateMultiplier;
                itemModifiers.Add(modifier);
            }
        }

        private void UpdateModifiersValue()
        {
            foreach (var modifier in _baseModifiers.Concat(_additionalModifiers))
                modifier.Value = modifier.BaseValue * _currentUpdateMultiplier;
        }
    }
}
