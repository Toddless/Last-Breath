namespace Core.Save
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.SaveData;
    using Enums;
    using Items;
    using Items.Grants;
    using Modifiers;

    /// <summary>
    /// Round-trips a procedurally rolled item through its save DTO. Restore rebuilds the item
    /// through the normal mutation API (Set*/Upgrade/TryAscend) so every invariant — value
    /// recomputation from the update multiplier, seal rules — is enforced by the item itself.
    /// </summary>
    public class EquipItemSaveConverter(IGrantFactory grantFactory)
    {
        public EquipItemSaveData ToData(IEquipItem item) => new()
        {
            Kind = item is IWeaponItem ? EquipItemSaveData.WeaponKind : EquipItemSaveData.EquipKind,
            Id = item.Id,
            Piece = item.EquipmentPiece,
            Tags = [.. item.Tags],
            Rarity = item.Rarity,
            UpdateLevel = item.UpdateLevel,
            MaxUpdateLevel = item.MaxUpdateLevel,
            IsSealed = item.IsSealed,
            PowerMultiplier = item.PowerMultiplier,
            RecraftCount = item.RecraftCount,
            AscensionMultiplier = item.AscensionMultiplier,
            Implicits = ToModifierData(item.Implicits),
            Modifiers = ToModifierData(item.Modifiers),
            ContextImplicits = ToContextData(item.ContextImplicits),
            ContextModifiers = ToContextData(item.ContextModifiers),
            UsedResources = new UsedResourcesSaveData
            {
                Required = new Dictionary<string, int>(item.UsedRequiredResources),
                Optional = new Dictionary<string, int>(item.UsedOptionalResources)
            },
            Grants = item.Grants.Select(ToGrantData).ToList(),
            WeaponType = (item as WeaponItem)?.WeaponType,
            Handedness = (item as WeaponItem)?.Handedness,
            BaseDamage = (item as WeaponItem)?.BaseDamage,
            CriticalChance = (item as WeaponItem)?.CriticalChance,
            CriticalDamage = (item as WeaponItem)?.CriticalDamage
        };

        public IEquipItem FromData(EquipItemSaveData data)
        {
            EquipItem item = data.Kind == EquipItemSaveData.WeaponKind
                ? new WeaponItem(
                    data.WeaponType ?? WeaponType.Sword,
                    data.Handedness ?? Handedness.OneHanded,
                    data.BaseDamage ?? 0f,
                    data.CriticalChance ?? 0f,
                    data.CriticalDamage ?? 0f,
                    data.Id, data.Tags)
                : new EquipItem(data.Piece, data.Id, data.Tags);

            // A missing field (pre-live-pool save) reads as the neutral 1 — the reroll pool is live now,
            // recomputed from data on every recraft, so nothing else needs restoring for it.
            item.PowerMultiplier = data.PowerMultiplier ?? 1f;
            item.RecraftCount = data.RecraftCount ?? 0; // legacy saves predate the counter: base price
            // Assigned BEFORE the lines and the seal replay: the Set*/Upgrade calls below recompute
            // every value through it. Legacy saves (pre-rework mythics included) read as the neutral 1.
            item.AscensionMultiplier = data.AscensionMultiplier ?? 1f;
            item.SetImplicits(data.Implicits.Select(modifier => ToModifier(modifier, item.InstanceId)));
            item.SetModifiers(data.Modifiers.Select(modifier => ToModifier(modifier, item.InstanceId)));
            item.SetContextImplicits(data.ContextImplicits.Select(ToContextEntry));
            item.SetContextModifiers(data.ContextModifiers.Select(ToContextEntry));
            item.SaveUsedResources(data.UsedResources.Required, data.UsedResources.Optional);
            foreach (var grant in data.Grants)
                item.AddGrant(FromGrantData(grant, item.InstanceId));

            item.MaxUpdateLevel = data.MaxUpdateLevel;
            if (data.UpdateLevel > 0) item.Upgrade(data.UpdateLevel);

            // The only path into IsSealed is ascension (Legendary at max level -> Mythic + seal),
            // so a sealed item is re-sealed by replaying that transition.
            if (data.IsSealed)
            {
                item.Rarity = Rarity.Legendary;
                if (!item.TryAscend()) item.Rarity = data.Rarity;
            }
            else
            {
                item.Rarity = data.Rarity;
            }

            return item;
        }

        private static List<ModifierSaveData> ToModifierData(IReadOnlyList<IModifier> modifiers) =>
            modifiers.Select(modifier => new ModifierSaveData
            {
                Parameter = modifier.EntityParameter,
                ValueType = modifier.ModifierValueType,
                Scope = modifier.Scope,
                BaseValue = modifier.BaseValue,
                Weight = modifier.Weight,
                Affix = (modifier as SimpleModifier)?.Affix is { } affix and not AffixKind.None ? affix : null,
                GroupId = (modifier as SimpleModifier)?.GroupId,
                RangeMin = (modifier as SimpleModifier)?.RolledRange?.Min,
                RangeMax = (modifier as SimpleModifier)?.RolledRange?.Max
            }).ToList();

        private static IModifierInstance ToModifier(ModifierSaveData data, string source) =>
            new SimpleModifier(data.Parameter, data.ValueType, data.BaseValue, source, data.Weight)
            {
                Scope = data.Scope,
                Affix = data.Affix ?? AffixKind.None,
                GroupId = data.GroupId,
                RolledRange = ToRolledRange(data.RangeMin, data.RangeMax)
            };

        private static List<ContextModifierSaveData> ToContextData(IReadOnlyList<ContextModifierEntry> entries) =>
            entries.Select(entry => new ContextModifierSaveData
            {
                Parameter = entry.Parameter,
                ValueType = entry.ValueType,
                BaseValue = entry.BaseValue,
                Weight = entry.Weight,
                Affix = entry.Affix != AffixKind.None ? entry.Affix : null,
                GroupId = entry.GroupId,
                RangeMin = entry.RolledRange?.Min,
                RangeMax = entry.RolledRange?.Max
            }).ToList();

        private static ContextModifierEntry ToContextEntry(ContextModifierSaveData data) =>
            new(data.Parameter, data.ValueType, data.BaseValue, data.Weight)
            {
                Affix = data.Affix ?? AffixKind.None,
                GroupId = data.GroupId,
                RolledRange = ToRolledRange(data.RangeMin, data.RangeMax)
            };

        private static ValueRange? ToRolledRange(float? min, float? max) =>
            min is { } rangeMin && max is { } rangeMax ? new ValueRange(rangeMin, rangeMax) : null;

        private GrantSaveData ToGrantData(IItemGrant grant) => grant switch
        {
            ModifierGrant modifierGrant => new GrantSaveData
            {
                Kind = GrantSaveData.ModifierKind,
                Id = grant.Id,
                Modifiers = ToModifierData([.. modifierGrant.Modifiers])
            },
            PassiveSkillGrant skillGrant => new GrantSaveData
            {
                Kind = GrantSaveData.PassiveSkillKind,
                Id = grant.Id,
                SkillId = skillGrant.SkillId,
                Properties = new Dictionary<string, float>(skillGrant.Properties)
            },
            EffectGrant effectGrant => new GrantSaveData
            {
                Kind = GrantSaveData.EffectKind,
                Id = grant.Id,
                EffectId = effectGrant.EffectId,
                Properties = new Dictionary<string, float>(effectGrant.Properties)
            },
            // A silently dropped grant is a corrupted item: fail the capture, the old save survives.
            _ => throw new NotSupportedException($"Grant type {grant.GetType().Name} has no save representation.")
        };

        private IItemGrant FromGrantData(GrantSaveData data, string source) => data.Kind switch
        {
            GrantSaveData.PassiveSkillKind => grantFactory.CreatePassiveGrant(data.Id, data.SkillId ?? data.Id, data.Properties),
            GrantSaveData.EffectKind => grantFactory.CreateEffectGrant(data.Id, data.EffectId ?? data.Id, data.Properties),
            _ => grantFactory.CreateModifierGrant(data.Id, data.Modifiers.Select(modifier => ToModifier(modifier, source)).ToList())
        };
    }
}
