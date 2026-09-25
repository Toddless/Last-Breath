namespace Core.Modifiers
{
    using Entity.Components;
    using Enums;

    /// <summary>The single source of affix slot capacity per rarity (replaces the flat
    /// "modifier amount" conversion): total capacity stays 1/2/3/4, but it is now split into
    /// prefix/suffix slots. Odd capacities roll their orientation 50/50.</summary>
    public static class AffixRules
    {
        public static (int Prefixes, int Suffixes) SlotsFor(Rarity rarity, IRandomNumberGenerator rnd) => rarity switch
        {
            Rarity.Uncommon => rnd.RandFloat() < 0.5f ? (1, 0) : (0, 1),
            Rarity.Rare => (1, 1),
            Rarity.Epic => rnd.RandFloat() < 0.5f ? (2, 1) : (1, 2),
            Rarity.Legendary => (2, 2),
            // Common rolls nothing; Unique/Mythic are authored templates and never enter the affix roll.
            _ => (0, 0),
        };
    }
}
