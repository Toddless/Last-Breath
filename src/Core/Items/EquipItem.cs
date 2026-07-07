namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;
    using Godot;
    using Modifiers;

    public class EquipItem : IEquipItem, IAscendable
    {
        private readonly HashSet<IModifier> _baseModifiers = [];
        private readonly HashSet<IModifier> _additionalModifiers = [];
        private readonly List<IModifier> _modifiersPool = [];
        private readonly Dictionary<string, int> _usedResources = [];
        private readonly List<IItemGrant> _grants = [];
        private Dictionary<EntityParameter, List<IModifierInstance>>? _resolvedModifiers;
        private IFightable? _owner;

        protected float UpdateMultiplier { get; private set; } = 1f;

        public EquipmentPiece EquipmentPiece { get; }
        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; }
        public int MaxStackSize => 1;

        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>($"res://Internal/_Placeholders/Items/{Id}.png");
                return field;
            }
            private init;
        }
        public Rarity Rarity { get; set; } = Rarity.Uncommon;
        public int UpdateLevel { get; set; }
        public int MaxUpdateLevel { get; set; } = 12;
        public string ItemEffect { get; private set; } = string.Empty;
        public bool IsSealed { get; private set; }
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public IReadOnlyList<IModifier> Implicits => [.. _baseModifiers];
        public IReadOnlyList<IModifier> Modifiers => [.. _additionalModifiers];
        public IReadOnlyList<IItemGrant> Grants => _grants;
        public IReadOnlyCollection<EntityParameter> AffectedParameters =>
            _baseModifiers.Concat(_additionalModifiers).Select(modifier => modifier.EntityParameter).ToHashSet();
        public IReadOnlyList<IModifier> ModifiersPool => _modifiersPool;
        public IReadOnlyDictionary<string, int> UsedResources => _usedResources;
        public bool IsAscendable => Rarity == Rarity.Legendary && UpdateLevel >= MaxUpdateLevel;

        public EquipItem(EquipmentPiece piece, string id, string[] tags)
        {
            EquipmentPiece = piece;
            Id = id;
            Tags = tags;
        }

        protected EquipItem(EquipItem source)
        {
            EquipmentPiece = source.EquipmentPiece;
            Id = source.Id;
            Tags = [.. source.Tags];
            Icon = source.Icon;
            Rarity = source.Rarity;
            UpdateLevel = source.UpdateLevel;
            MaxUpdateLevel = source.MaxUpdateLevel;
            ItemEffect = source.ItemEffect;
            UpdateMultiplier = source.UpdateMultiplier;

            SetImplicits(CopyModifiers(source._baseModifiers));
            SetModifiers(CopyModifiers(source._additionalModifiers));
            _grants.AddRange(source._grants.Select(grant => grant.Copy()));
            _modifiersPool.AddRange(source._modifiersPool);
            foreach (var resource in source._usedResources)
                _usedResources.Add(resource.Key, resource.Value);
            IsSealed = source.IsSealed;
        }

        public T Copy<T>() => (T)(object)CreateCopy();

        // Local stage: local modifiers scale the item's own flat values and never leave the item.
        // The entity-side pull model only ever sees the output of this method.
        public IEnumerable<IModifierInstance> GetResolvedModifiers(EntityParameter parameter)
        {
            _resolvedModifiers ??= BuildResolvedModifiers();
            return _resolvedModifiers.TryGetValue(parameter, out var modifiers) ? modifiers : [];
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public void SetImplicits(IEnumerable<IModifier> modifiers)
        {
            if (IsSealed) return;
            SetModifiers(_baseModifiers, modifiers);
        }

        public void SetModifiers(IEnumerable<IModifier> modifiers)
        {
            if (IsSealed) return;
            SetModifiers(_additionalModifiers, modifiers);
        }

        public void SetItemEffect(string effectId)
        {
            if (IsSealed) return;
            ItemEffect = effectId;
        }

        public void AddAdditionalModifier(IModifier modifier)
        {
            if (IsSealed) return;
            modifier.Value = modifier.BaseValue * UpdateMultiplier;
            _additionalModifiers.Add(modifier);
            _resolvedModifiers = null;
        }

        public void RemoveAdditionalModifier(int hash)
        {
            if (IsSealed) return;
            _additionalModifiers.RemoveWhere(modifier => modifier.GetHashCode() == hash);
            _resolvedModifiers = null;
        }

        public void ReplaceAdditionalModifier(int hash, IModifier newModifier)
        {
            RemoveAdditionalModifier(hash);
            AddAdditionalModifier(newModifier);
        }

        public bool Upgrade(int upgradeLevel = 1)
        {
            if (IsSealed) return false;
            if (UpdateLevel >= MaxUpdateLevel) return false;
            int appliedLevels = Math.Min(upgradeLevel, MaxUpdateLevel - UpdateLevel);
            UpdateMultiplier += appliedLevels / 10f;
            UpdateLevel += appliedLevels;
            UpdateModifiersValue();
            return true;
        }

        public bool Downgrade(int downgradeLevel = 1)
        {
            if (IsSealed) return false;
            if (UpdateLevel == 0) return false;
            int appliedLevels = Math.Min(downgradeLevel, UpdateLevel);
            UpdateMultiplier = Math.Max(1f, UpdateMultiplier - (appliedLevels / 10f));
            UpdateLevel -= appliedLevels;
            UpdateModifiersValue();
            return true;
        }

        public void AddGrant(IItemGrant grant)
        {
            if (IsSealed) return;
            _grants.Add(grant);
        }

        public void OnEquip(IFightable owner)
        {
            _owner = owner;
            foreach (var grant in _grants) grant.Attach(owner);
        }

        public void OnUnequip()
        {
            if (_owner == null) return;
            foreach (var grant in _grants) grant.Detach(_owner);
            _owner = null;
        }

        public void SaveModifiersPool(IEnumerable<IModifier> modifiers) => _modifiersPool.AddRange(modifiers);

        public void SaveUsedResources(Dictionary<string, int> resources)
        {
            foreach (var resource in resources)
                _usedResources.TryAdd(resource.Key, resource.Value);
        }

        // State transition only; the ascension gift roll lives in the crafting-side ascender.
        public bool TryAscend()
        {
            if (IsSealed || !IsAscendable) return false;
            Rarity = Rarity.Mythic;
            IsSealed = true;
            return true;
        }

        public override int GetHashCode() => HashCode.Combine(Id, InstanceId, Rarity);

        public override bool Equals(object? obj)
        {
            if (obj is not IEquipItem other) return false;
            return Id == other.Id;
        }


        protected IEnumerable<IModifier> AllModifiers => _baseModifiers.Concat(_additionalModifiers);

        protected virtual EquipItem CreateCopy() => new(this);

        // Parameters whose local bucket is consumed outside the generic resolution
        // (weapon damage: locals fold into the base value channel instead of a synthetic flat modifier).
        protected virtual bool IsLocalBucketExternal(EntityParameter parameter) => false;

        private IEnumerable<IModifier> CopyModifiers(IEnumerable<IModifier> modifiers) =>
            modifiers.Select(IModifier (modifier) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue, InstanceId);
                copy.Scope = modifier.Scope;
                return copy;
            });

        private void SetModifiers(HashSet<IModifier> itemModifiers, IEnumerable<IModifier> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var modifier in newModifiers)
            {
                modifier.Value = modifier.BaseValue * UpdateMultiplier;
                itemModifiers.Add(modifier);
            }

            _resolvedModifiers = null;
        }

        private void UpdateModifiersValue()
        {
            foreach (var modifier in _baseModifiers.Concat(_additionalModifiers))
                modifier.Value = modifier.BaseValue * UpdateMultiplier;

            _resolvedModifiers = null;
        }

        private Dictionary<EntityParameter, List<IModifierInstance>> BuildResolvedModifiers()
        {
            var resolved = new Dictionary<EntityParameter, List<IModifierInstance>>();
            foreach (var group in _baseModifiers.Concat(_additionalModifiers).GroupBy(modifier => modifier.EntityParameter))
            {
                var modifiers = new List<IModifierInstance>();
                bool localsExternal = IsLocalBucketExternal(group.Key);
                float localFlat = 0f, localIncrease = 0f, localMultiplier = 0f;
                foreach (var modifier in group)
                {
                    if (modifier.Scope == ModifierScope.Global)
                    {
                        modifiers.Add(AsInstance(modifier));
                        continue;
                    }

                    if (localsExternal) continue;

                    switch (modifier.ModifierValueType)
                    {
                        case ModifierValueType.Flat: localFlat += modifier.Value; break;
                        case ModifierValueType.Increase: localIncrease += modifier.Value; break;
                        case ModifierValueType.Multiplicative: localMultiplier += modifier.Value; break;
                    }
                }

                if (localFlat != 0f)
                {
                    float resolvedFlat = localFlat * (1f + localIncrease) * (1f + localMultiplier);
                    modifiers.Add(ModifiersCreator.CreateModifierInstance(group.Key, ModifierValueType.Flat, resolvedFlat, InstanceId));
                }

                if (modifiers.Count > 0) resolved[group.Key] = modifiers;
            }

            return resolved;
        }

        private IModifierInstance AsInstance(IModifier modifier) =>
            modifier as IModifierInstance
            ?? new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.Value, InstanceId, modifier.Weight) { Scope = modifier.Scope };
    }
}
