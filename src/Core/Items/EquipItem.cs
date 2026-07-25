namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Constants;
    using Entity;
    using Enums;
    using Godot;
    using Modifiers;

    public class EquipItem : IEquipItem, IAscendable
    {
        // Design reference: each sharpening level adds +5% to every line value.
        private const float UpgradeBonusPerLevel = 0.05f;

        private readonly List<IModifierInstance> _implicits = [];
        private readonly List<IModifierInstance> _modifiers = [];
        private readonly List<ContextModifierEntry> _implicitsContextModifier = [];
        private readonly List<ContextModifierEntry> _contextModifiers = [];
        // Two carriers by design: the recipe's mandatory resources and the optional additives are
        // different currencies (a rune of creation cares which slot fed the pool), but most consumers
        // read the merged UsedResources view.
        private readonly Dictionary<string, int> _usedRequiredResources = [];
        private readonly Dictionary<string, int> _usedOptionalResources = [];
        private readonly List<IItemGrant> _grants = [];
        private Dictionary<EntityParameter, List<IModifierInstance>>? _resolvedModifiers;
        private IFightable? _owner;

        // THE line-value formula: every stored line (both channels, implicit or rolled) is worth
        // Base × sharpening scale × ascension scale. All five write paths go through this.
        private float LineMultiplier => UpdateMultiplier * AscensionMultiplier;
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
                field = ResourceLoader.Load<Texture2D>(AssetPaths.ItemIcon(Id));
                return field;
            }
            private init;
        }
        public Rarity Rarity { get; set; } = Rarity.Common;
        // The item's caliber: loot stamps the kill's difficulty multiplier, crafting stamps the mastery
        // quality multiplier. Scales the LIVE reroll pool on every recraft, so a rerolled line matches
        // the magnitude the item was born with. Never touched by upgrades (that's UpdateMultiplier).
        public float PowerMultiplier { get; set; } = 1f;
        // Successful rerolls only (the upgrader increments): drives the growing recraft price.
        public int RecraftCount { get; set; }

        // Ascension's "everything +15%": a factor on top of the sharpening scale, part of the one
        // line-value formula (Base × UpdateMultiplier × AscensionMultiplier). Assigning recomputes
        // every line so the multiplier can never desync from the values; sealed items refuse like
        // every other mutation (restore assigns it BEFORE the seal replay).
        public float AscensionMultiplier
        {
            get;
            set
            {
                if (IsSealed) return;
                field = value;
                UpdateModifiersValue();
            }
        } = 1f;
        // No public setter: levels only move through Upgrade/Downgrade so the multiplier and
        // line values can never desync from the level.
        public int UpdateLevel { get; private set; }
        public int MaxUpdateLevel { get; set; } = 12;
        public bool IsSealed { get; private set; }
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        public IReadOnlyList<IModifierInstance> Implicits => [.. _implicits];
        public IReadOnlyList<IModifierInstance> Modifiers => [.. _modifiers];
        public IReadOnlyList<ContextModifierEntry> ContextImplicits => _implicitsContextModifier;
        public IReadOnlyList<ContextModifierEntry> ContextModifiers => _contextModifiers;
        public IReadOnlyList<IItemGrant> Grants => _grants;
        public IReadOnlyCollection<EntityParameter> AffectedParameters =>
            _implicits.Concat(_modifiers).Select(modifier => modifier.EntityParameter).ToHashSet();
        public IReadOnlyDictionary<string, int> UsedRequiredResources => _usedRequiredResources;
        public IReadOnlyDictionary<string, int> UsedOptionalResources => _usedOptionalResources;
        public IReadOnlyDictionary<string, int> UsedResources
        {
            get
            {
                var merged = new Dictionary<string, int>(_usedRequiredResources);
                foreach ((string id, int amount) in _usedOptionalResources)
                    merged[id] = merged.GetValueOrDefault(id) + amount;
                return merged;
            }
        }
        public bool IsAscendable => Rarity == Rarity.Legendary && UpdateLevel >= MaxUpdateLevel;

        public float NextUpgradeValueScale => (UpdateMultiplier + UpgradeBonusPerLevel) / UpdateMultiplier;

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
            // Icon is intentionally NOT copied: the path derives from Id, so the copy lazy-loads its
            // own texture on first access. Reading source.Icon here would force a ResourceLoader call
            // on every copy — and hard-crash hosts without the Godot runtime (tests, simulations).
            Rarity = source.Rarity;
            PowerMultiplier = source.PowerMultiplier;
            RecraftCount = source.RecraftCount;
            UpdateLevel = source.UpdateLevel;
            MaxUpdateLevel = source.MaxUpdateLevel;
            UpdateMultiplier = source.UpdateMultiplier;
            AscensionMultiplier = source.AscensionMultiplier; // before the lines: Set* below scale by it

            SetImplicits(CopyModifiers(source._implicits));
            SetModifiers(CopyModifiers(source._modifiers));
            SetContextImplicits(source._implicitsContextModifier.Select(entry => entry.Copy()));
            SetContextModifiers(source._contextModifiers.Select(entry => entry.Copy()));
            _grants.AddRange(source._grants.Select(grant => grant.Copy()));
            foreach (var resource in source._usedRequiredResources)
                _usedRequiredResources.Add(resource.Key, resource.Value);
            foreach (var resource in source._usedOptionalResources)
                _usedOptionalResources.Add(resource.Key, resource.Value);
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

        public void AddAdditionalModifier(IModifierInstance modifier) => InsertAdditionalModifier(_modifiers.Count, modifier);

        // Rolled context line ("+35% burning damage"). Attaches to the owner on equip like any context line;
        // crafting operates on inventory (unequipped) items, so there is no live owner to attach to here.
        public void AddAdditionalContextModifier(ContextModifierEntry entry) => InsertAdditionalContextModifier(_contextModifiers.Count, entry);

        // Position is display state the reroll must preserve (its replacement line takes the slot of the one
        // it replaced), so the whole Add path routes through the insert — appending is just "at the end".
        public void InsertAdditionalModifier(int index, IModifierInstance modifier)
        {
            if (IsSealed) return;
            if (modifier is CompositeModifier composite)
            {
                foreach (var part in ExpandComposite(composite)) InsertAdditionalModifier(index++, part);
                return;
            }

            modifier.Value = Scaled(modifier.BaseValue, modifier.ModifierValueType);
            _modifiers.Insert(Math.Clamp(index, 0, _modifiers.Count), modifier);
            _resolvedModifiers = null;
        }

        public void InsertAdditionalContextModifier(int index, ContextModifierEntry entry)
        {
            if (IsSealed) return;
            entry.Value = Scaled(entry.BaseValue, entry.ValueType);
            _contextModifiers.Insert(Math.Clamp(index, 0, _contextModifiers.Count), entry);
        }

        // Identity is InstanceId (variant B): duplicates of the same parameter+type may coexist across both
        // channels, so only the exact instance is removed (from whichever channel holds it).
        public void RemoveAdditionalModifier(string instanceId)
        {
            if (IsSealed) return;
            _modifiers.RemoveAll(modifier => modifier.InstanceId == instanceId);
            _contextModifiers.RemoveAll(entry => entry.InstanceId == instanceId);
            _resolvedModifiers = null;
        }

        public void ReplaceAdditionalModifier(string instanceId, IModifierInstance newModifier)
        {
            RemoveAdditionalModifier(instanceId);
            AddAdditionalModifier(newModifier);
        }

        public bool Upgrade(int upgradeLevel = 1)
        {
            if (IsSealed) return false;
            if (UpdateLevel >= MaxUpdateLevel) return false;
            int appliedLevels = Math.Min(upgradeLevel, MaxUpdateLevel - UpdateLevel);
            UpdateMultiplier += appliedLevels * UpgradeBonusPerLevel;
            UpdateLevel += appliedLevels;
            UpdateModifiersValue();
            return true;
        }

        public bool Downgrade(int downgradeLevel = 1)
        {
            if (IsSealed) return false;
            if (UpdateLevel == 0) return false;
            int appliedLevels = Math.Min(downgradeLevel, UpdateLevel);
            UpdateMultiplier = Math.Max(1f, UpdateMultiplier - (appliedLevels * UpgradeBonusPerLevel));
            UpdateLevel -= appliedLevels;
            UpdateModifiersValue();
            return true;
        }

        public void AddGrant(IItemGrant grant)
        {
            if (IsSealed) return;
            _grants.Add(grant);
        }

        // One-time by design (ascension): grants do not scale with sharpening, so their payloads have no
        // recomputable base — the scaled copies simply become the item's grants and save as such.
        public void ScaleGrantValues(float factor)
        {
            if (IsSealed) return;
            for (int i = 0; i < _grants.Count; i++)
                _grants[i] = _grants[i].WithScaledValues(factor);
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

        public void SaveUsedResources(IReadOnlyDictionary<string, int> required, IReadOnlyDictionary<string, int> optional)
        {
            foreach (var resource in required)
                _usedRequiredResources.TryAdd(resource.Key, resource.Value);
            foreach (var resource in optional)
                _usedOptionalResources.TryAdd(resource.Key, resource.Value);
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

        /// <summary>Sums the LOCAL lines of one parameter into the flat/increase/multiplier buckets —
        /// THE one place local contributions are gathered (resolve, weapon stats, tooltip views).</summary>
        protected (float Flat, float Increase, float Multiplier) LocalBucket(EntityParameter parameter)
        {
            float flat = 0f, increase = 0f, multiplier = 0f;
            foreach (var modifier in AllModifiers)
            {
                if (modifier.Scope != ModifierScope.Local || modifier.EntityParameter != parameter) continue;
                switch (modifier.ModifierValueType)
                {
                    case ModifierValueType.Flat: flat += modifier.Value; break;
                    case ModifierValueType.Increase: increase += modifier.Value; break;
                    case ModifierValueType.Multiplicative: multiplier += modifier.Value; break;
                }
            }

            return (flat, increase, multiplier);
        }

        /// <summary>The local bucket collapsed into the single flat value the resolve path hands the
        /// owner: flat × (1 + increase) × (1 + multiplier). An increase-only bucket yields 0 —
        /// percent locals amplify a flat local, they have no base of their own here.</summary>
        protected float ResolvedLocalFlat(EntityParameter parameter)
        {
            (float flat, float increase, float multiplier) = LocalBucket(parameter);
            return flat * (1f + increase) * (1f + multiplier);
        }


        /// <summary>The scaled value of one line. A flag line ("attacks ignore elemental resistances") is a
        /// switch, not a number: sharpening and ascension leave it exactly as data wrote it.</summary>
        private float Scaled(float baseValue, ModifierValueType type) =>
            type == ModifierValueType.Flag ? baseValue : baseValue * LineMultiplier;

        protected virtual EquipItem CreateCopy() => new(this);

        // Parameters whose local bucket is consumed outside the generic resolution
        // (weapon damage: locals fold into the base value channel instead of a synthetic flat modifier).
        protected virtual bool IsLocalBucketExternal(EntityParameter parameter) => false;

        private IEnumerable<IModifier> CopyModifiers(IEnumerable<IModifier> modifiers) =>
            modifiers.Select(IModifier (modifier) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue, InstanceId);
                copy.Scope = modifier.Scope;
                SimpleModifier.TransferStamps(modifier, copy); // roll provenance must survive an item copy
                return copy;
            });

        private IEnumerable<ContextModifierEntry> AllContextModifiers => _implicitsContextModifier.Concat(_contextModifiers);

        private void SetContextModifiers(List<ContextModifierEntry> itemEntries, IEnumerable<ContextModifierEntry> newEntries)
        {
            itemEntries.Clear();
            foreach (var entry in newEntries)
            {
                entry.Value = Scaled(entry.BaseValue, entry.ValueType);
                itemEntries.Add(entry);
            }
        }

        private void SetModifiers(List<IModifierInstance> itemModifiers, IEnumerable<IModifier> newModifiers)
        {
            itemModifiers.Clear();
            foreach (var instance in newModifiers.SelectMany(ToInstances))
            {
                instance.Value = Scaled(instance.BaseValue, instance.ModifierValueType);
                itemModifiers.Add(instance);
            }

            _resolvedModifiers = null;
        }

        // Everything stored on the item is an instance: the stable InstanceId is the reroll identity (variant B),
        // and wrapping detaches the item from shared data/material templates. Composites expand to their parts.
        private IEnumerable<IModifierInstance> ToInstances(IModifier modifier) => modifier switch
        {
            CompositeModifier composite => ExpandComposite(composite),
            IModifierInstance instance => [instance],
            _ => [new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue, InstanceId, modifier.Weight) { Scope = modifier.Scope }],
        };

        // Composites live in pools/data only: on the item they become plain per-parameter modifiers,
        // so scaling, resolution, save and UI stay single-parameter. Fresh instances detach from the shared pool entry.
        private IEnumerable<IModifierInstance> ExpandComposite(CompositeModifier composite) =>
            composite.Parts.Select(IModifierInstance (part) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(part.EntityParameter, part.ModifierValueType, part.BaseValue, InstanceId);
                copy.Scope = part.Scope;
                SimpleModifier.TransferStamps(part, copy);
                return copy;
            });

        private void UpdateModifiersValue()
        {
            foreach (var modifier in _implicits.Concat(_modifiers))
                modifier.Value = Scaled(modifier.BaseValue, modifier.ModifierValueType);
            foreach (var entry in AllContextModifiers)
                entry.Value = Scaled(entry.BaseValue, entry.ValueType);

            _resolvedModifiers = null;
        }

        private Dictionary<EntityParameter, List<IModifierInstance>> BuildResolvedModifiers()
        {
            var resolved = new Dictionary<EntityParameter, List<IModifierInstance>>();
            // Only plain parameter modifiers resolve here: context lines deliberately travel their own
            // channel (Attach to the owner's pipelines on equip), and composites are expanded before resolve.
            foreach (var group in _implicits.Concat(_modifiers).GroupBy(modifier => modifier.EntityParameter))
            {
                var modifiers = new List<IModifierInstance>();
                foreach (var modifier in group)
                    if (modifier.Scope == ModifierScope.Global)
                        modifiers.Add(AsInstance(modifier));

                if (!IsLocalBucketExternal(group.Key))
                {
                    float resolvedFlat = ResolvedLocalFlat(group.Key);
                    if (resolvedFlat != 0f)
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
