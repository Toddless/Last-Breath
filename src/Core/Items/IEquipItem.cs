namespace Core.Items
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Entity;

    public interface IEquipItem : IItem
    {
        IReadOnlyList<IModifierInstance> Implicits { get; }
        IReadOnlyList<IModifierInstance> Modifiers { get; }

        /// <summary>Context lines ("healing efficiency +15%", "+1 bleed duration"): scale with upgrades
        /// like regular lines, but attach to the owner's pipelines on equip instead of parameter resolution.</summary>
        IReadOnlyList<ContextModifierEntry> ContextImplicits { get; }
        IReadOnlyList<ContextModifierEntry> ContextModifiers { get; }
        IReadOnlyList<IItemGrant> Grants { get; }
        IReadOnlyCollection<EntityParameter> AffectedParameters { get; }
        EquipmentPiece EquipmentPiece { get; }
        /// <summary>Read-only by design: levels move only through <see cref="Upgrade"/>/<see cref="Downgrade"/>,
        /// which keep the update multiplier (and every line value) in sync.</summary>
        int UpdateLevel { get; }
        int MaxUpdateLevel { get; set; }

        /// <summary>Value scale of the NEXT sharpening level relative to now — the UI "before → after"
        /// preview multiplies current line values by this (ascension cancels out of the ratio).</summary>
        float NextUpgradeValueScale { get; }

        /// <summary>Sealed items (ascended to Mythic) can never be modified again: no upgrades, rerolls or new grants.</summary>
        bool IsSealed { get; }

        /// <summary>The recipe's mandatory resources the item was crafted from (loot items have none).</summary>
        IReadOnlyDictionary<string, int> UsedRequiredResources { get; }

        /// <summary>Optional creation resources (additive slots: essences and the like).</summary>
        IReadOnlyDictionary<string, int> UsedOptionalResources { get; }

        /// <summary>Merged view of both parts (amounts of a shared id summed) — for consumers that
        /// treat every used resource the same (shatter refund, live reroll pool).</summary>
        IReadOnlyDictionary<string, int> UsedResources { get; }

        /// <summary>The item's caliber: the multiplier its lines were generated with (loot difficulty or
        /// crafting quality). Scales the LIVE reroll pool on every recraft; defaults to 1.</summary>
        float PowerMultiplier { get; set; }

        /// <summary>The piece's typed base channel (armor's evade, a ring's health) — rolled once at
        /// mint, stored UNSCALED; implicits are reserved for special authored lines. Local lines
        /// amplify this base (the weapon-damage convention generalized).</summary>
        IReadOnlyDictionary<EntityParameter, float> BaseStats { get; }

        /// <summary>Scaled base (sharpening × ascension) and everything the LOCAL lines add on top:
        /// effective = (Base + flat) × (1 + increase) × (1 + multiplier). The owner receives
        /// Base + LocalBonus as one flat; the tooltip splits it on the Ctrl reveal.</summary>
        (float Base, float LocalBonus) GetBaseStatBreakdown(EntityParameter parameter);

        /// <summary>Minter/restore plumbing — replaces the whole base channel.</summary>
        void SetBaseStats(IEnumerable<KeyValuePair<EntityParameter, float>> stats);

        /// <summary>How many modifier rerolls actually happened on this item — each one raises the next
        /// recraft's price. The setter exists for restore/copy plumbing; gameplay increments live in the
        /// upgrader and fire ONLY on a reroll that took place (a refusal is free and does not count).</summary>
        int RecraftCount { get; set; }

        /// <summary>Ascension's "everything +15%": a separate factor on top of the sharpening scale —
        /// every line of both channels recomputes as Base × UpdateMultiplier × AscensionMultiplier.
        /// Defaults to 1; the ascender sets it (from data) right before the seal, the save restores it.</summary>
        float AscensionMultiplier { get; set; }

        IEnumerable<IModifierInstance> GetResolvedModifiers(EntityParameter parameter);
        void SetImplicits(IEnumerable<IModifier> modifiers);
        void SetModifiers(IEnumerable<IModifier> modifiers);
        void SetContextImplicits(IEnumerable<ContextModifierEntry> entries);
        void SetContextModifiers(IEnumerable<ContextModifierEntry> entries);
        void OnEquip(IFightable owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        // Additional (rolled) modifiers are identified by InstanceId: duplicates of the same parameter+type
        // may coexist, so a rebuild/reroll targets exactly one line.
        void ReplaceAdditionalModifier(string instanceId, IModifierInstance newModifier);
        void SaveUsedResources(IReadOnlyDictionary<string, int> required, IReadOnlyDictionary<string, int> optional);
        void RemoveAdditionalModifier(string instanceId);
        void AddAdditionalModifier(IModifierInstance modifier);
        void AddAdditionalContextModifier(ContextModifierEntry entry);

        /// <summary>Slot-preserving twins of the Add pair: the reroll puts the fresh line back at the index
        /// the replaced one held, so a rerolled row keeps its place in the list instead of falling to the
        /// bottom. An index past the end appends.</summary>
        void InsertAdditionalModifier(int index, IModifierInstance modifier);
        void InsertAdditionalContextModifier(int index, ContextModifierEntry entry);
        void AddGrant(IItemGrant grant);

        /// <summary>Ascension-only: scales every grant's numeric payload ONCE (passive skill properties,
        /// granted modifier values). Unlike <see cref="AscensionMultiplier"/> this is not recomputable —
        /// the scaled values persist through save as the new base.</summary>
        void ScaleGrantValues(float factor);
    }
}
