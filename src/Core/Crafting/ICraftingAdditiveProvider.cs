namespace Core.Crafting
{
    using System.Collections.Generic;

    /// <summary>
    /// Crafting effects of an OPTIONAL resource (essences and the like), from the
    /// CraftingAdditives catalog. Upgrade: a flat success-chance bonus and a chance of a second
    /// level on success. Recraft: a modifier pool mixed into the roll FOR THIS OPERATION ONLY —
    /// the item's own pool, fixed at creation, never changes. Create: a creation rune's
    /// <paramref name="MinRarity"/> guarantees the crafted item's rarity floor (lower enum = better).
    /// </summary>
    public record CraftingAdditiveEffects(float UpgradeChanceBonus, float ExtraUpgradeLevelChance, string? RecraftPoolId, Enums.Rarity? MinRarity = null);

    public interface ICraftingAdditiveProvider
    {
        /// <summary>Every resource id that has additive effects — the UI offers these in the optional slots.</summary>
       IReadOnlyCollection<string> KnownAdditiveIds { get; }

        /// <summary>Null when the resource carries no crafting additive effects.</summary>
        CraftingAdditiveEffects? GetEffects(string resourceId);
    }
}
