namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Core.Results;
    using Godot;

    public class ItemUpgrader(
        IRandomNumberGenerator rnd,
        ICraftingMastery mastery,
        IItemDataProvider itemDataProvider,
        ICraftingAdditiveProvider additives,
        IModifierMaterializer materializer) : IItemUpgrader
    {
        private const float P0 = 0.95f;
        private const float P5 = 0.70f;
        private const float P6 = 0.35f;
        private const float P9 = 0.15f;
        private const float P12 = 0.01f;

        /// <summary>Each successful reroll raises the next recraft's price by this fraction of the
        /// base cost (amounts round UP). Lives next to the sharpening curve constants by the same rule:
        /// operation pricing shape is upgrader-owned, the base amounts stay in UpgradeCosts data.</summary>
        private const float RecraftCostGrowthPerReroll = 0.1f;

        private static readonly float s_r = Mathf.Pow(P12 / P9, 1.0f / 3.0f);

        // Costs come rarity-resolved straight from data (byRarity dimension) — no code scaling.
        public List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory) =>
            itemDataProvider.GetUpgradeCost(itemCategory, itemRarity).ToList();

        public List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory) =>
            itemDataProvider.GetRecraftCost(itemCategory, itemRarity).ToList();

        // The item-aware price: base cost of its rarity/category, every amount scaled by
        // ceil(base × (1 + growth × RecraftCount)). This is the price the handler spends;
        // the UI merely mirrors it.
        public List<IRequirement> GetRecraftResourceCost(IEquipItem item) =>
            GetRecraftResourceCost(item.Rarity, item.EquipmentPiece.ConvertEquipmentPartToCategory())
                .Select(IRequirement (requirement) => new Requirement(requirement.Type, requirement.Id,
                    Mathf.CeilToInt(requirement.Amount * (1f + (RecraftCostGrowthPerReroll * item.RecraftCount)))))
                .ToList();

        public string? TryRecraftModifier(IEquipItem item, string modifierInstanceId, IReadOnlyCollection<string>? additiveResourceIds = null)
        {
            var target = FindLine(item, modifierInstanceId);
            if (target == null) return null; // the line is not on the item
            (var targetAffix, string? targetGroupId) = target.Value;
            // A reroll preserves the slot family: candidates are filtered to the target line's affix.
            // A None line (legacy save / authored fodder predating the affix markup) has no slot family
            // to preserve, so it tolerantly rerolls from the FULL pool.
            var livePool = GetRerollPreviewPool(item, additiveResourceIds);

            // No duplicate lines: a candidate whose IDENTITY (what the line is about, NEVER the values —
            // atoms by key, composites by their whole part set) already sits on the item is out. The
            // identities are read BEFORE anything leaves the item, so a refusal below still mutates
            // nothing; the target's own line/group is lifted, so a reroll may honestly return the same
            // stat (or the same composite) with fresh values. Entries that materialize into something
            // else (a rolled grant, the sharpening-levels operation — even hidden among composite parts)
            // would take the old line away and put no line back, so they carry no identity and never qualify.
            var groupMemberIds = GroupMemberIds(item, modifierInstanceId, targetGroupId);
            var occupied = item.OccupiedLineIdentities(groupMemberIds);
            var candidates = livePool
                .Where(descriptor => targetAffix == AffixKind.None || descriptor.Affix == targetAffix)
                .Where(descriptor => LineIdentity.TryFrom(descriptor, out var identity) && !occupied.Contains(identity))
                .ToList();
            // Refuse for free (the handler spends nothing) when nothing of the required kind survived the
            // filters — or when what survived carries no weight at all (weight 0 = "never rolls").
            (var weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(candidates);
            if (totalWeight <= 0f) return null;

            // The replaced line's slot, read BEFORE the removal: taking the group out shifts everything
            // after its first member one step left, so that very index is where the replacement belongs.
            // The fresh line keeps the row's place instead of falling to the bottom of the list.
            int entitySlot = SlotOf(item.Modifiers.Select(modifier => modifier.InstanceId), groupMemberIds);
            int contextSlot = SlotOf(item.ContextModifiers.Select(entry => entry.InstanceId), groupMemberIds);

            // The replacement is minted BEFORE the old line leaves: a candidate that materializes into
            // nothing (its predicate is named by an id this host cannot build) would otherwise take the
            // line away and put none back — a reroll that destroys a line is not one the player asked for,
            // and an impossible reroll costs nothing by the same rule as every refusal above.
            var picked = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);
            var sink = new CollectingSink();
            materializer.Materialize(picked, sink, item.InstanceId); // candidates are already power-scaled
            if (sink.Entities.Count == 0 && sink.Contexts.Count == 0) return null;

            // A grouped line (parts of one composite roll, presented to the player as a SINGLE line)
            // rerolls as one unit: every part leaves the item and exactly one fresh candidate replaces
            // the whole group — an atom, or another composite (the materializer stamps its parts with
            // a fresh GroupId, so they land as one line again). Group affix sits on every part
            // (stamped from the composite root), so the filter above already used it.
            foreach (string memberId in groupMemberIds)
                item.RemoveAdditionalModifier(memberId);

            foreach (var entity in sink.Entities) item.InsertAdditionalModifier(entitySlot++, entity);
            foreach (var context in sink.Contexts) item.InsertAdditionalContextModifier(contextSlot++, context);

            // The reroll HAPPENED — only now does the growing-price counter move (refusals above are free).
            item.RecraftCount++;

            return sink.Entities.Select(entity => entity.InstanceId)
                .Concat(sink.Contexts.Select(entry => entry.InstanceId))
                .FirstOrDefault();
        }

        /// <summary>LIVE pool, computed at call time (never stored on the item, so a json edit is visible
        /// on existing items): family pool + the item's own pool + the used resources' descriptors, plus
        /// the operation's additive pools. The WHOLE union scales by the item's PowerMultiplier so a
        /// reroll matches the magnitude the item was born with (loot difficulty or crafting quality) —
        /// additives included, deliberately. TryRecraftModifier rolls from EXACTLY this; the window's
        /// pool column renders it (same one-source rule as the sharpening chance bar).</summary>
        public IEnumerable<IModifierDescriptor> GetRerollPreviewPool(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null) =>
            itemDataProvider.GetLiveRerollPool(item)
                .Concat(AdditivePools(additiveResourceIds))
                .Concat(AdditiveResourceDescriptors(additiveResourceIds, item))
                .Select(descriptor => DescriptorOperations.Scale(descriptor, item.PowerMultiplier));

        // The informational mirror of the roll below: ONE formula in ComputeUpgradeChance serves both,
        // so the chance bar can never drift from what TryUpgradeItem actually rolls against.
        public float GetUpgradeChance(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null) =>
            ComputeUpgradeChance(item.UpdateLevel, AdditiveEffects(additiveResourceIds));

        public float GetBaseUpgradeChance(IEquipItem item) => Mathf.Clamp(GetChance(item.UpdateLevel), 0f, 1f);

        public ItemUpgradeResult TryUpgradeItem(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null)
        {
            if (item.UpdateLevel == item.MaxUpdateLevel) return ItemUpgradeResult.ReachedMaxLevel;

            var effects = AdditiveEffects(additiveResourceIds);
            float chances = ComputeUpgradeChance(item.UpdateLevel, effects);
            bool upgradeSucceed = rnd.RandFloat() <= chances;

            if (upgradeSucceed)
            {
                item.Upgrade();
                TryGrantExtraLevel(item, effects);
            }

            return upgradeSucceed ? ItemUpgradeResult.Success : ItemUpgradeResult.Failure;
        }

        /// <summary>Additives may gift a second level on success (never past the cap).</summary>
        private void TryGrantExtraLevel(IEquipItem item, List<CraftingAdditiveEffects> effects)
        {
            float extraChance = Mathf.Clamp(effects.Sum(e => e.ExtraUpgradeLevelChance), 0f, 1f);
            if (extraChance <= 0f || item.UpdateLevel >= item.MaxUpdateLevel) return;
            if (rnd.RandFloat() <= extraChance) item.Upgrade();
        }

        /// <summary>The target line's slot family and group, from whichever channel holds it; null when the
        /// id is not on the item at all. Entity lines that predate the stamps (bare instances) read as
        /// (None, no group).</summary>
        private static (AffixKind, string?)? FindLine(IEquipItem item, string instanceId)
        {
            foreach (var modifier in item.Modifiers)
                if (modifier.InstanceId == instanceId)
                    return modifier is SimpleModifier simple ? (simple.Affix, simple.GroupId) : (AffixKind.None, null);

            foreach (var entry in item.ContextModifiers)
                if (entry.InstanceId == instanceId)
                    return (entry.Affix, entry.GroupId);

            return null;
        }

        /// <summary>Where the group sits in one channel — the index of its first member there. A group with
        /// no member in that channel (the reroll crossed channels) reads as the end of the list, so the
        /// replacement appends instead of stealing someone else's slot.</summary>
        private static int SlotOf(IEnumerable<string> channelInstanceIds, IReadOnlyCollection<string> groupMemberIds)
        {
            int index = 0;
            foreach (string instanceId in channelInstanceIds)
            {
                if (groupMemberIds.Contains(instanceId)) break;
                index++;
            }

            return index;
        }

        /// <summary>Every rolled line sharing the target's GroupId, across both channels; just the target
        /// itself when it is not part of a group.</summary>
        private static List<string> GroupMemberIds(IEquipItem item, string instanceId, string? groupId)
        {
            if (groupId == null) return [instanceId];
            return item.Modifiers.Where(modifier => modifier is SimpleModifier simple && simple.GroupId == groupId)
                .Select(modifier => modifier.InstanceId)
                .Concat(item.ContextModifiers.Where(entry => entry.GroupId == groupId).Select(entry => entry.InstanceId))
                .ToList();
        }

        /// <summary>Final chance = level curve × (1 + mastery bonus) + flux bonuses, clamped to a probability.</summary>
        private float ComputeUpgradeChance(int level, List<CraftingAdditiveEffects> effects) =>
            Mathf.Clamp(
                GetChance(level) * (1f + mastery.GetUpgradeChanceBonus()) + effects.Sum(e => e.UpgradeChanceBonus),
                0f, 1f);

        private List<CraftingAdditiveEffects> AdditiveEffects(IReadOnlyCollection<string>? resourceIds) =>
            resourceIds?.Select(additives.GetEffects).Where(effects => effects != null).Cast<CraftingAdditiveEffects>().ToList() ?? [];

        // Additive pools are parsed as affixed descriptors like every rollable pool (strict parse guarantees it).
        private IEnumerable<IModifierDescriptor> AdditivePools(IReadOnlyCollection<string>? resourceIds) =>
            AdditiveEffects(resourceIds)
                .Where(effects => !string.IsNullOrEmpty(effects.RecraftPoolId))
                .SelectMany(effects => itemDataProvider.GetEquipItemModifierPool(effects.RecraftPoolId!));

        /// <summary>Essences ride the optional slots as PLAIN resources: their own descriptors join
        /// the operation's pool exactly like creation (where every used resource feeds the roll).
        /// Entries restricted to another equipment category are gated out.</summary>
        private IEnumerable<IModifierDescriptor> AdditiveResourceDescriptors(IReadOnlyCollection<string>? resourceIds, IEquipItem item) =>
            (resourceIds ?? []).SelectMany(itemDataProvider.GetResourceDescriptors)
                .ForCategory(item.EquipmentPiece.ConvertEquipmentPartToCategory());

        private float GetChance(int level)
        {
            return true switch
            {
                _ when level <= 5 => Mathf.Lerp(P0, P5, level / 3.0f),
                _ when level <= 9 => Mathf.Lerp(P6, P9, (level - 5) / 3.0f),
                _ => P9 * Mathf.Pow(s_r, level - 9),
            };
        }
    }
}
