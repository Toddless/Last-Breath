namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Constants;
    using Entity;
    using Enums;
    using Godot;
    using Interfaces;
    using Modifiers;

    public class EquipItem : IEquipItem, IAscendable
    {
        // Design reference: each sharpening level adds +5% to the BASE channel — base stats, the weapon
        // triple and the flat implicits.
        private const float UpgradeBonusPerLevel = 0.05f;

        private readonly List<IModifierInstance> _implicits = [];
        private readonly List<IModifierInstance> _modifiers = [];
        // The piece's typed base channel (armor's evade, a ring's health), rolled once by the minter.
        // Stored UNSCALED: the base channel applies on read, exactly like the implicit line values.
        private readonly Dictionary<EntityParameter, float> _baseStats = [];
        private readonly List<ContextModifierEntry> _implicitsContextModifier = [];
        private readonly List<ContextModifierEntry> _contextModifiers = [];
        // Two carriers by design: the recipe's mandatory resources and the optional additives are
        // different currencies (a rune of creation cares which slot fed the pool), but most consumers
        // read the merged UsedResources view.
        private readonly Dictionary<string, int> _usedRequiredResources = [];
        private readonly Dictionary<string, int> _usedOptionalResources = [];
        private readonly List<IItemGrant> _grants = [];
        // The predicates of the worn conditional lines, each with the handler that answers its flips —
        // the only thing an equipped item has to hand back, and the reason it is kept at all.
        private readonly List<(ICondition Condition, Action<bool> OnFlip)> _watchedConditions = [];
        private Dictionary<EntityParameter, List<IModifierInstance>>? _resolvedModifiers;
        private IFightable? _owner;

        // The ROLLED channel — affixes and everything a recraft puts on the item. Only ascension's
        // "+15% to everything" reaches it: sharpening does not, because a rolled value was already
        // scaled once by the item's caliber (PowerMultiplier) at the moment it was rolled.
        private float RolledChannelMultiplier => AscensionMultiplier;

        // The BASE channel — what the piece itself is worth: the typed base stats, the weapon triple
        // and the flat IMPLICIT lines. Both scales reach it: Base × sharpening × ascension.
        protected float BaseChannelMultiplier => UpdateMultiplier * AscensionMultiplier;
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

        // Ascension's "everything +15%": the one scale that reaches BOTH channels. Assigning recomputes
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
            _implicits.Concat(_modifiers).Select(modifier => modifier.EntityParameter).Concat(_baseStats.Keys).ToHashSet();

        public IReadOnlyDictionary<EntityParameter, float> BaseStats => _baseStats;
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

        /// <summary>What the next sharpening level does to the BASE channel — base stats, the weapon
        /// triple and the flat implicits. Rolled lines do not move with it (ascension cancels out of
        /// the ratio, so the scale is the same before and after an ascension).</summary>
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
            AscensionMultiplier = source.AscensionMultiplier; // before the lines: both channels below scale by it
            foreach (var stat in source._baseStats) _baseStats[stat.Key] = stat.Value;

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

        public void SetBaseStats(IEnumerable<KeyValuePair<EntityParameter, float>> stats)
        {
            if (IsSealed) return;
            _baseStats.Clear();
            foreach ((var parameter, float value) in stats) _baseStats[parameter] = value;
            _resolvedModifiers = null;
        }

        /// <summary>The weapon-damage convention generalized: base × the BASE channel scale, then the
        /// whole local bucket folds around it — (scaledBase + flat) × (1 + increase) × (1 + multiplier).
        /// LocalBonus is everything on top of the scaled base, each line already scaled by its own channel.</summary>
        public (float Base, float LocalBonus) GetBaseStatBreakdown(EntityParameter parameter)
        {
            float scaledBase = _baseStats.GetValueOrDefault(parameter) * BaseChannelMultiplier;
            (float flat, float increase, float multiplier) = LocalBucket(parameter);
            float effective = (scaledBase + flat) * (1f + increase) * (1f + multiplier);
            return (scaledBase, effective - scaledBase);
        }

        public void SetImplicits(IEnumerable<IModifier> modifiers)
        {
            if (IsSealed) return;
            SetModifiers(_implicits, modifiers, BaseChannelMultiplier);
        }

        public void SetModifiers(IEnumerable<IModifier> modifiers)
        {
            if (IsSealed) return;
            SetModifiers(_modifiers, modifiers, RolledChannelMultiplier);
        }

        public void SetContextImplicits(IEnumerable<ContextModifierEntry> entries)
        {
            if (IsSealed) return;
            SetContextModifiers(_implicitsContextModifier, entries, BaseChannelMultiplier);
        }

        public void SetContextModifiers(IEnumerable<ContextModifierEntry> entries)
        {
            if (IsSealed) return;
            SetContextModifiers(_contextModifiers, entries, RolledChannelMultiplier);
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

            modifier.Value = Scaled(modifier.BaseValue, modifier.ModifierValueType, RolledChannelMultiplier);
            _modifiers.Insert(Math.Clamp(index, 0, _modifiers.Count), modifier);
            _resolvedModifiers = null;
        }

        public void InsertAdditionalContextModifier(int index, ContextModifierEntry entry)
        {
            if (IsSealed) return;
            entry.Value = Scaled(entry.BaseValue, entry.ValueType, RolledChannelMultiplier);
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
            WatchConditions(owner);
        }

        public void OnUnequip()
        {
            if (_owner == null) return;
            ReleaseConditions();
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
                // A local line is folded into ONE number before it leaves the item, so a line held up by a
                // condition has to be weighed here — nothing downstream can tell it apart afterwards.
                if (modifier is IConditionalModifier { IsActive: false }) continue;
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



        /// <summary>The scaled value of one line — FLAT lines ONLY, by the multiplier of the channel the line
        /// belongs to. A percent line (increase/multiplicative) already multiplies a value the scales have
        /// raised, so scaling it too would stack a multiplier on a multiplier; a flag line ("attacks ignore
        /// elemental resistances") is a switch, not a number. Both stay exactly as data wrote them.</summary>
        private static float Scaled(float baseValue, ModifierValueType type, float channelMultiplier) =>
            type == ModifierValueType.Flat ? baseValue * channelMultiplier : baseValue;

        protected virtual EquipItem CreateCopy() => new(this);

        // Parameters whose local bucket is consumed outside the generic per-line resolution: any
        // parameter with a BASE STAT folds its locals around that base (see GetBaseStatBreakdown);
        // weapon damage additionally folds into the weapon's own value channel (override below).
        protected virtual bool IsLocalBucketExternal(EntityParameter parameter) => _baseStats.ContainsKey(parameter);

        private IEnumerable<IModifier> CopyModifiers(IEnumerable<IModifier> modifiers) =>
            modifiers.Select(IModifier (modifier) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(modifier.EntityParameter, modifier.ModifierValueType, modifier.BaseValue, InstanceId);
                copy.Scope = modifier.Scope;
                SimpleModifier.TransferStamps(modifier, copy); // roll provenance must survive an item copy
                return copy;
            });

        private IEnumerable<ContextModifierEntry> AllContextModifiers => _implicitsContextModifier.Concat(_contextModifiers);

        private void SetContextModifiers(List<ContextModifierEntry> itemEntries, IEnumerable<ContextModifierEntry> newEntries, float channelMultiplier)
        {
            itemEntries.Clear();
            itemEntries.AddRange(newEntries);
            RescaleContext(itemEntries, channelMultiplier);
        }

        private void SetModifiers(List<IModifierInstance> itemModifiers, IEnumerable<IModifier> newModifiers, float channelMultiplier)
        {
            itemModifiers.Clear();
            itemModifiers.AddRange(newModifiers.SelectMany(ToInstances));
            Rescale(itemModifiers, channelMultiplier);
            _resolvedModifiers = null;
        }

        /// <summary>Re-reads a whole list off its unscaled bases through the given channel. Single-line
        /// placements write the same value through <see cref="Scaled"/> directly.</summary>
        private static void Rescale(IEnumerable<IModifierInstance> lines, float channelMultiplier)
        {
            foreach (var line in lines)
                line.Value = Scaled(line.BaseValue, line.ModifierValueType, channelMultiplier);
        }

        private static void RescaleContext(IEnumerable<ContextModifierEntry> entries, float channelMultiplier)
        {
            foreach (var entry in entries)
                entry.Value = Scaled(entry.BaseValue, entry.ValueType, channelMultiplier);
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

        /// <summary>Points the predicates of the item's conditional lines at the wearer. The lines are never
        /// written onto him — he pulls them off the item while resolving a value — so a flip has nothing to
        /// add or remove: it drops what was resolved and asks for the parameter again.
        /// <para>Whatever was watched before is let go first: an item put on twice without coming off in
        /// between (a slot refreshed, a fighter rebuilt for a new scene) must watch its predicates once.</para></summary>
        private void WatchConditions(IFightable owner)
        {
            ReleaseConditions();
            foreach (var modifier in AllModifiers)
            {
                if (modifier is not SimpleModifier { Condition: { } condition } line) continue;

                var parameter = line.EntityParameter;
                Action<bool> onFlip = _ => RefreshParameter(parameter);
                condition.StateChanged += onFlip;
                condition.Attach(owner);
                _watchedConditions.Add((condition, onFlip));
            }
        }

        /// <summary>Lets every watched predicate go. An item taken off leaves nothing listening to the
        /// fighter who wore it, whichever line the predicate belonged to.</summary>
        private void ReleaseConditions()
        {
            foreach ((var condition, var onFlip) in _watchedConditions)
            {
                condition.StateChanged -= onFlip;
                condition.Detach();
            }

            _watchedConditions.Clear();
        }

        /// <summary>What a condition flip costs: the resolved view is thrown away and the wearer is asked
        /// to work the parameter out again.</summary>
        private void RefreshParameter(EntityParameter parameter)
        {
            _resolvedModifiers = null;
            _owner?.ParameterModifiers.RefreshParameter(parameter);
        }

        /// <summary>Every stored line back to its channel's current scale. Sharpening moves only the base
        /// channel, ascension moves both — so both are recomputed on every scale change and neither can
        /// drift out of step with the multipliers.</summary>
        private void UpdateModifiersValue()
        {
            Rescale(_implicits, BaseChannelMultiplier);
            Rescale(_modifiers, RolledChannelMultiplier);
            RescaleContext(_implicitsContextModifier, BaseChannelMultiplier);
            RescaleContext(_contextModifiers, RolledChannelMultiplier);
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

            // The base channel: every base stat hands the owner ONE flat — the scaled base with its
            // whole local bucket folded around it. The group loop above deliberately skipped those
            // locals (IsLocalBucketExternal), so nothing lands twice.
            foreach (var parameter in _baseStats.Keys)
            {
                (float baseValue, float localBonus) = GetBaseStatBreakdown(parameter);
                float effective = baseValue + localBonus;
                if (effective == 0f) continue;
                if (!resolved.TryGetValue(parameter, out var modifiers)) resolved[parameter] = modifiers = [];
                modifiers.Add(ModifiersCreator.CreateModifierInstance(parameter, ModifierValueType.Flat, effective, InstanceId));
            }

            return resolved;
        }

        private IModifierInstance AsInstance(IModifier modifier) =>
            modifier as IModifierInstance
            ?? new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.Value, InstanceId, modifier.Weight) { Scope = modifier.Scope };
    }
}
