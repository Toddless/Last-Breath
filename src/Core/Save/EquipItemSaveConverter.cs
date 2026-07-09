namespace Core.Save
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Skills;
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
    public class EquipItemSaveConverter(Func<ISkillProvider?> skillProviderAccessor)
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
            Implicits = ToModifierData(item.Implicits),
            Modifiers = ToModifierData(item.Modifiers),
            ContextImplicits = ToContextData(item.ContextImplicits),
            ContextModifiers = ToContextData(item.ContextModifiers),
            ModifiersPool = ToModifierData(item.ModifiersPool),
            UsedResources = new Dictionary<string, int>(item.UsedResources),
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

            item.SetImplicits(data.Implicits.Select(modifier => ToModifier(modifier, item.InstanceId)));
            item.SetModifiers(data.Modifiers.Select(modifier => ToModifier(modifier, item.InstanceId)));
            item.SetContextImplicits(data.ContextImplicits.Select(ToContextEntry));
            item.SetContextModifiers(data.ContextModifiers.Select(ToContextEntry));
            item.SaveModifiersPool(data.ModifiersPool.Select(modifier => (IModifier)ToModifier(modifier, item.InstanceId)));
            item.SaveUsedResources(new Dictionary<string, int>(data.UsedResources));
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
                Weight = modifier.Weight
            }).ToList();

        private static IModifierInstance ToModifier(ModifierSaveData data, string source) =>
            new SimpleModifier(data.Parameter, data.ValueType, data.BaseValue, source, data.Weight) { Scope = data.Scope };

        private static List<ContextModifierSaveData> ToContextData(IReadOnlyList<ContextModifierEntry> entries) =>
            entries.Select(entry => new ContextModifierSaveData
            {
                Parameter = entry.Parameter,
                ValueType = entry.ValueType,
                BaseValue = entry.BaseValue,
                Weight = entry.Weight
            }).ToList();

        private static ContextModifierEntry ToContextEntry(ContextModifierSaveData data) =>
            new(data.Parameter, data.ValueType, data.BaseValue, data.Weight);

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
            // A silently dropped grant is a corrupted item: fail the capture, the old save survives.
            _ => throw new NotSupportedException($"Grant type {grant.GetType().Name} has no save representation.")
        };

        private IItemGrant FromGrantData(GrantSaveData data, string source) => data.Kind switch
        {
            GrantSaveData.PassiveSkillKind => new PassiveSkillGrant(data.Id, data.SkillId ?? string.Empty, data.Properties, skillProviderAccessor),
            _ => new ModifierGrant(data.Id, data.Modifiers.Select(modifier => ToModifier(modifier, source)).ToList())
        };
    }
}
