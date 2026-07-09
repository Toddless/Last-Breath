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
        private readonly HashSet<IModifier> _implicits = [];
        private readonly HashSet<IModifier> _modifiers = [];
        private readonly List<ContextModifierEntry> _implicitsContextModifier = [];
        private readonly List<ContextModifierEntry> _contextModifiers = [];
        // TODO:
        // старый список пулов. Не рассчитан на модификаторы контекста и композитные/условные модификаторы
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
                // TODO:
                // сменить на нормальный путь позднее.
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>($"res://Internal/_Placeholders/Items/{Id}.png");
                return field;
            }
            private init;
        }
        public Rarity Rarity { get; set; } = Rarity.Uncommon;
        public int UpdateLevel { get; set; }
        public int MaxUpdateLevel { get; set; } = 12;
        public bool IsSealed { get; private set; }
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public IReadOnlyList<IModifier> Implicits => [.. _implicits];
        public IReadOnlyList<IModifier> Modifiers => [.. _modifiers];
        public IReadOnlyList<ContextModifierEntry> ContextImplicits => _implicitsContextModifier;
        public IReadOnlyList<ContextModifierEntry> ContextModifiers => _contextModifiers;
        public IReadOnlyList<IItemGrant> Grants => _grants;
        public IReadOnlyCollection<EntityParameter> AffectedParameters =>
            _implicits.Concat(_modifiers).Select(modifier => modifier.EntityParameter).ToHashSet();
        // TODO:
        // старый список пулов. Не рассчитан на модификаторы контекста и композитные/условные модификаторы
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
            // TODO:
            // иконки загружаются лениво, что мы здесь передаем??
            Icon = source.Icon;
            Rarity = source.Rarity;
            UpdateLevel = source.UpdateLevel;
            MaxUpdateLevel = source.MaxUpdateLevel;
            UpdateMultiplier = source.UpdateMultiplier;

            SetImplicits(CopyModifiers(source._implicits));
            SetModifiers(CopyModifiers(source._modifiers));
            SetContextImplicits(source._implicitsContextModifier.Select(entry => entry.Copy()));
            SetContextModifiers(source._contextModifiers.Select(entry => entry.Copy()));
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
            SetModifiers(_implicits, modifiers);
        }

        public void SetModifiers(IEnumerable<IModifier> modifiers)
        {
            if (IsSealed) return;
            SetModifiers(_modifiers, modifiers);
        }

        public void SetContextImplicits(IEnumerable<ContextModifierEntry> entries)
        {
            if (IsSealed) return;
            SetContextModifiers(_implicitsContextModifier, entries);
        }

        public void SetContextModifiers(IEnumerable<ContextModifierEntry> entries)
        {
            if (IsSealed) return;
            SetContextModifiers(_contextModifiers, entries);
        }

        public void AddAdditionalModifier(IModifier modifier)
        {
            if (IsSealed) return;
            if (modifier is CompositeModifier composite)
            {
                foreach (var part in ExpandComposite(composite)) AddAdditionalModifier(part);
                return;
            }

            modifier.Value = modifier.BaseValue * UpdateMultiplier;
            _modifiers.Add(modifier);
            _resolvedModifiers = null;
        }

        public void RemoveAdditionalModifier(int hash)
        {
            if (IsSealed) return;
            _modifiers.RemoveWhere(modifier => modifier.GetHashCode() == hash);
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
            foreach (var entry in AllContextModifiers) entry.Attach(owner);
        }

        public void OnUnequip()
        {
            if (_owner == null) return;
            foreach (var grant in _grants) grant.Detach(_owner);
            foreach (var entry in AllContextModifiers) entry.Detach(_owner);
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

        protected IEnumerable<IModifier> AllModifiers => _implicits.Concat(_modifiers);

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

        private IEnumerable<ContextModifierEntry> AllContextModifiers => _implicitsContextModifier.Concat(_contextModifiers);

        private void SetContextModifiers(List<ContextModifierEntry> itemEntries, IEnumerable<ContextModifierEntry> newEntries)
        {
            itemEntries.Clear();
            foreach (var entry in newEntries)
            {
                entry.Value = entry.BaseValue * UpdateMultiplier;
                itemEntries.Add(entry);
            }
        }

        private void SetModifiers(HashSet<IModifier> itemModifiers, IEnumerable<IModifier> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var modifier in newModifiers.SelectMany(ExpandIfComposite))
            {
                modifier.Value = modifier.BaseValue * UpdateMultiplier;
                itemModifiers.Add(modifier);
            }

            _resolvedModifiers = null;
        }

        private IEnumerable<IModifier> ExpandIfComposite(IModifier modifier) =>
            modifier is CompositeModifier composite ? ExpandComposite(composite) : [modifier];

        // Composites live in pools/data only: on the item they become plain per-parameter modifiers,
        // so scaling, resolution, save and UI stay single-parameter. Fresh instances detach from the shared pool entry.
        private IEnumerable<IModifier> ExpandComposite(CompositeModifier composite) =>
            composite.Parts.Select(IModifier (part) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(part.EntityParameter, part.ModifierValueType, part.BaseValue, InstanceId);
                copy.Scope = part.Scope;
                return copy;
            });

        private void UpdateModifiersValue()
        {
            foreach (var modifier in _implicits.Concat(_modifiers))
                modifier.Value = modifier.BaseValue * UpdateMultiplier;
            foreach (var entry in AllContextModifiers)
                entry.Value = entry.BaseValue * UpdateMultiplier;

            _resolvedModifiers = null;
        }

        private Dictionary<EntityParameter, List<IModifierInstance>> BuildResolvedModifiers()
        {
            var resolved = new Dictionary<EntityParameter, List<IModifierInstance>>();
            // NOTE:
            // Здесь мы обрабатываем только простые модификаторы изменяющие параметры Что с контекстными?
            // NOTE: Композитные модификаторы разбираются ДО резолва
            foreach (var group in _implicits.Concat(_modifiers).GroupBy(modifier => modifier.EntityParameter))
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
