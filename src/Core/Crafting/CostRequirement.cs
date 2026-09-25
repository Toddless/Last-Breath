namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    /// <summary>Per-rarity override of one cost line: either half may be omitted — the entry's
    /// defaults fill the gap (a rune keeps its id and varies the amount; a recraft main resource
    /// keeps amount 1 and varies the id).</summary>
    public readonly record struct RarityCostOverride(string? Id, int? Amount);

    /// <summary>One upgrade/recraft/ascend cost line as authored: defaults plus optional per-rarity
    /// overrides. Resolution is data-only — Common paying like Uncommon or Mythic like Legendary is
    /// expressed by authoring those rarities explicitly, never by code fallbacks.</summary>
    public class CostRequirement(RequirementType type, string? defaultId, int? defaultAmount, IReadOnlyDictionary<Rarity, RarityCostOverride>? byRarity = null)
    {
        public RequirementType Type => type;

        /// <summary>The concrete requirement for the item's rarity, or null when the line defines
        /// nothing for it (no default and no override — the cost simply has no such component there).</summary>
        public IRequirement? Resolve(Rarity rarity)
        {
            RarityCostOverride overrideEntry = default;
            byRarity?.TryGetValue(rarity, out overrideEntry);

            string? id = overrideEntry.Id ?? defaultId;
            int? amount = overrideEntry.Amount ?? defaultAmount;
            if (string.IsNullOrEmpty(id) || amount is not > 0) return null;

            return new Requirement(type, id, amount.Value);
        }
    }
}
